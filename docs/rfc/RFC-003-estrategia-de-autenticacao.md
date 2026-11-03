# RFC-003 — Estratégia de autenticação

- **Status:** Aceita
- **Data:** 2026-11-03 (registro retroativo da decisão tomada na separação em quatro repositórios, em outubro de 2026)
- **Autor:** Arthur Castro
- **Escopo:** TechChallenger-severless, TechChallenger-infra-kubernetes, TechChallenger-Api

## Resumo

Propõe autenticar clientes **pelo CPF** numa **Lambda** exposta pelo API Gateway, que emite um **JWT HS256** de curta duração e um **refresh token opaco com rotação**. A API no EKS valida o JWT em cada requisição com o mesmo segredo, guardado no SSM. Funcionários (`Adm`, `Funcionario`) usam a mesma Lambda, com e-mail e senha.

O contrato detalhado (campos, erros, tokens, LGPD) está em [auth-flow.md do TechChallenger-severless](https://github.com/arthur-lucas-castro/TechChallenger-severless/blob/main/docs/auth-flow.md).

## Contexto

A Fase 3 pede:

- proteger rotas sensíveis com autenticação via CPF;
- uma function serverless que valide o CPF, consulte a existência e o status do cliente e devolva um JWT válido para as APIs protegidas.

A API já tinha autenticação JWT própria para funcionários (`POST /Auth/login`), com o middleware `JwtBearer` do ASP.NET Core e papéis (`[Authorize(Roles = "Adm")]`).

## Questões a decidir

1. Onde o token é emitido.
2. Algoritmo de assinatura e onde fica a chave.
3. Onde o token é validado: no API Gateway ou na API.
4. Como manter a sessão sem pedir o CPF a cada 15 minutos.

## Alternativas

### 1. Emissão do token

| Opção | Prós | Contras |
|---|---|---|
| **Lambda própria (escolhida)** | Atende literalmente o requisito; regra de status do cliente sob controle; sem serviço extra | Código de segurança mantido pelo time |
| Amazon Cognito com *custom auth challenge* | Gestão de usuários e tokens pronta | CPF não é senha: exigiria um fluxo customizado com três Lambdas (define/create/verify challenge); duplicaria o cadastro de clientes fora do banco; mais difícil de operar no Learner Lab |
| Endpoint na própria API | Simples | Não é serverless; não atende o requisito |

### 2. Assinatura

| Opção | Prós | Contras |
|---|---|---|
| **HS256 com segredo no SSM (escolhida)** | A API já valida HS256; um único segredo SecureString lido pela Lambda e pela pipeline da API | Quem valida também poderia assinar; a rotação exige atualizar os dois lados |
| RS256 com chave no KMS e JWKS | Só a Lambda assina; validação por chave pública | Exige KMS, endpoint JWKS e mudança na API; ganho pequeno com dois serviços do mesmo time |

### 3. Validação

| Opção | Prós | Contras |
|---|---|---|
| **Middleware `JwtBearer` na API (escolhida)** | Já existia; valida assinatura, emissor, audiência, expiração e papéis por rota | O gateway encaminha requisições sem token até a API, que responde 401 |
| JWT authorizer nativo do HTTP API | Barra token inválido na borda | Só aceita assinatura assimétrica com JWKS; não serve para HS256 |
| Lambda Authorizer | Barra na borda com HS256; recomendado no [auth-flow.md](https://github.com/arthur-lucas-castro/TechChallenger-severless/blob/main/docs/auth-flow.md) como camada extra, mantendo o middleware | Mais uma Lambda e mais latência em toda requisição; os papéis continuariam sendo checados na API. **Não implementado** (ver Consequências) |

### 4. Sessão

| Opção | Prós | Contras |
|---|---|---|
| **Access token curto + refresh token rotativo (escolhida)** | Token roubado vale no máximo 15 min; reuso de refresh token revoga a sessão inteira; logout real | Tabela `refresh_token` no banco |
| Access token longo | Simples | Sem revogação; risco alto se vazar |

## Decisão

| Aspecto | Decisão |
|---|---|
| Rotas | `POST /auth` (CPF), `POST /auth/login` (e-mail e senha), `POST /auth/refresh`, `POST /auth/logout`, todas para a Lambda `techchallenger-fase1-auth` |
| Validação do CPF | Normalização e dígitos verificadores; CPF nunca vai para o token nem aparece em claro nos logs (mascarado) |
| Status do cliente | Só `ATIVO` autentica (`INATIVO` e `BLOQUEADO` recebem 403); coluna `cliente.status` criada pela migration `V001` |
| Access token | JWT HS256, 15 minutos, claims `sub`, `role` (`cliente`, `Adm`, `Funcionario`), `iss` e `aud` (`TechChallenger`), `jti` |
| Refresh token | Opaco, guardado só como hash SHA-256; 7 dias, sessão máxima de 30 dias; rotação a cada uso; reuso revoga a família |
| Segredo | `/techchallenger/jwt/secret` no SSM (SecureString, mínimo 32 bytes); a Lambda lê em tempo de execução e a pipeline da API o injeta como Secret do Kubernetes |
| Validação na API | `JwtBearer` com `ValidateIssuer`, `ValidateAudience`, `ValidateLifetime` e `ClockSkew = 0`; `[Authorize]` em todos os controllers e `[Authorize(Roles = "Adm")]` nas escritas de catálogo |
| Proteção contra força bruta | Throttling próprio nas rotas `/auth*` no API Gateway (5 req/s, burst 10) |

## Consequências

- A API não depende de rede para validar tokens: a verificação é local, sem chamada à Lambda.
- Rotacionar o segredo exige atualizar o SSM, aguardar um cold start da Lambda e fazer um deploy da API, nessa ordem; tokens antigos param de valer.
- O `POST /Auth/login` legado da API continua funcionando, mas o caminho oficial é o da Lambda, que é o único com refresh token.
- O Lambda Authorizer recomendado no `auth-flow.md` ficou fora desta entrega: a proteção exigida já é garantida pelo middleware, e o authorizer só anteciparia a recusa para a borda. É a próxima evolução prevista, sem mudar o contrato dos tokens.
- Se o time migrar para RS256 + JWKS, o JWT authorizer nativo do HTTP API passa a ser possível e substitui o Lambda Authorizer, barrando requisições sem token na borda.

# Diagramas de sequência

Fluxos como estão implementados hoje. Todas as chamadas passam pelo API Gateway. O contrato completo da autenticação (campos, erros, tokens) está em [auth-flow.md do TechChallenger-severless](https://github.com/arthur-lucas-castro/TechChallenger-severless/blob/main/docs/auth-flow.md).

## 1. Autenticação do cliente por CPF e consumo de rota protegida

```mermaid
sequenceDiagram
    autonumber
    actor C as Cliente
    participant GW as API Gateway (HTTP API)
    participant L as Lambda /auth (Node.js)
    participant SSM as SSM Parameter Store
    participant DB as RDS PostgreSQL
    participant API as API da oficina (EKS)
    participant DD as Datadog

    C->>GW: POST /auth { "cpf": "529.982.247-25" }
    Note over GW: throttling da rota (5 req/s)<br/>requestId = correlação
    GW->>L: invoca (payload 2.0)

    L->>L: normaliza e valida dígitos do CPF
    alt CPF inválido
        L-->>GW: 400 CPF_INVALIDO
        GW-->>C: 400
    end

    opt cold start
        L->>SSM: GetParameters (/techchallenger/db/*, /techchallenger/jwt/secret)
        SSM-->>L: credenciais e segredo (cache na instância)
    end

    L->>DB: SELECT id, nome, status FROM cliente WHERE numerodocumento = $1
    DB-->>L: cliente
    alt cliente não encontrado
        L-->>GW: 404 CLIENTE_NAO_ENCONTRADO
        GW-->>C: 404
    else status diferente de ATIVO
        L-->>GW: 403 CLIENTE_INATIVO
        GW-->>C: 403
    else cliente ativo
        L->>L: assina access token JWT HS256<br/>(sub, role=cliente, iss, aud, exp 15 min)
        L->>DB: INSERT refresh_token (hash SHA-256, família, validade)
        L-->>GW: 200 { accessToken, refreshToken, expiresIn }
        GW-->>C: 200
    end
    L--)DD: logs JSON (correlationId, CPF mascarado),<br/>trace com spans pg.query (Datadog Extension)

    C->>GW: GET /Peca (Authorization: Bearer accessToken)
    GW->>API: VPC Link → NLB → pod (x-correlation-id)
    API->>API: JwtBearer valida assinatura, iss, aud e exp<br/>com o mesmo segredo (Secret do Kubernetes)
    alt token ausente, inválido ou expirado
        API-->>C: 401
    else rota exige Adm e o token é de cliente
        API-->>C: 403
    else autorizado
        API->>DB: SELECT ... (EF Core)
        API-->>C: 200 [peças]
    end
    API--)DD: trace aspnet_core.request + postgres.query,<br/>log JSON com CorrelationId e dd.trace_id
```

`Adm` e `Funcionario` seguem o mesmo caminho em `POST /auth/login`, com e-mail e senha (bcrypt) da tabela `usuario`. O token recebe `role=Adm` ou `role=Funcionario`, que a API usa em `[Authorize(Roles = "Adm")]`. Quando o access token expira, `POST /auth/refresh` troca o refresh token por um novo par (rotação); reusar um refresh token já trocado revoga toda a família (sessão).

## 2. Abertura da ordem de serviço

```mermaid
sequenceDiagram
    autonumber
    actor A as Atendente (token Adm)
    participant GW as API Gateway
    participant CT as OrdemServicoController
    participant SV as OrdemServicoService
    participant CAT as ServicoService / PecaService
    participant REP as OrdemServicoRepositorio
    participant DB as RDS PostgreSQL
    participant DD as Datadog

    A->>GW: POST /OrdemServico<br/>{ clienteId, veiculoId, servicos[], pecas[] }
    GW->>CT: VPC Link → NLB → pod
    Note over CT: CorrelationIdMiddleware<br/>+ JwtBearer + [Authorize]
    CT->>SV: CriarAsync(dto)
    SV->>SV: nova OrdemServico (status Recebida, DataCriacao)
    loop cada serviço solicitado
        SV->>CAT: ObterPorIdAsync(servicoId)
        CAT->>DB: SELECT servico
        alt serviço inexistente
            SV-->>CT: KeyNotFoundException
            CT-->>A: 404 (ExceptionMiddleware)<br/>+ métrica oficina.api.falha{tipo:nao_encontrado}
        end
        SV->>SV: AdicionarServico (preço do catálogo)
    end
    loop cada peça solicitada
        SV->>CAT: ObterPorIdAsync(pecaId)
        CAT->>DB: SELECT peca
        SV->>SV: AdicionarPeca (nome e preço do catálogo)
    end
    SV->>REP: InsertAsync(ordem)
    REP->>DB: INSERT ordemservico, servicosolicitado, pecasolicitada (uma transação)
    DB-->>REP: id
    SV--)DD: métrica oficina.os.abertas (DogStatsD)
    SV-->>CT: id
    CT-->>A: 201 Created { id }
    CT--)DD: trace + log "HTTP POST /OrdemServico responded 201"
```

## 3. Ciclo da ordem de serviço até a entrega

Cada mudança de status gera o evento de domínio `OrdemServicoStatusAlteradoEvent`, despachado depois do commit. Os handlers rodam na mesma requisição ([ADR-005](../adr/ADR-005-padrao-de-comunicacao.md)).

```mermaid
sequenceDiagram
    autonumber
    actor A as Atendente (token Adm)
    actor C as Cliente (token CPF)
    participant API as API da oficina
    participant OS as OrdemServico (agregado)
    participant DISP as DomainEventDispatcher
    participant MET as MetricasStatusOrdemServicoHandler
    participant NOT as NotificacaoStatusOrdemServicoHandler
    participant DB as RDS
    participant SMTP as Provedor SMTP
    participant DD as Datadog

    A->>API: PATCH /StatusOrdemServico/{id}/iniciar-diagnostico
    API->>OS: IniciarDiagnostico()
    OS->>OS: Recebida → EmDiagnostico<br/>+ OrdemServicoStatusAlteradoEvent
    API->>DB: UPDATE ordemservico
    API->>DISP: DispatchAsync(eventos)
    DISP->>MET: tempo em Recebida
    MET--)DD: oficina.os.tempo_no_status{status:Recebida}
    DISP->>NOT: notificar cliente
    NOT->>DB: SELECT cliente
    NOT->>SMTP: e-mail "entrou em diagnóstico" (Polly: até 3 novas tentativas, backoff exponencial)
    alt SMTP falha
        NOT--)DD: oficina.integracao.falha{integracao:smtp}<br/>(a OS não é desfeita)
    end
    API-->>A: 204

    A->>API: PATCH /StatusOrdemServico/{id}/finalizar-diagnostico
    API->>OS: FinalizarDiagnostico()
    OS->>OS: gera Orcamento (soma serviços + peças)<br/>EmDiagnostico → AguardandoAprovacao
    API->>DB: UPDATE ordemservico, INSERT orcamento
    API->>DISP: eventos (métrica + e-mail "orçamento disponível")
    API-->>A: 204

    C->>API: POST /Cliente/{clienteId}/ordens-servico/{id}/responder-orcamento { aprovado: true }
    API->>DISP: OrcamentoRespondidoEvent
    DISP->>OS: AprovarOrcamento()
    OS->>OS: AguardandoAprovacao → EmExecucao
    API->>DB: UPDATE orcamento, ordemservico
    API-->>C: 204

    A->>API: PATCH /StatusOrdemServico/{id}/status { "status": "Finalizada" }
    API->>OS: FinalizarOrdem() → Finalizada (DataFinalizacao)
    API-->>A: 204

    A->>API: POST /OrdemServico/{id}/confirmar-pagamento
    API->>OS: EntregarVeiculo() → Entregue
    API-->>A: 204

    Note over DD: dashboard: tempo médio em Diagnóstico,<br/>Execução e Finalização (oficina.os.tempo_no_status)
    alt transição inválida (ex.: diagnóstico em OS entregue)
        API-->>A: 422 TransicaoStatusInvalida<br/>+ oficina.api.falha{tipo:dominio}
    else erro inesperado ou banco indisponível
        API-->>A: 500<br/>+ oficina.api.falha{tipo:erro_interno} → alerta no Datadog
    end
```

## Estados da ordem de serviço

```mermaid
stateDiagram-v2
    [*] --> Recebida: POST /OrdemServico
    Recebida --> EmDiagnostico: iniciar-diagnostico
    Recebida --> AguardandoAprovacao: finalizar-diagnostico
    EmDiagnostico --> AguardandoAprovacao: finalizar-diagnostico (gera orçamento)
    AguardandoAprovacao --> EmExecucao: orçamento aprovado pelo cliente
    AguardandoAprovacao --> AguardandoAprovacao: orçamento recusado (e-mail ao cliente)
    EmExecucao --> Finalizada: status Finalizada
    Finalizada --> Entregue: confirmar-pagamento
    Entregue --> [*]
```

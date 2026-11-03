# ADR-005 — Padrão de comunicação entre componentes

- **Status:** Aceito
- **Data:** 2026-11-03
- **Repositórios afetados:** TechChallenger-Api, TechChallenger-severless, TechChallenger-infra-kubernetes

## Contexto

O sistema tem dois serviços executáveis, a **API da oficina** no EKS e a **Lambda de autenticação**, e dentro da API três bounded contexts: **Atendimento** (clientes e veículos), **Catálogo** (serviços, peças e estoque) e **Operação** (ordens de serviço). Também fala com dois sistemas externos: o **PostgreSQL** e o **provedor SMTP**.

É preciso definir como esses componentes se comunicam:

1. **Cliente → sistema:** como os clientes chamam a API e a autenticação.
2. **Entre os contextos da API:** por exemplo, a mudança de status da OS precisa notificar o cliente (Atendimento) e registrar métricas; a aprovação do orçamento pelo cliente precisa mudar o status da OS (Operação).
3. **Lambda ↔ API:** como a API confia no token emitido pela Lambda.
4. **Notificações:** o e-mail ao cliente a cada mudança de status.

Requisitos que pesam: consistência da OS (status, orçamento e estoque não podem divergir), simplicidade operacional no Learner Lab, latência baixa e rastreabilidade ponta a ponta (correlation id e traces no Datadog).

## Alternativas consideradas

### Opção A — REST síncrono na borda + eventos de domínio em processo (escolhida)

- Clientes chamam **REST/JSON sobre HTTPS** no API Gateway.
- Dentro da API, os contextos conversam por **eventos de domínio em memória**: o agregado registra o evento (`AddDomainEvent`), o serviço persiste a mudança e só então o `DomainEventDispatcher` entrega o evento aos handlers registrados no DI, na mesma requisição.
- A Lambda e a API **não se chamam**: o contrato entre elas é o JWT, assinado com um segredo compartilhado.

**Prós**
- Um único banco e uma transação por comando: sem consistência eventual entre status, orçamento e estoque.
- Os contextos ficam desacoplados no código: Operação publica `OrdemServicoStatusAlteradoEvent` sem conhecer quem notifica ou mede.
- Zero infraestrutura extra.
- Um trace único no APM mostra a requisição inteira, inclusive os handlers.

**Contras**
- O tempo dos handlers (e-mail SMTP com retry) entra no tempo de resposta da requisição.
- Se o pod cair entre o commit e o envio do e-mail, a notificação se perde, porque o evento não é persistido.

### Opção B — Mensageria assíncrona (SQS/SNS ou EventBridge)

Eventos publicados numa fila e consumidos por workers ou Lambdas.

- **Prós:** desacopla o tempo de resposta do envio de e-mail; retry e DLQ nativos; abre caminho para notificações serverless.
- **Contras:** exige outbox para não perder eventos (a gravação no banco e a publicação na fila não são atômicas); mais componentes e permissões; consistência eventual onde o domínio pede transação (a aprovação do orçamento muda a OS).

### Opção C — Contextos como microsserviços separados, chamadas HTTP entre eles

- **Prós:** deploy e escala independentes por contexto.
- **Contras:** transações distribuídas para operações que hoje são locais; mais pods num cluster com poucos slots; latência e pontos de falha extras, sem demanda que justifique.

## Decisão

**Opção A.**

| Interação | Padrão | Detalhes |
|---|---|---|
| Cliente → autenticação | REST síncrono | `POST /auth*` no API Gateway → Lambda (proxy, payload 2.0) |
| Cliente → API | REST síncrono | `ANY /{proxy+}` → VPC Link → NLB interno → pod; JSON com enums como texto |
| Lambda → API | Sem chamada direta | Contrato por JWT HS256 ([RFC-003](../rfc/RFC-003-estrategia-de-autenticacao.md)) |
| Entre contextos da API | Eventos de domínio em processo | `IDomainEvent` / `IDomainEventHandler<T>` / `DomainEventDispatcher`, disparados depois do commit |
| API → banco, Lambda → banco | SQL síncrono | EF Core (API) e `pg` (Lambda), no mesmo RDS |
| API → SMTP | Síncrono dentro do handler | Polly com até 3 novas tentativas e backoff exponencial; a falha é registrada (`oficina.integracao.falha`) e **não** desfaz a mudança de status |
| Correlação | Header `x-correlation-id` | O gateway injeta o `requestId`; a API grava em todos os logs, devolve na resposta e marca o span; a Lambda grava o mesmo id |

Eventos de domínio usados hoje:

| Evento | Publicado por | Handlers |
|---|---|---|
| `OrdemServicoStatusAlteradoEvent` | `OrdemServico` (toda transição) | `NotificacaoStatusOrdemServicoHandler` (e-mail), `MetricasStatusOrdemServicoHandler` (tempo por status) |
| `OrdemServicoDiagnosticoFinalizadoEvent` | `OrdemServico.FinalizarDiagnostico` | `DiagnosticoFinalizadoHandler` (Atendimento: registra cliente, veículo e orçamento gerado) |
| `OrdemServicoIniciadaEvent` | `OrdemServico.IniciarExecucao` | `OrdemServicoIniciadaHandler` (Catálogo: dá baixa no estoque das peças da OS) |
| `OrcamentoRespondidoEvent` | `Cliente.ResponderOrcamento` (Atendimento) | `OrcamentoRespondidoHandler` (Operação: aprova ou recusa o orçamento e muda a OS) |
| `OrcamentoAprovadoEvent` | `OrdemServico.AprovarOrcamento` | nenhum hoje |
| `OrcamentoRecusadoEvent` | `OrdemServico.RecusarOrcamento` | `NotificacaoStatusOrdemServicoHandler` |
| `EstoqueBaixaRealizadaEvent`, `EstoqueAbaixoMinimoEvent` | `Peca.DarBaixa` | `EstoqueBaixaRealizadaHandler`, `EstoqueAbaixoMinimoHandler` (log e alerta de estoque mínimo) |

## Consequências

- O tempo de resposta das mudanças de status inclui o envio do e-mail. Com SMTP lento, a latência p95 sobe; o monitor de latência e o widget de retentativas de SMTP do dashboard mostram isso.
- **Ponto de atenção encontrado ao documentar:** a baixa de estoque depende do `OrdemServicoIniciadaEvent`, publicado só em `IniciarExecucao` (mudança manual para `EmExecucao`). Quando o próprio cliente aprova o orçamento, `AprovarOrcamento` leva a OS para `EmExecucao` publicando apenas `OrcamentoAprovadoEvent`, que não tem handler; nesse caminho o estoque não recebe baixa. A correção é publicar `OrdemServicoIniciadaEvent` também em `AprovarOrcamento`, ou tratar `OrcamentoAprovadoEvent` no Catálogo.
- Uma notificação pode se perder se o pod morrer entre o commit e o envio. Para a oficina isso é aceitável: o status correto está no banco e o cliente pode consultar.
- **Evolução prevista:** se as notificações precisarem de garantia de entrega ou de outros canais (SMS, WhatsApp), o handler de notificação passa a publicar numa fila (SQS) consumida por uma Lambda, com uma tabela outbox na mesma transação da OS. Os agregados e os outros handlers não mudam, porque já falam por eventos.

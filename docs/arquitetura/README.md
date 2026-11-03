# Documentação da arquitetura

Índice da documentação do sistema da oficina. Fica neste repositório (aplicação principal) e aponta para os documentos dos outros três.

## Visão do sistema

| Documento | Conteúdo |
|---|---|
| [Diagrama de componentes](diagrama-de-componentes.md) | Nuvem, API Gateway, Lambda, EKS, RDS, SSM, CI/CD e Datadog, com fronteiras de rede |
| [Diagramas de sequência](diagramas-de-sequencia.md) | Autenticação por CPF e consumo de rota protegida; abertura da ordem de serviço; ciclo de status até a entrega |
| [Modelo relacional](../banco-de-dados/modelo-relacional.md) | Justificativa do PostgreSQL, diagrama ER, relacionamentos, ajustes feitos e recomendados |

## RFCs (decisões técnicas)

| RFC | Decisão |
|---|---|
| [RFC-001](../rfc/RFC-001-escolha-da-nuvem.md) | AWS (Learner Lab), com as restrições do lab e como foram contornadas |
| [RFC-002](../rfc/RFC-002-banco-de-dados.md) | Amazon RDS for PostgreSQL, comparado com MySQL, SQL Server, Aurora e DynamoDB |
| [RFC-003](../rfc/RFC-003-estrategia-de-autenticacao.md) | Lambda emitindo JWT HS256 a partir do CPF, validação na API e refresh token rotativo |

## ADRs (decisões arquiteturais)

| ADR | Decisão | Repositório |
|---|---|---|
| [ADR-001](../adr/ADR-001-localizacao-manifestos-kubernetes.md) | Manifestos Kubernetes da aplicação ficam com a aplicação; componentes do cluster, no infra-kubernetes | Api / infra-kubernetes |
| [ADR-002](https://github.com/arthur-lucas-castro/TechChallenger-severless/blob/main/docs/adr/ADR-002-localizacao-terraform-lambda-api-gateway.md) | Terraform da Lambda e do API Gateway fica no infra-kubernetes | severless |
| [ADR-003](https://github.com/arthur-lucas-castro/TechChallenger-infra-kubernetes/blob/main/docs/adr/ADR-003-integracao-api-gateway-eks.md) | API Gateway → VPC Link → NLB interno → NodePort | infra-kubernetes |
| [ADR-004](https://github.com/arthur-lucas-castro/TechChallenger-infra-kubernetes/blob/main/docs/adr/ADR-004-observabilidade-datadog.md) | Observabilidade com Datadog sem a integração AWS | infra-kubernetes |
| [ADR-005](../adr/ADR-005-padrao-de-comunicacao.md) | REST síncrono na borda e eventos de domínio em processo entre os contextos | Api |
| [ADR-006](../adr/ADR-006-escalabilidade-com-hpa.md) | HPA por CPU e memória, de 1 a 3 réplicas | Api |

## Documentos por repositório

| Repositório | Documentos |
|---|---|
| [TechChallenger-Api](https://github.com/arthur-lucas-castro/TechChallenger-Api) | [README](../../README.md), [manifestos Kubernetes](../K8S.md), collections Postman ([E2E pelo gateway](../TechChallenger-Gateway-E2E.postman_collection.json), [completa](../TechChallenger.postman_collection.json)), Swagger em `<api_gateway_endpoint>/swagger` |
| [TechChallenger-severless](https://github.com/arthur-lucas-castro/TechChallenger-severless) | [README](https://github.com/arthur-lucas-castro/TechChallenger-severless/blob/main/README.md), [contrato e fluxo de autenticação](https://github.com/arthur-lucas-castro/TechChallenger-severless/blob/main/docs/auth-flow.md) |
| [TechChallenger-infra-kubernetes](https://github.com/arthur-lucas-castro/TechChallenger-infra-kubernetes) | [README](https://github.com/arthur-lucas-castro/TechChallenger-infra-kubernetes/blob/main/README.md): VPC, EKS, API Gateway, Lambda, Datadog, dashboard e alertas |
| [TechChallenger-infra-database](https://github.com/arthur-lucas-castro/TechChallenger-infra-database) | [README](https://github.com/arthur-lucas-castro/TechChallenger-infra-database/blob/main/README.md): RDS PostgreSQL |

## Rastreabilidade com o enunciado da Fase 3

| Requisito | Onde está |
|---|---|
| Diagrama de componentes (nuvem, APIs, banco, monitoramento) | [diagrama-de-componentes.md](diagrama-de-componentes.md) |
| Diagrama de sequência (autenticação e abertura de OS) | [diagramas-de-sequencia.md](diagramas-de-sequencia.md) |
| RFCs (nuvem, banco, autenticação) | [RFC-001](../rfc/RFC-001-escolha-da-nuvem.md), [RFC-002](../rfc/RFC-002-banco-de-dados.md), [RFC-003](../rfc/RFC-003-estrategia-de-autenticacao.md) |
| ADRs (padrão de comunicação, HPA) | [ADR-005](../adr/ADR-005-padrao-de-comunicacao.md), [ADR-006](../adr/ADR-006-escalabilidade-com-hpa.md) e os ADRs 001 a 004 |
| Justificativa do banco, diagrama ER e relacionamentos | [modelo-relacional.md](../banco-de-dados/modelo-relacional.md) |

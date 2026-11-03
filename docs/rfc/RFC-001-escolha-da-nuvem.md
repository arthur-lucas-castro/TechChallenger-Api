# RFC-001 — Escolha do provedor de nuvem

- **Status:** Aceita
- **Data:** 2026-11-03 (registro retroativo da decisão tomada na separação em quatro repositórios, em outubro de 2026)
- **Autor:** Arthur Castro
- **Escopo:** os quatro repositórios

## Resumo

Propõe hospedar toda a solução da oficina na **AWS**, região `us-east-1`, usando a conta do **AWS Academy Learner Lab**: API Gateway, Lambda, EKS, RDS for PostgreSQL, ECR, SSM Parameter Store e S3/DynamoDB para o state do Terraform.

## Contexto

A Fase 3 exige, com livre escolha de nuvem:

- API Gateway para controle e roteamento;
- function serverless para autenticação;
- banco de dados gerenciado;
- cluster Kubernetes com escalabilidade;
- provisionamento com Terraform e deploy automático pelo CI/CD.

As fases anteriores já rodavam a API em Kubernetes com PostgreSQL. O time tem créditos educacionais apenas no AWS Academy, e o orçamento para pagar outra nuvem é zero.

## Requisitos para a decisão

1. Ter todos os serviços gerenciados da lista acima, com provider Terraform maduro.
2. Custo zero para o time durante a fase.
3. Integração com GitHub Actions por credenciais programáticas.
4. Suporte do Datadog para o ambiente (EKS e serverless).

## Alternativas

### A. AWS (AWS Academy Learner Lab)

| Requisito | Serviço |
|---|---|
| API Gateway | API Gateway HTTP API, com VPC Link para o cluster |
| Serverless | Lambda (Node.js 20) |
| Banco gerenciado | RDS for PostgreSQL |
| Kubernetes | EKS com node group gerenciado |
| Registro de imagens | ECR |
| Segredos | SSM Parameter Store (SecureString) |

- **Prós:** créditos já disponíveis; o provider `hashicorp/aws` é o mais usado do Terraform; o Datadog tem extensão oficial para Lambda e chart Helm para EKS; documentação e exemplos abundantes.
- **Contras, todos impostos pelo Learner Lab:**
  - não é possível criar IAM roles: tudo usa a `LabRole` existente;
  - credenciais temporárias que expiram a cada sessão e precisam ser renovadas nos secrets do GitHub;
  - cota de 9 instâncias e 32 vCPUs por região;
  - recursos podem ser desligados no fim da sessão.

### B. Azure (Azure for Students)

AKS, Azure Functions, Azure Database for PostgreSQL Flexible Server e API Management.

- **Prós:** crédito anual de estudante; service principals permitem um CI/CD com identidade própria.
- **Contras:** o time não tem a conta ativa; o API Management no tier Consumption tem limitações de VNet; nenhuma experiência prévia do time.

### C. Google Cloud (trial)

GKE Autopilot, Cloud Functions, Cloud SQL for PostgreSQL e API Gateway.

- **Prós:** GKE Autopilot simplifica o cluster.
- **Contras:** o trial exige cartão de crédito e expira em 90 dias; o API Gateway do GCP não tem integração privada direta com o GKE; nenhuma experiência prévia do time.

## Decisão

**Alternativa A, AWS.** É a única com custo zero garantido para o time e atende todos os requisitos com serviços gerenciados. As restrições do Learner Lab foram tratadas no desenho:

| Restrição | Como foi contornada |
|---|---|
| Sem criação de IAM role | EKS, node group e Lambda usam a `LabRole`; não há AWS Load Balancer Controller (exigiria role própria), por isso o NLB interno é criado pelo Terraform ([ADR-003](https://github.com/arthur-lucas-castro/TechChallenger-infra-kubernetes/blob/main/docs/adr/ADR-003-integracao-api-gateway-eks.md)); o Datadog coleta de dentro dos componentes, sem a integração AWS ([ADR-004](https://github.com/arthur-lucas-castro/TechChallenger-infra-kubernetes/blob/main/docs/adr/ADR-004-observabilidade-datadog.md)) |
| Credenciais temporárias | Ficam nos secrets do environment `aws-deploy` de cada repositório e são renovadas a cada sessão |
| Cota de instâncias | Node group de 1 a 3 nodes `t3.small`; RDS `db.t3.micro` |
| Ambiente efêmero | Toda a infraestrutura é recriável por Terraform; workflows de apply e destroy em cada repositório de infra |

## Consequências

- O SES não foi usado para e-mail: exige sair do sandbox e credenciais SMTP via IAM, indisponíveis no lab. O envio usa SMTP externo, trocável só por configuração.
- O módulo `terraform-aws-modules/eks` não foi usado, porque consulta uma role que o lab bloqueia; o EKS é declarado com recursos diretos.
- Migrar para uma conta AWS comum exige trocar a `LabRole` por roles próprias (IRSA no EKS, role de execução da Lambda) e pode habilitar a integração AWS do Datadog. O resto do Terraform não muda.

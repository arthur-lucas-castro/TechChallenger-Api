# Diagrama de componentes

Visão do sistema da oficina na AWS: borda (API Gateway), computação (EKS e Lambda), dados (RDS), segredos (SSM), entrega (GitHub Actions, ECR e Terraform) e monitoramento (Datadog).

## Visão geral

```mermaid
flowchart LR
    cliente(["Cliente / Atendente<br/>(Postman, app)"])
    smtp[["Provedor SMTP<br/>(Gmail)"]]

    subgraph gh["GitHub"]
        actions["GitHub Actions<br/>CI/CD dos 4 repositórios"]
    end

    subgraph dd["Datadog (SaaS)"]
        ddapm["APM, Logs e Métricas"]
        dddash["Dashboard 'Oficina — Operação'<br/>+ 9 monitores"]
        ddsyn["Synthetics<br/>uptime GET /health"]
    end

    subgraph aws["AWS us-east-1 (AWS Academy Learner Lab)"]
        apigw["API Gateway<br/>HTTP API<br/>throttling + access logs"]
        cw[("CloudWatch Logs<br/>access logs do gateway")]
        ecr[("ECR<br/>imagem da API")]
        ssm[("SSM Parameter Store<br/>banco, JWT, SMTP,<br/>API key Datadog")]
        tfstate[("S3 + DynamoDB<br/>state do Terraform")]

        subgraph vpc["VPC 10.0.0.0/16"]
            nat["NAT Gateway<br/>(subnet pública)"]

            subgraph priv["Subnets privadas"]
                vpclink["VPC Link"]
                nlb["NLB interno<br/>:80 → NodePort 30080"]

                subgraph eks["EKS techchallenger-fase1 (node group t3.small, 1 a 3 nodes)"]
                    api["API da oficina (.NET 8)<br/>Deployment + HPA<br/>Atendimento · Catálogo · Operação"]
                    agent["Datadog Agent (DaemonSet)<br/>+ Cluster Agent"]
                    ms["metrics-server"]
                end

                lambda["Lambda techchallenger-fase1-auth<br/>(Node.js 20)<br/>+ Datadog Extension"]
                rds[("RDS PostgreSQL<br/>db.t3.micro, criptografado,<br/>sem acesso público")]
            end
        end
    end

    cliente -- "HTTPS" --> apigw
    apigw -- "POST /auth, /auth/login,<br/>/auth/refresh, /auth/logout" --> lambda
    apigw -- "ANY /{proxy+}<br/>+ x-correlation-id" --> vpclink --> nlb --> api
    apigw -. "access logs JSON" .-> cw

    lambda -- "SQL :5432" --> rds
    api -- "SQL :5432 (EF Core)" --> rds
    lambda -- "segredos (cold start)" --> nat
    nat --> ssm
    api -- "SMTP 587 (notificações de status)" --> smtp

    api -- "traces, logs, DogStatsD<br/>(sockets unix)" --> agent
    ms -- "CPU/memória" --> api
    agent -- "HTTPS" --> ddapm
    lambda -- "Extension: logs, traces,<br/>métricas (HTTPS via NAT)" --> ddapm
    ddapm --> dddash
    ddsyn -- "a cada 5 min" --> apigw

    actions -- "docker push" --> ecr
    actions -- "kubectl apply (deploy no merge da release)" --> eks
    actions -- "update-function-code" --> lambda
    actions -- "terraform apply" --> tfstate
    actions -- "terraform (observability/)" --> dddash
    ecr -- "pull da imagem" --> api
```

## Componentes

| Componente | Tecnologia | Responsabilidade | Repositório |
|---|---|---|---|
| **API Gateway** | Amazon API Gateway HTTP API (v2), stage `$default` | Porta de entrada única. Roteia `POST /auth*` para a Lambda e o resto (`ANY /{proxy+}`) para a API no EKS via VPC Link. Throttling global (50 req/s, burst 100) e mais restrito nas rotas de autenticação (5 req/s, burst 10). Access logs em JSON. Injeta `x-correlation-id = requestId` | TechChallenger-infra-kubernetes (`api-gateway.tf`) |
| **VPC Link + NLB interno** | API Gateway VPC Link, Network Load Balancer | Ligação privada do gateway com os nodes do EKS (NodePort 30080). A API não tem endereço público | TechChallenger-infra-kubernetes (`nlb.tf`) |
| **Lambda de autenticação** | AWS Lambda, Node.js 20, `pg`, `jsonwebtoken`, `bcryptjs` | Valida CPF, consulta existência e status do cliente, emite JWT (HS256) e refresh token. Login de `Adm`/`Funcionario` por e-mail e senha. Refresh com rotação e logout | TechChallenger-severless (código), TechChallenger-infra-kubernetes (`lambda.tf`) |
| **API da oficina** | ASP.NET Core 8, EF Core 8, DDD | Clientes, veículos, catálogo (serviços, peças, estoque) e ciclo da ordem de serviço. Valida o JWT em todas as rotas, exceto `/health` | TechChallenger-Api |
| **EKS** | Amazon EKS, node group gerenciado (1 a 3 nodes) | Executa a API com HPA, o metrics-server e o agente do Datadog | TechChallenger-infra-kubernetes (`eks.tf`), TechChallenger-Api (`k8s/aws/`) |
| **RDS** | Amazon RDS for PostgreSQL, `db.t3.micro`, gp3 20 a 50 GB | Banco relacional único da oficina, em subnets privadas, acessível só pelos SGs do EKS e da Lambda | TechChallenger-infra-database |
| **SSM Parameter Store** | SecureString / String | Credenciais do banco, segredo do JWT, SMTP e API key do Datadog. Lidos pela Lambda em tempo de execução e pelas pipelines no deploy | TechChallenger-infra-kubernetes (`ssm-parameters.yml`) |
| **ECR** | Amazon ECR | Imagens da API, com tag igual à versão da release | TechChallenger-infra-kubernetes (`ecr.tf`) |
| **Terraform state** | S3 (versionado, criptografado) + DynamoDB (lock) | State de cada repositório de infraestrutura, com key própria | TechChallenger-infra-kubernetes (`bootstrap/`) |
| **GitHub Actions** | Workflows por repositório | CI em todo PR; deploy da API e da Lambda no merge da release na `main`; apply/destroy da infraestrutura | todos |
| **Datadog** | Agent + Cluster Agent (Helm), Lambda Extension, Synthetics, Terraform provider | APM, logs, métricas de negócio (`oficina.*`), dashboard, monitores e uptime | TechChallenger-infra-kubernetes (`datadog.yml`, `observability/`) |
| **Provedor SMTP** | Gmail (senha de app) | Envio dos e-mails de mudança de status da OS | externo |

## Fronteiras de rede e segurança

- **Só o API Gateway é público.** A API está atrás de um NLB interno e o RDS não tem endereço público.
- **RDS:** a porta 5432 aceita conexões só do security group dos nodes do EKS e do SG da Lambda.
- **Saída para a internet** (SSM, Datadog, SMTP) passa pelo NAT Gateway.
- **Segredos** ficam no SSM e entram no cluster como Secret do Kubernetes no deploy. Nenhum segredo fica no repositório ou no state do Terraform.
- **IAM:** todos os componentes usam a `LabRole` existente, porque o Learner Lab não permite criar roles (ver [RFC-001](../rfc/RFC-001-escolha-da-nuvem.md)).

## Documentos relacionados

- [Diagramas de sequência](diagramas-de-sequencia.md): autenticação e abertura de ordem de serviço.
- [Modelo relacional](../banco-de-dados/modelo-relacional.md): banco, diagrama ER e relacionamentos.
- [ADR-003 do TechChallenger-infra-kubernetes](https://github.com/arthur-lucas-castro/TechChallenger-infra-kubernetes/blob/main/docs/adr/ADR-003-integracao-api-gateway-eks.md): por que VPC Link + NLB interno.
- [ADR-004 do TechChallenger-infra-kubernetes](https://github.com/arthur-lucas-castro/TechChallenger-infra-kubernetes/blob/main/docs/adr/ADR-004-observabilidade-datadog.md): estratégia de observabilidade.

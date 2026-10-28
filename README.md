# TechChallenger — API da Oficina Mecânica

API REST para gestão de uma oficina mecânica: atendimento de clientes e veículos, catálogo de peças e serviços, e o ciclo completo de ordens de serviço. Construída em **ASP.NET Core 8** com **Domain-Driven Design (DDD)** e **EF Core 8 + PostgreSQL**, conteinerizada com **Docker** e implantada no **Amazon EKS**.

## Repositórios do projeto

O projeto está dividido em quatro repositórios:

| Repositório | Responsabilidade |
|---|---|
| **TechChallenger-Api** (este) | Código da API, manifestos Kubernetes da aplicação, schema do banco, pipeline de deploy |
| [TechChallenger-infra-kubernetes](https://github.com/arthur-lucas-castro/TechChallenger-infra-kubernetes) | Terraform da VPC, EKS, node group, ECR e componentes do cluster (metrics-server) |
| [TechChallenger-infra-database](https://github.com/arthur-lucas-castro/TechChallenger-infra-database) | Terraform do RDS PostgreSQL |
| [TechChallenger-severless](https://github.com/arthur-lucas-castro/TechChallenger-severless) | Lambda (Node.js) de autenticação por CPF + API Gateway |

**Ordem de provisionamento:** infra-kubernetes → infra-database → severless / Api.

As decisões sobre essa divisão estão registradas em [docs/adr/](docs/adr/).

## Sumário

- [Arquitetura da aplicação](#arquitetura-da-aplicação)
- [Fluxo de deploy](#fluxo-de-deploy)
- [Execução local](#execução-local)
- [Deploy em Kubernetes](#deploy-em-kubernetes)
- [Schema do banco](#schema-do-banco)

## Arquitetura da aplicação

A API segue DDD com bounded contexts isolados, todos sob `src/`:

| Bounded Context | Responsabilidade |
|---|---|
| **Atendimento** | Cadastro de clientes e veículos |
| **Catalogo** | Peças, serviços e controle de estoque |
| **Operacao** | Ordens de serviço, orçamentos e sua máquina de estados |
| **Compartilhado** | Entidade base, value objects comuns (`Dinheiro`, `Email`, `Telefone`…), `AppDbContext`, `BaseRepository<T>`, despacho de domain events e envio de e-mail (MailKit/SMTP) |

Cada contexto segue o fluxo de camadas `Controller → IService → IRepositorio`. As camadas se comunicam sempre via DTOs; entidades de domínio nunca chegam ao controller.

```
┌─────────────────────────────────────────────────────────┐
│                     Presentation                          │
│         Controllers (Atendimento/Catalogo/Operacao)       │
└───────────────────────────┬─────────────────────────────┘
                             │ DTOs
┌───────────────────────────▼─────────────────────────────┐
│                       Application                          │
│           Services · DTOs · Domain Event Handlers          │
└───────────────────────────┬─────────────────────────────┘
                             │ Entidades de domínio
┌───────────────────────────▼─────────────────────────────┐
│                          Domain                             │
│     Aggregate Roots · Value Objects · Domain Events          │
└───────────────────────────┬─────────────────────────────┘
                             │
┌───────────────────────────▼─────────────────────────────┐
│                      Infrastructure                         │
│     Repositórios (EF Core) · AppDbContext · Email (SMTP)    │
└───────────────────────────┬─────────────────────────────┘
                             │
                      PostgreSQL (RDS em produção)
```

Componentes de apoio:

- **Mailpit**: servidor SMTP de testes usado no ambiente local. Captura os e-mails enviados a cada mudança de status da ordem de serviço.
- **Autenticação JWT**: login via `POST /Auth/login`, com senhas em hash BCrypt.

### Notificação por e-mail: por que não Amazon SES

O envio de e-mail (`Compartilhado.Infrastructure.Email`, via MailKit/SMTP) é **agnóstico de provedor**: host, porta, usuário e senha vêm do Secret `techchallenger-api-secret`. O SES não foi usado porque, em produção, ele exige sair do sandbox (caso de suporte ou verificação de domínio) e credenciais SMTP via IAM, e nenhuma das duas coisas é possível no AWS Academy Learner Lab. O secret usa um provedor SMTP externo (Gmail com senha de app). Trocar de provedor é só mudar valores no secret, sem alterar código.

## Fluxo de deploy

Automatizado via GitHub Actions ([.github/workflows/](.github/workflows)):

```
1. Pull Request  ──────────────►  ci-cd.yml: build-and-test
                                   (dotnet build + dotnet test)

2. workflow_dispatch em branch    ci-cd.yml (confirm_deploy=yes)
   release/X.Y.Z          │
                           ├──► build-and-test
                           ├──► build-and-push-image
                           │       dotnet publish → docker build → push no ECR
                           ├──► deploy-k8s
                           │       kubectl apply: configmap, secret, deployment,
                           │       service, hpa → rollout status
                           └──► merge-to-main
                                   PR automático release → main (auto-merge)

  database-migration.yml → aplica database/init/01_schema.sql no RDS via Job k8s
```

### Secrets e variables necessários (environment `aws-deploy`)

| Nome | Origem |
|---|---|
| `AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY`, `AWS_SESSION_TOKEN` | Credenciais do Learner Lab (renovar a cada sessão) |
| `DB_HOST`, `DB_NAME`, `DB_USER`, `DB_PASSWORD` | Outputs do **infra-database** (`rds_address`, `db_name`) e credenciais definidas lá |
| `K8S_SECRET_ENV` | Conteúdo `.env` do Secret da API (ver abaixo) |
| `RELEASE_MERGE_TOKEN` (opcional) | PAT para o auto-merge da release |

O nome do cluster EKS e do repositório ECR não são secrets: ficam fixos no `env` dos workflows (`techchallenger-fase1`, iguais às variáveis `cluster_name` e `ecr_repository_name` do infra-kubernetes). A URL do ECR, que inclui o ID da conta AWS, é descoberta pela pipeline com `aws ecr describe-repositories`.

O Secret `techchallenger-api-secret` é gerado pela pipeline a partir do `K8S_SECRET_ENV`, uma linha `Chave=Valor` por variável:

```
ConnectionStrings__Default=Host=...;Port=5432;Database=...;Username=...;Password=...
Jwt__SecretKey=...
Email__Host=smtp.gmail.com
Email__Username=...
Email__Password=...
```

## Execução local

### Pré-requisitos

- Docker e Docker Compose
- .NET 8 SDK _(opcional, apenas para rodar a API fora do container ou para a análise SonarQube)_

### Stack completa (API + banco + mailpit)

```bash
docker-compose up
```

- API: `http://localhost:5158`
- Swagger UI: `http://localhost:5158/swagger`
- Mailpit: `http://localhost:8025`

### Modo desenvolvimento (API sem Docker)

```bash
docker-compose up postgres mailpit
dotnet run --project src/Presentation/TechChallenger-fase1
```

### Testes automatizados

```bash
dotnet test src/TechChallenger-fase1.sln --filter "FullyQualifiedName~Tests"
```

## Deploy em Kubernetes

Manifestos em [k8s/](k8s). Os detalhes de cada arquivo estão em [docs/K8S.md](docs/K8S.md).

- **`k8s/local/`**: cluster local (Docker Desktop, Minikube ou Kind), com Postgres rodando como pod.
- **`k8s/aws/`**: EKS + RDS, aplicado pela pipeline `ci-cd.yml`.

### Local

```bash
docker build -t techchallenger-fase1:latest .
docker build -t techchallenger-postgres:latest ./database

kubectl apply -f k8s/local/secret.yaml   # não versionado; ver docs/K8S.md
kubectl apply -f k8s/local/configmap.yaml
kubectl apply -f k8s/local/database.yaml
kubectl apply -f k8s/local/deployment.yaml
kubectl apply -f k8s/local/service.yaml
kubectl apply -f k8s/local/mailpit.yaml
```

### AWS (EKS), manual

Pré-requisito: o cluster precisa ter sido criado pelo **infra-kubernetes**, que também instala o metrics-server usado pelo HPA.

```bash
aws eks update-kubeconfig --name techchallenger-fase1 --region us-east-1
ECR_REPOSITORY_URL=$(aws ecr describe-repositories --repository-names techchallenger-fase1 --query 'repositories[0].repositoryUri' --output text)
sed -i "s#IMAGE_PLACEHOLDER#$ECR_REPOSITORY_URL:<tag>#" k8s/aws/deployment.yaml

kubectl apply -f k8s/aws/configmap.yaml
kubectl apply -f k8s/aws/deployment.yaml
kubectl apply -f k8s/aws/service.yaml
kubectl apply -f k8s/aws/hpa.yaml
kubectl rollout status deployment/techchallenger-api --timeout=180s
# endereço público: output api_gateway_endpoint do infra-kubernetes
```

## Schema do banco

O schema fica em [database/init/01_schema.sql](database/init/01_schema.sql), neste repositório, porque evolui junto com as entidades da aplicação. O workflow `database-migration.yml` aplica o schema no RDS: como o banco não tem acesso público, ele roda um Kubernetes Job dentro do cluster.

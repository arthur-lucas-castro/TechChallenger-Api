# Manifestos Kubernetes (`/k8s`)

Este documento descreve o propósito de cada manifesto desta pasta. Os arquivos `.yaml` não contêm mais comentários — todo o racional foi centralizado aqui.

Existem dois conjuntos de manifestos:

- **`k8s/local/*.yaml`** — ambiente local (Docker Desktop / Minikube / Kind), com Postgres rodando como pod dentro do próprio cluster.
- **`k8s/aws/*.yaml`** — ambiente AWS (EKS + RDS), aplicado pela pipeline de CI/CD via `kubectl apply` (job `deploy-k8s`), onde o banco é o RDS gerenciado (não há pod de banco).

## Ambiente local (`k8s/local/`)

### `secret.yaml`

Dados sensíveis lidos pela aplicação: `ConnectionStrings__Default` (string de conexão completa com Postgres local, incluindo senha), `Jwt__SecretKey`, e as credenciais do Postgres (`POSTGRES_USER`, `POSTGRES_PASSWORD`) consumidas pelo pod do banco em `database.yaml`.

### `configmap.yaml`

Variáveis não sensíveis lidas pela aplicação (`Program.cs` / `appsettings.json`): ambiente ASP.NET Core, URLs, configuração de JWT (issuer/audience/expiração) e `POSTGRES_DB`.

Host, porta e nome do banco **não** aparecem espalhados aqui: o app lê uma única `ConnectionStrings__Default` já montada (ex.: `Host=postgres-service;Port=5432;Database=techchallenger;Username=postgres;Password=...`), que contém a senha e por isso fica inteira no Secret, não neste ConfigMap.

`POSTGRES_DB` é a exceção — não é sensível por si só, e é usado tanto pelo Postgres (`POSTGRES_DB`, no pod de `database.yaml`) quanto embutido na connection string do app (no Secret) — fica aqui para os dois consumirem o mesmo valor sem duplicar.

### `database.yaml`

PersistentVolumeClaim + Deployment + Service do Postgres para o ambiente local:

- **PVC** `postgres-pvc` — 1Gi, `ReadWriteOnce`.
- **Deployment** `postgres` — imagem `techchallenger-postgres:latest` (buildada localmente), estratégia `Recreate` (evita dois pods de banco escrevendo no mesmo volume simultaneamente). Recebe `POSTGRES_DB` do ConfigMap e `POSTGRES_USER`/`POSTGRES_PASSWORD` do Secret. A variável `PGDATA` aponta para um subdiretório (`/var/lib/postgresql/data/pgdata`) em vez da raiz do volume, para evitar erro de inicialização em provisioners cujo diretório raiz do volume já vem com arquivos (ex.: `lost+found`). `livenessProbe`/`readinessProbe` usam `pg_isready`.
- **Service** `postgres-service` — `ClusterIP` na porta 5432, usado como host na connection string do Secret da API.

### `deployment.yaml`

Deployment da API para o ambiente local: 2 réplicas, imagem `techchallenger-fase1:latest` (buildada localmente, `imagePullPolicy: IfNotPresent`), variáveis injetadas via `configmap.yaml` + `secret.yaml`, probes de liveness/readiness batendo em `/health` na porta 8080.

### `service.yaml`

Expõe a API localmente via `NodePort` na porta 8080.

## Ambiente AWS (`k8s/aws/`)

### `secret.yaml`

Não há arquivo no repositório: a pipeline gera o Secret `techchallenger-api-secret` com os valores do SSM Parameter Store, publicados pelo infra-kubernetes. É equivalente ao Secret local, mas a `ConnectionStrings__Default` aponta para o endpoint do RDS. Não contém `POSTGRES_USER`/`POSTGRES_PASSWORD` porque não há pod de banco neste ambiente — quem gerencia essas credenciais é o RDS.

### `configmap.yaml`

Mesmas variáveis não sensíveis do ambiente local (ambiente ASP.NET Core, URLs, configuração de JWT), sem `POSTGRES_DB` — o nome do banco já vem embutido na connection string do Secret, e não há pod de Postgres para consumi-lo separadamente.

Inclui também a configuração do tracer Datadog: `DD_LOGS_INJECTION` (põe `dd.trace_id`/`dd.span_id` nos logs, ligando log e trace), `DD_RUNTIME_METRICS_ENABLED` (GC, threads e heap do .NET) e `DD_TRACE_HEADER_TAGS`, que grava o `x-correlation-id` como tag `correlation_id` no span.

### `deployment.yaml`

Deployment da API para o ambiente AWS: 2 réplicas, `image: IMAGE_PLACEHOLDER` (substituído pela pipeline de CI/CD pela URL da imagem no ECR), `imagePullPolicy: Always` (garante que a tag mais recente publicada seja sempre puxada). Mesmas probes de `/health` do ambiente local.

Os `resources.requests`/`limits` (100m/128Mi de request, 250m/256Mi de limit) são mantidos iguais ao ambiente local porque já são enxutos e cabem confortavelmente em um node `t3.micro` (1 vCPU / 1GiB RAM). Referências de dimensionamento para `t3.micro`:

- O allocatable real fica em ~700-800Mi de memória e ~940m de CPU depois do `kube-reserved`/`system-reserved` da AMI do EKS (o restante é consumido por `kubelet`, `kube-proxy`, `aws-node`/VPC CNI, etc.).
- 2 réplicas × 128Mi/100m de request = 256Mi/200m no total, o que cabe em um único `t3.micro` junto com os daemonsets do sistema — mas para ter HA de verdade (sobreviver à perda de 1 node) é necessário ter pelo menos 2 nodes no node group, não só 2 réplicas.
- Os limits (256Mi/250m) evitam que um pod estoure a memória do node sozinho; se no futuro for adicionado HPA ou mais pods no mesmo node, revisar esses números com `kubectl top nodes/pods`.

**Datadog.** Com o agente instalado no cluster (workflow `datadog.yml` do [TechChallenger-infra-kubernetes](https://github.com/arthur-lucas-castro/TechChallenger-infra-kubernetes)), o pod é instrumentado sem mudança no Dockerfile:

- o label `admission.datadoghq.com/enabled: "true"` faz o Cluster Agent injetar o tracer .NET, na versão fixada pela annotation `admission.datadoghq.com/dotnet-lib.version`, e preencher `DD_AGENT_HOST`;
- os labels `tags.datadoghq.com/env|service|version` viram `DD_ENV`, `DD_SERVICE` e `DD_VERSION`. A versão é o `VERSION_PLACEHOLDER`, trocado pela pipeline pela versão da release, como o `IMAGE_PLACEHOLDER`;
- a annotation `ad.datadoghq.com/techchallenger-api.logs` marca os logs com `source:csharp`, que ativa o pipeline de logs C# do Datadog.

Por causa do tracer, a memória subiu para 192Mi de request e 384Mi de limit. Isso exige nodes `t3.small`, que o agente Datadog já exige. Sem o agente no cluster, o label e as annotations não têm efeito.

A API também envia métricas de negócio por DogStatsD ao agente do node (`DD_AGENT_HOST`, UDP 8125). Sem `DD_AGENT_HOST`, como na execução local, elas são descartadas:

| Métrica | Tipo | Tags | Origem |
|---|---|---|---|
| `oficina.os.criada` | count | — | `OrdemServicoService.CriarAsync` |
| `oficina.os.tempo_no_status` | distribution (segundos) | `status` (status que a OS deixou) | `MetricasStatusOrdemServicoHandler`, a cada `OrdemServicoStatusAlteradoEvent` |
| `oficina.api.falha` | count | `controller`, `acao`, `tipo` (`dominio`, `validacao`, `nao_encontrado`, `operacao_invalida`, `erro_interno`) | `ExceptionMiddleware`. Falha de banco (`DbException` em qualquer nível) vira 500 e `erro_interno` |
| `oficina.integracao.falha` | distribution (valor 1; consultar com `count:`) | `integracao` (`smtp`, `postgres`), `operacao` | `NotificacaoStatusOrdemServicoHandler`, quando a notificação da OS não sai. A Lambda de autenticação envia a mesma métrica com `integracao:lambda_auth` |
| `oficina.integracao.retentativa` | count | `integracao`, `operacao` | `SmtpEmailService`, a cada nova tentativa do Polly |

O tempo em cada status é a diferença entre a transição de saída e a de entrada (`DataUltimaAlteracao`, ou `DataCriacao` para `Recebida`). Para o dashboard, Diagnóstico = `status:EmDiagnostico`, Execução = `status:EmExecucao` e Finalização = `status:Finalizada` (da finalização até a entrega).

Os logs da API saem em JSON (Serilog), uma linha por evento, com `timestamp`, `level`, `message`, `logger`, `CorrelationId` e as demais propriedades. O `CorrelationIdMiddleware` reaproveita o `x-correlation-id` enviado pelo API Gateway (o `requestId` dos access logs do gateway), ou gera um novo, e devolve o id no header da resposta.

### `hpa.yaml`

HorizontalPodAutoscaler para o Deployment da API: entre 2 e 5 réplicas, escalando por utilização média de CPU (60%) ou memória (70%). Funciona em conjunto com o `node_group_max_size = 3` do node group (repositório infra-kubernetes), que permite o node group crescer para acomodar as réplicas extras.

### metrics-server (fora deste repositório)

É pré-requisito do HPA: sem ele, os targets ficam em `<unknown>` e o HPA nunca escala. Como é um componente do cluster, e não da aplicação, ele saiu deste repositório e passou a ser instalado pelo workflow de apply do [TechChallenger-infra-kubernetes](https://github.com/arthur-lucas-castro/TechChallenger-infra-kubernetes) (ver [ADR-001](adr/ADR-001-localizacao-manifestos-kubernetes.md)).

### `service.yaml`

Expõe a API como `NodePort` fixo `30080`, **sem endereço público**. A entrada é o API Gateway (HTTP API) do [TechChallenger-infra-kubernetes](https://github.com/arthur-lucas-castro/TechChallenger-infra-kubernetes): gateway → VPC Link → NLB interno (criado pelo Terraform) → nodes na porta `30080` → pods na `8080`. O `nodePort` precisa ser igual ao output `api_node_port` do infra-kubernetes; a decisão está no ADR-003 daquele repositório.

Antes, o Service era `type: LoadBalancer` e criava um NLB público fora do Terraform, o que deixava a API acessível sem passar pelo gateway e impedia o gateway de conhecer o listener do NLB no `plan`.

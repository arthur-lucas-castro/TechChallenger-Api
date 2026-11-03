# ADR-006 — Escalabilidade da API com Horizontal Pod Autoscaler

- **Status:** Aceito
- **Data:** 2026-11-03
- **Repositórios afetados:** TechChallenger-Api (`k8s/aws/hpa.yaml`, `k8s/aws/deployment.yaml`), TechChallenger-infra-kubernetes (node group, metrics-server)

## Contexto

A Fase 3 exige um **cluster Kubernetes com escalabilidade** e alta disponibilidade diante do aumento da base de clientes. A carga da oficina é irregular: picos no início do expediente (abertura de OS) e em consultas de status pelos clientes.

Restrições do ambiente:

- **Learner Lab:** cota de 9 instâncias e 32 vCPUs por região; não é possível criar IAM roles, o que impede o Cluster Autoscaler e o Karpenter (ambos precisam de role própria via IRSA).
- **Limite de pods por node:** o VPC CNI limita os pods pelo número de IPs da instância: 4 pods no `t3.micro`, 11 no `t3.small`.
- **Pods fixos por node:** `aws-node`, `kube-proxy` e o agente do Datadog (DaemonSets), mais CoreDNS, metrics-server, Cluster Agent e operator do Datadog distribuídos no cluster.

## Alternativas consideradas

### Opção A — Réplicas fixas

- **Prós:** simples e previsível.
- **Contras:** paga por capacidade ociosa fora do pico ou fica sem capacidade no pico. Não atende "cluster com escalabilidade".

### Opção B — HPA por CPU e memória (escolhida)

O HPA (`autoscaling/v2`) ajusta o número de pods da API a partir das métricas do metrics-server.

- **Prós:** nativo do Kubernetes, sem role extra; reage em segundos; o Datadog mostra réplicas desejadas e disponíveis no dashboard.
- **Contras:** escala só pods; se os nodes estiverem cheios, as réplicas novas ficam `Pending`.

### Opção C — HPA + Cluster Autoscaler ou Karpenter

- **Prós:** também cria nodes quando faltam slots.
- **Contras:** exige IAM role própria (IRSA), indisponível no Learner Lab.

### Opção D — KEDA, escalando por fila ou por métrica do Datadog

- **Prós:** escala por métricas de negócio (requisições, OS por minuto).
- **Contras:** mais um componente no cluster; a API não consome filas; CPU e memória já representam bem a carga da API.

## Decisão

**Opção B**, com estes parâmetros:

| Parâmetro | Valor | Motivo |
|---|---|---|
| `minReplicas` | 2 | Disponibilidade: a API continua atendendo na perda de um pod ou durante o rolling update |
| `maxReplicas` | 5 | Cabe nos 2 nodes `t3.small` (22 slots de pod) junto com os pods de sistema e do Datadog |
| Métrica de CPU | 60% do request (100m) | Escala antes da saturação; o limit de 250m dá margem para o pico durante a criação do pod novo |
| Métrica de memória | 70% do request (192Mi) | O tracer do Datadog aumenta a memória base; escalar por memória evita OOMKill antes do limit (384Mi) |
| Fonte das métricas | metrics-server, instalado pelo `infra-apply.yml` do TechChallenger-infra-kubernetes | Pré-requisito do HPA; componente do cluster ([ADR-001](ADR-001-localizacao-manifestos-kubernetes.md)) |
| Node group | `t3.small`, mínimo 1, desejado 2, máximo 3 | Capacidade para as 5 réplicas; o crescimento do node group é manual, pelo `node_group_desired_size` |

Disponibilidade:

- O Deployment usa `RollingUpdate` com `maxSurge: 0` e `maxUnavailable: 1`, para não precisar de um slot extra durante o deploy. Como o HPA mantém no mínimo 2 réplicas, o rolling update troca um pod por vez e sempre há pelo menos um pod atendendo.
- As probes de liveness e readiness em `/health` tiram do NLB um pod que não responde.
- O teste de uptime do Datadog e o monitor "API sem réplicas disponíveis" avisam quando o Deployment fica sem pod pronto.

## Consequências

- O HPA reage a CPU e memória, não a requisições por segundo. Uma carga de I/O (consultas lentas ao banco) pode não disparar a escala; nesse caso o sinal é a latência p95 no Datadog.
- **Escala de nodes é manual.** Se as 5 réplicas não couberem (por exemplo, depois de instalar mais componentes no cluster), elas ficam `Pending`, e é preciso aumentar o `node_group_desired_size`.
- O RDS `db.t3.micro` aceita um número limitado de conexões, e cada réplica abre o próprio pool do EF Core.

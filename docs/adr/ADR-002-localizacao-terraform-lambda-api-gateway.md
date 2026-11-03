# ADR-002 — Localização do Terraform da Lambda de autenticação e do API Gateway

- **Status:** Aceito
- **Data:** 2026-10-22
- **Repositórios afetados:** TechChallenger-severless, TechChallenger-infra-kubernetes, TechChallenger-infra-database

## Contexto

A autenticação de clientes por CPF passa a ser uma função serverless em Node.js (AWS Lambda), exposta por um API Gateway. A função:

- recebe o CPF em uma rota HTTP (ex.: `POST /auth`);
- valida o CPF e procura o cliente na tabela `cliente` do PostgreSQL (RDS);
- devolve um token (JWT) que a API principal, no EKS, aceita.

Para chegar ao RDS, que fica em subnets privadas, a Lambda precisa rodar **dentro da VPC**, que é criada pelo TechChallenger-infra-kubernetes, e ter acesso liberado no security group do banco, que é criado pelo TechChallenger-infra-database.

Restrição do ambiente: no AWS Academy Learner Lab não é possível criar IAM roles. A Lambda precisa usar a `LabRole` existente, como o EKS já faz.

É preciso decidir onde fica o Terraform da Lambda, do API Gateway e dos recursos ligados a eles (permissões, log group, security group da função).

## Alternativas consideradas

### Opção A — Terraform dentro de `TechChallenger-severless/infra/`

**Prós**
- Código e infraestrutura da função mudam juntos. Runtime, handler, memória, timeout, variáveis de ambiente e rotas do gateway são alterados no mesmo PR do código que precisa deles.
- O pipeline é único e linear: `test → empacotar .zip → terraform apply`. O `source_code_hash` do `aws_lambda_function` detecta código novo e atualiza a função sem passo extra.
- Não há drift entre um deploy de código (`aws lambda update-function-code`) e o state do Terraform, porque só o Terraform publica a função.
- O repositório é autocontido: dá para entender, testar e implantar a autenticação lendo um único repositório.
- Segue a divisão em 4 repositórios exigida, sem criar um quinto.

**Contras**
- O pipeline do TechChallenger-severless precisa de credenciais que criam infraestrutura, e não só publicam código. No Learner Lab as credenciais são as mesmas de qualquer forma.
- Todo deploy de código roda `terraform plan/apply`. O plan é pequeno, então isso é aceitável.
- Se o API Gateway passar a ser o ponto de entrada também da API do EKS, ele vira um recurso compartilhado e deixa de pertencer só ao TechChallenger-severless.

### Opção B — Terraform em um repositório de infraestrutura separado (`infra-serverless`, ou dentro de TechChallenger-infra-kubernetes)

**Prós**
- As permissões ficam separadas: quem publica código não altera infraestrutura.
- Facilita um API Gateway compartilhado entre a Lambda e a API do EKS.

**Contras**
- O deploy de código precisa de outro mecanismo (`aws lambda update-function-code` ou artefato no S3). Isso cria drift com o state do Terraform e obriga a usar `ignore_changes` em `filename`/`source_code_hash`.
- Uma rota ou variável de ambiente nova exige dois PRs coordenados em dois repositórios.
- Ou se cria um 5º repositório, fora do escopo pedido, ou se coloca serverless dentro do TechChallenger-infra-kubernetes, cujo propósito é o EKS. Isso quebra a coesão daquele repositório.
- A ordem de deploy fica mais frágil: a infraestrutura precisa de um pacote da função para o primeiro `apply`, e o pacote vem de outro repositório.

## Decisão

**O Terraform da Lambda e do API Gateway fica em `TechChallenger-severless/infra/`**, com state próprio no mesmo bucket S3 compartilhado (`key = "lambda-auth/terraform.tfstate"`).

Integração com os outros repositórios, **somente leitura** via `terraform_remote_state`:

| Dado | Origem |
|---|---|
| `vpc_id`, `private_subnet_ids` | outputs do TechChallenger-infra-kubernetes |
| `rds_endpoint`, `db_name`, `rds_client_security_group_id` | outputs do TechChallenger-infra-database |

Para que o TechChallenger-severless não altere recursos de outro repositório, o TechChallenger-infra-database cria e exporta um **security group "cliente do RDS"**, sem regras próprias. O SG do RDS libera a porta 5432 para esse grupo. A Lambda (e qualquer outro consumidor futuro) apenas associa esse SG às suas ENIs.

Recursos que o `TechChallenger-severless/infra/` gerencia: `aws_lambda_function` (role `LabRole`, `vpc_config`), `aws_cloudwatch_log_group`, `aws_apigatewayv2_api` (HTTP API), integração, rota, stage e `aws_lambda_permission`.

## Consequências

- **Ordem de provisionamento:** TechChallenger-infra-kubernetes → TechChallenger-infra-database → TechChallenger-severless.
- O TechChallenger-infra-database precisa de duas mudanças em relação ao `rds.tf` atual:
  1. trocar a referência direta `aws_eks_cluster.this...cluster_security_group_id` por `data.terraform_remote_state.kubernetes.outputs.eks_cluster_security_group_id`;
  2. criar e exportar o SG `rds-client` e liberar o ingress a partir dele.
- O segredo de assinatura do JWT precisa ser o mesmo na Lambda (que emite o token) e no TechChallenger-Api (que valida).

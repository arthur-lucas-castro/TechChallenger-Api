# RFC-002 — Escolha do banco de dados gerenciado

- **Status:** Aceita
- **Data:** 2026-11-03 (registro retroativo da decisão tomada na separação em quatro repositórios, em outubro de 2026)
- **Autor:** Arthur Castro
- **Escopo:** TechChallenger-infra-database, TechChallenger-Api, TechChallenger-severless

## Resumo

Propõe manter o **PostgreSQL**, agora como serviço gerenciado **Amazon RDS for PostgreSQL**, como banco único da oficina, compartilhado pela API (EF Core) e pela Lambda de autenticação (`pg`).

A justificativa detalhada do modelo, o diagrama ER e os ajustes estão em [modelo-relacional.md](../banco-de-dados/modelo-relacional.md).

## Contexto

O domínio da oficina é fortemente relacional:

- uma **ordem de serviço** referencia cliente e veículo, tem serviços e peças solicitados, um orçamento e a execução de cada serviço;
- o **estoque** de peças precisa de baixa consistente (não pode ficar negativo por concorrência);
- mudanças de status e aprovação de orçamento precisam ser **atômicas**: atualizar a OS e o orçamento na mesma transação;
- relatórios (tempo de execução por serviço, OS por status) dependem de **joins e agregações**.

Nas fases 1 e 2 o banco já era PostgreSQL em container, com schema em `database/init/01_schema.sql` e mapeamento pelo EF Core. A Fase 3 exige banco gerenciado e provisionado por Terraform.

## Requisitos para a decisão

1. Transações ACID e integridade referencial (chaves estrangeiras, `ON DELETE CASCADE`).
2. Serviço gerenciado na AWS, com backup, criptografia e acesso apenas pela VPC.
3. Compatível com o código existente (EF Core + Npgsql) e com um driver Node.js leve para a Lambda.
4. Caber no Learner Lab (instância pequena, sem custo extra de licença).

## Alternativas

| Critério | **RDS PostgreSQL** | RDS MySQL | RDS SQL Server | Aurora PostgreSQL | DynamoDB |
|---|---|---|---|---|---|
| Modelo | Relacional | Relacional | Relacional | Relacional | Chave-valor/documento |
| ACID e FKs | Sim | Sim | Sim | Sim | Transações limitadas, sem FKs nem joins |
| Código existente | Sem mudança | Troca de provider, de tipos e do `ENUM` nativo | Troca de provider; licença | Sem mudança | Reescrita da camada de dados |
| Tipos usados hoje | `ENUM` (`status_ordem_servico`), `uuid`, `timestamptz` | `ENUM` diferente, sem `uuid` nativo | Sem `ENUM` | Iguais | n/a |
| Instância/licença | `db.t3.micro` | `db.t3.micro` | Express é gratuita mas limitada (10 GB por banco); as demais edições cobram licença | Classes provisionadas começam em `db.t3.medium` | On-demand |
| Custo no lab | Baixo | Baixo | Alto | Mais alto | Baixo, mas exige remodelar |

## Decisão

**Amazon RDS for PostgreSQL**, configuração atual (TechChallenger-infra-database):

| Item | Valor | Por quê |
|---|---|---|
| Classe | `db.t3.micro` | Carga baixa de uma oficina; cabe na cota do lab |
| Armazenamento | gp3, 20 GB com autoscaling até 50 GB, criptografado | Cresce sem intervenção; criptografia em repouso |
| Rede | Subnets privadas, `publicly_accessible = false` | Só o SG do EKS e o da Lambda alcançam a porta 5432 |
| Backup | Retenção de 7 dias, janela 03:00–04:00 UTC | Recuperação pontual em caso de erro de migração |
| Credenciais | Usuário e senha publicados no SSM | A API e a Lambda leem do SSM; nada fica no repositório |
| Versão do engine | Padrão da AWS no momento do apply | Evita erro com versão descontinuada |

## Consequências

- **Banco único compartilhado por dois serviços.** A Lambda lê `cliente` e `usuario` e é dona das tabelas `refresh_token` e `auth_schema_migration`; a API é dona do resto. Cada lado tem as próprias migrations (`database/init` na API, `db/migrations` no TechChallenger-severless), aplicadas por Jobs no EKS, já que o RDS não é público.
- **Conexões:** a Lambda usa pool de 1 conexão por instância; a concorrência da função limita as conexões abertas no RDS.
- **Single-AZ:** escolha de custo.

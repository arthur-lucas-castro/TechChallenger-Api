CREATE TABLE IF NOT EXISTS Cliente (
    Id               SERIAL PRIMARY KEY,
    Nome             VARCHAR(50)  NOT NULL,
    Sobrenome        VARCHAR(50)  NOT NULL,
    Telefone         VARCHAR(11)  NOT NULL,
    Email            VARCHAR(50)  NOT NULL,
    NumeroDocumento  VARCHAR(14)  NOT NULL,
    TipoPessoa       VARCHAR(1)   NOT NULL
);

CREATE TABLE IF NOT EXISTS Veiculo (
    Id      SERIAL PRIMARY KEY,
    Modelo  VARCHAR(50) NOT NULL,
    Placa   VARCHAR(7)  NOT NULL UNIQUE,
    Marca   VARCHAR(50) NOT NULL,
    Ano     INTEGER     NOT NULL
);

CREATE TABLE IF NOT EXISTS ClienteVeiculo (
    ClienteId  INTEGER NOT NULL REFERENCES Cliente(Id) ON DELETE CASCADE,
    VeiculoId  INTEGER NOT NULL REFERENCES Veiculo(Id) ON DELETE CASCADE,
    PRIMARY KEY (ClienteId, VeiculoId)
);

CREATE TABLE IF NOT EXISTS Servico (
    Id                       SERIAL PRIMARY KEY,
    Nome                     VARCHAR(50)    NOT NULL,
    PrecoVenda               DECIMAL(10, 2) NOT NULL,
    TempoEstimadoEmMinutos   INTEGER        NOT NULL
);

CREATE TABLE IF NOT EXISTS Peca (
    Id          SERIAL PRIMARY KEY,
    Nome        VARCHAR(50)    NOT NULL,
    Descricao   VARCHAR(50)    NOT NULL,
    Custo       DECIMAL(10, 2) NOT NULL,
    PrecoVenda  DECIMAL(10, 2) NOT NULL
);

CREATE TABLE IF NOT EXISTS ProdutoEstoque (
    Id               SERIAL PRIMARY KEY,
    PecaId           INTEGER        NOT NULL REFERENCES Peca(Id) ON DELETE CASCADE,
    QuantidadeAtual  INTEGER        NOT NULL DEFAULT 0,
    QuantidadeMinima INTEGER        NOT NULL,
    PrecoCustoMedio  DECIMAL(10, 2) NOT NULL
);

-- Postgres não suporta "CREATE TYPE IF NOT EXISTS"; o bloco DO abaixo captura
-- o erro "duplicate_object" e ignora, para o script poder rodar mais de uma vez.
DO $$ BEGIN
    CREATE TYPE status_ordem_servico AS ENUM (
        'recebida',
        'em_diagnostico',
        'aguardando_aprovacao',
        'em_execucao',
        'finalizada',
        'entregue'
    );
EXCEPTION
    WHEN duplicate_object THEN NULL;
END $$;

CREATE TABLE IF NOT EXISTS OrdemServico (
    Id                   SERIAL PRIMARY KEY,
    VeiculoId            INTEGER     NOT NULL REFERENCES Veiculo(Id) ON DELETE CASCADE,
    ClienteId            INTEGER     NOT NULL REFERENCES Cliente(Id) ON DELETE CASCADE,
    Status               status_ordem_servico NOT NULL,
    DataUltimaAlteracao  TIMESTAMP,
    DataCriacao          TIMESTAMP   NOT NULL,
    DataFinalizacao      TIMESTAMP
);

CREATE TABLE IF NOT EXISTS ServicoSolicitado (
    Id               SERIAL PRIMARY KEY,
    OrdemServicoId   INTEGER        NOT NULL REFERENCES OrdemServico(Id) ON DELETE CASCADE,
    ServicoId        INTEGER        NOT NULL REFERENCES Servico(Id) ON DELETE CASCADE,
    Quantidade       INTEGER        NOT NULL,
    PrecoVenda       DECIMAL(10, 2) NOT NULL
);


CREATE TABLE IF NOT EXISTS PecaSolicitada (
    Id               SERIAL PRIMARY KEY,
    OrdemServicoId   INTEGER        NOT NULL REFERENCES OrdemServico(Id) ON DELETE CASCADE,
    PecaId           INTEGER        NOT NULL REFERENCES Peca(Id) ON DELETE CASCADE,
    Quantidade       INTEGER        NOT NULL,
    Nome             VARCHAR(100)   NOT NULL,
    PrecoVenda       DECIMAL(10, 2) NOT NULL
);

CREATE TABLE IF NOT EXISTS ServicoExecucao (
    Id                   SERIAL PRIMARY KEY,
    ServicoSolicitadoId  INTEGER      NOT NULL REFERENCES ServicoSolicitado(Id) ON DELETE CASCADE,
    Status               VARCHAR(20)  NOT NULL,
    DataInicio           TIMESTAMP,
    DataFinalizacao      TIMESTAMP
);

CREATE TABLE IF NOT EXISTS Orcamento (
    Id               SERIAL PRIMARY KEY,
    OrdemServicoId   INTEGER        NOT NULL REFERENCES OrdemServico(Id) ON DELETE CASCADE,
    PrecoTotal       DECIMAL(10, 2) NOT NULL,
    Status           VARCHAR(10)    NOT NULL,
    DataCriacao      TIMESTAMP      NOT NULL,
    DataEnvio        TIMESTAMP,
    DataAprovacao    TIMESTAMP
);

CREATE TABLE IF NOT EXISTS usuario (
    id         SERIAL PRIMARY KEY,
    email      VARCHAR(100) NOT NULL UNIQUE,
    senhahash  VARCHAR(72)  NOT NULL,
    tipo       VARCHAR(20)  NOT NULL
);

INSERT INTO usuario (email, senhahash, tipo) VALUES
    ('adm@oficina.com',         '$2a$12$.r/874bZqjmBZNPkYf7el.6c9e6apb39cOUypaELCiKPw78.BSNIK', 'Adm'),
    ('funcionario@oficina.com', '$2a$12$Rs8rAOD6Edl9DDwliF15f.k2HnHGSjEZGJ8RYLce69cEVNNWI4bwe', 'Funcionario')
ON CONFLICT (email) DO NOTHING;

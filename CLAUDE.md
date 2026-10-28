# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Idioma

Responda sempre em português do Brasil (pt-BR).

## Visão Geral do Projeto

**TechChallenger-fase1** é uma API REST ASP.NET Core 8.0 que segue **Domain-Driven Design (DDD)** com bounded contexts. Usa Entity Framework Core 8 com PostgreSQL e é conteinerizada com Docker.

## Estrutura de Pastas

Todo o código-fonte fica em `src/`. Na raiz ficam `CLAUDE.md`, `Dockerfile`, `docker-compose.yml`, `database/` (schema SQL), `k8s/` (`local/` e `aws/`), `docs/` (incluindo `docs/adr/`) e `.github/workflows/`.

Este repositório contém apenas a API. Terraform de VPC/EKS/ECR fica em **TechChallenger-infra-kubernetes**, o do RDS em **TechChallenger-infra-database**, e a Lambda de autenticação em **TechChallenger-severless**. Não adicione Terraform nem componentes de cluster (ex.: metrics-server) aqui (ver `docs/adr/ADR-001`).

```
src/
├── TechChallenger-fase1.sln
├── Presentation/TechChallenger-fase1/        # Entry point — Program.cs, DI
├── Compartilhado/
│   ├── Domain/Entities/                      # EntidadeBase<T>, IAggregateRoot, IDomainEvent*
│   ├── Domain/ValueObjects/                  # Dinheiro, Email, Telefone, TipoPessoa…
│   └── Infrastructure/Repositories/         # AppDbContext, BaseRepository<T>, DomainEventDispatcher
├── [BoundedContext]/                         # Atendimento | Catalogo | Operacao
│   ├── Domain/Entities/                      # Aggregate roots, domain events (Events/)
│   ├── Domain/Interfaces/                    # IXxxRepositorio
│   ├── Domain/ValueObjects/                  # Value objects específicos (projeto separado)
│   ├── Application/DTOs/                     # XxxRequestDTO, XxxResponseDTO
│   ├── Application/Services/                 # XxxService, Interfaces/, Events/
│   ├── Infrastructure/Repositories/          # XxxRepositorio : BaseRepository<T>
│   └── Presentation/Controllers/             # XxxController
└── Testes/TestesDeUnidade/
    └── [BoundedContext].Tests/               # Projetos XUnit por bounded context
```

## Arquitetura e Camadas

### Regra de camadas — OBRIGATÓRIO

- **Controller → IService → IRepositorio**: fluxo de dependência obrigatório, nesta ordem.
- **Controllers nunca acessam repositórios diretamente.** Toda lógica de negócio passa pelo serviço.
- **Controllers nunca conhecem entidades de domínio.** A comunicação entre Controller e Application é feita exclusivamente via DTOs.
- **Sempre use interfaces na injeção de dependência.** Nunca injete implementações concretas.

```
Presentation (Controller)
    ↕ DTOs (XxxRequestDTO / XxxResponseDTO)
Application (IXxxService → XxxService)
    ↕ Entidades de domínio
Infrastructure (IXxxRepositorio → XxxRepositorio)
    ↕ Entidades de domínio
Domain (Entidade / Aggregate Root)
```

### Localização dos artefatos

| Artefato | Local |
|---|---|
| DTOs | `Application/DTOs/` |
| Interface de repositório | `Domain/Interfaces/` |
| Interface de serviço | `Application/Services/Interfaces/` |
| Handler de domain event | `Application/Services/Events/` |
| Domain event | `Domain/Entities/Events/` |
| Value object compartilhado | `Compartilhado/Domain/ValueObjects/` |
| Value object do contexto | `[BoundedContext].Domain.ValueObjects/` (projeto separado) |

O mapeamento entre DTO e entidade é responsabilidade do **serviço**, nunca do controller.

---

## Domain Layer

### Entidades e Aggregate Roots

Toda entidade herda de `EntidadeBase<T>`. Aggregate roots também implementam `IAggregateRoot`.

```csharp
// Aggregate root
public class Peca : EntidadeBase<Peca>, IAggregateRoot
{
    public string Nome { get; set; } = string.Empty;
    public Dinheiro PrecoVenda { get; set; } = null!;
    public ProdutoEstoque? ProdutoEstoque { get; set; }   // navegação interna ao agregado

    public void DarBaixa(int quantidade)                  // operação protegida pelo agregado
    {
        if (ProdutoEstoque is null)
            throw new InvalidOperationException("Peça não possui estoque cadastrado.");
        ProdutoEstoque.DarBaixa(quantidade);
        AddDomainEvent(new EstoqueBaixaRealizadaEvent(Id, Nome,
            ProdutoEstoque.QuantidadeAtual, ProdutoEstoque.QuantidadeMinima));
    }
}

// Entidade interna (não é aggregate root)
public class ProdutoEstoque : EntidadeBase<ProdutoEstoque>
{
    public int PecaId { get; set; }
    public int QuantidadeAtual { get; set; }
    public Dinheiro PrecoCustoMedio { get; set; } = null!;
}
```

**Regras:**
- Somente aggregate roots implementam `IAggregateRoot` e são acessíveis pelo `BaseRepository<T>`.
- Entidades internas ao agregado (ex.: `ProdutoEstoque`) só são acessadas e modificadas pelo aggregate root.
- Domain events são adicionados via `AddDomainEvent()` (herdado de `EntidadeBase<T>`).

### Value Objects

Value objects são `record` com validação no construtor e conversões implícitas de/para primitivo.

```csharp
// Compartilhado/Domain/ValueObjects/Dinheiro.cs
public record Dinheiro
{
    public decimal Valor { get; }

    public Dinheiro(decimal valor)
    {
        if (valor < 0)
            throw new ArgumentException("Valor monetário não pode ser negativo.", nameof(valor));
        Valor = valor;
    }

    public static implicit operator Dinheiro(decimal valor) => new(valor);
    public static implicit operator decimal(Dinheiro dinheiro) => dinheiro.Valor;
    public override string ToString() => Valor.ToString("C2");
}

// [BoundedContext].Domain.ValueObjects/Placa.cs
public record Placa
{
    private static readonly Regex _padraoAntigo   = new(@"^[A-Z]{3}\d{4}$",        RegexOptions.Compiled);
    private static readonly Regex _padraoMercosul = new(@"^[A-Z]{3}\d[A-Z]\d{2}$", RegexOptions.Compiled);

    public string Valor { get; }

    public Placa(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            throw new ArgumentException("Placa não pode ser vazia.", nameof(valor));
        var normalizado = valor.Replace("-", "").ToUpperInvariant();
        if (!_padraoAntigo.IsMatch(normalizado) && !_padraoMercosul.IsMatch(normalizado))
            throw new ArgumentException("Placa inválida.", nameof(valor));
        Valor = normalizado;
    }

    public static implicit operator Placa(string valor) => new(valor);
    public static implicit operator string(Placa placa) => placa.Valor;
    public override string ToString() => Valor;
}
```

**Regras:**
- Value objects compartilhados (usados em múltiplos contextos) ficam em `Compartilhado/Domain/ValueObjects/`.
- Value objects específicos de um contexto ficam em um projeto separado `[BoundedContext].Domain.ValueObjects/`.
- O projeto `Infrastructure/Repositories/` do contexto deve referenciar explicitamente o projeto de value objects (referências de projeto não são transitivas).
- DTOs **não** usam value objects — usam tipos primitivos (`string`, `decimal`, etc.).

### Domain Events

```csharp
// Domain/Entities/Events/EstoqueBaixaRealizadaEvent.cs
public record EstoqueBaixaRealizadaEvent(
    int ProdutoId,
    string NomeProduto,
    int QuantidadeAtual,
    int QuantidadeMinima
) : IDomainEvent;

// Application/Services/Events/EstoqueBaixaRealizadaHandler.cs
public class EstoqueBaixaRealizadaHandler : IDomainEventHandler<EstoqueBaixaRealizadaEvent>
{
    private readonly ILogger<EstoqueBaixaRealizadaHandler> _logger;
    public EstoqueBaixaRealizadaHandler(ILogger<EstoqueBaixaRealizadaHandler> logger) => _logger = logger;

    public Task HandleAsync(EstoqueBaixaRealizadaEvent domainEvent, CancellationToken ct = default)
    {
        _logger.LogInformation("Baixa realizada: {NomeProduto} (Id {ProdutoId}).",
            domainEvent.NomeProduto, domainEvent.ProdutoId);
        return Task.CompletedTask;
    }
}
```

O serviço despacha e limpa eventos após persistir a mutação:
```csharp
await _repositorio.UpdateProdutoEstoqueAsync(estoque);
await _dispatcher.DispatchAsync(estoque.GetDomainEvents());
estoque.ClearDomainEvents();
```

---

## Infrastructure Layer

### BaseRepository\<T\> (EF Core)

`BaseRepository<T>` em `Compartilhado/Infrastructure/Repositories/BaseRepository.cs` fornece CRUD genérico via EF Core. Exige `T : class, IAggregateRoot`.

Campos protegidos disponíveis nas subclasses: `_context` (AppDbContext) e `_dbSet` (DbSet\<T\>).

Métodos disponíveis: `GetByIdAsync`, `GetAllAsync`, `GetByExpressionAsync`, `InsertAsync`, `UpdateAsync`, `DeleteAsync`.

```csharp
// Interface — Domain/Interfaces/IClienteRepositorio.cs
public interface IClienteRepositorio
{
    Task<ClienteEntity?> GetByIdAsync(int id);
    Task<IEnumerable<ClienteEntity>> GetAllAsync();
    Task<int> InsertAsync(ClienteEntity cliente);
    Task<bool> UpdateAsync(ClienteEntity cliente);
    Task<bool> DeleteAsync(int id);
    Task<ClienteEntity?> ObterPorNumeroDocumentoAsync(string numeroDocumento);   // consulta customizada
}

// Implementação — Infrastructure/Repositories/ClienteRepositorio.cs
public class ClienteRepositorio : BaseRepository<ClienteEntity>, IClienteRepositorio
{
    public ClienteRepositorio(AppDbContext context) : base(context) { }

    public async Task<ClienteEntity?> ObterPorNumeroDocumentoAsync(string numeroDocumento)
    {
        Documento doc = numeroDocumento;   // conversão implícita normaliza e valida
        return await _dbSet.FirstOrDefaultAsync(c => c.NumeroDocumento == doc);
    }
}
```

Para consultas com Include (navegação entre entidades do mesmo agregado):
```csharp
public async Task<Peca?> GetByIdComEstoqueAsync(int pecaId)
    => await _context.Set<Peca>()
        .Include(p => p.ProdutoEstoque)
        .FirstOrDefaultAsync(p => p.Id == pecaId);
```

### AppDbContext

Único contexto EF Core, em `Compartilhado/Infrastructure/Repositories/AppDbContext.cs`.

- Todas as conversões de value objects são registradas em `OnModelCreating`.
- Nomes de tabelas e colunas são automaticamente convertidos para minúsculas via `.UseLowerCaseNamingConvention()`.
- Chaves compostas (ex.: `ClienteVeiculo`) são configuradas em `OnModelCreating`.
- Tabelas com nome diferente do padrão EF usam `.ToTable("nomecustomizado")`.

```csharp
modelBuilder.Entity<Veiculo>(b =>
{
    b.Property(x => x.Placa)
        .HasConversion(v => v.Valor, v => new Placa(v))
        .HasMaxLength(7);
});

modelBuilder.Entity<Peca>(b =>
{
    b.Property(x => x.Custo)
        .HasConversion(v => v.Valor, v => new Dinheiro(v))
        .HasColumnType("numeric(10,2)");
});
```

---

## Application Layer

### Serviços

O serviço recebe o repositório (e `IDomainEventDispatcher` se houver domain events) via injeção de dependência, mapeia DTOs ↔ entidades e orquestra as operações.

```csharp
public class ClienteService : IClienteService
{
    private readonly IClienteRepositorio _repositorio;
    public ClienteService(IClienteRepositorio repositorio) => _repositorio = repositorio;

    public async Task<ClienteResponseDTO?> ObterPorIdAsync(int id)
    {
        var c = await _repositorio.GetByIdAsync(id);
        return c is null ? null : MapearParaDTO(c);
    }

    public Task<int> CriarAsync(ClienteRequestDTO dto)
        => _repositorio.InsertAsync(CriarEntidade(dto));

    public async Task<bool> AtualizarAsync(int id, ClienteRequestDTO dto)
    {
        var e = CriarEntidade(dto);
        e.Id = id;
        return await _repositorio.UpdateAsync(e);
    }

    private static ClienteResponseDTO MapearParaDTO(ClienteEntity c) => new()
    {
        Id = c.Id, Nome = c.Nome, NumeroDocumento = c.NumeroDocumento, /* … */
    };

    private static ClienteEntity CriarEntidade(ClienteRequestDTO dto) => new()
    {
        Nome = dto.Nome, NumeroDocumento = dto.NumeroDocumento, /* … */
    };
}
```

### DTOs

- **Request DTOs**: propriedades primitivas (`string`, `int`, `decimal`). Nunca use value objects.
- **Response DTOs**: também primitivos. Value objects serializam via conversão implícita para string/decimal.
- Ficam em `Application/DTOs/`.

---

## Presentation Layer

### Controllers

```csharp
[ApiController]
[Route("[controller]")]
public class VeiculoController : ControllerBase
{
    private readonly IVeiculoService _service;
    public VeiculoController(IVeiculoService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _service.ObterTodosAsync());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var veiculo = await _service.ObterPorIdAsync(id);
        if (veiculo is null) return NotFound();
        return Ok(veiculo);
    }

    [HttpGet("placa/{placa}")]    // rota customizada
    public async Task<IActionResult> GetByPlaca(string placa)
    {
        var veiculo = await _service.ObterPorPlacaAsync(placa);
        if (veiculo is null) return NotFound();
        return Ok(veiculo);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] VeiculoRequestDTO dto)
    {
        var id = await _service.CriarAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] VeiculoRequestDTO dto)
    {
        if (!await _service.AtualizarAsync(id, dto)) return NotFound();
        return Ok(dto);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!await _service.ExcluirAsync(id)) return NotFound();
        return NoContent();
    }
}
```

**Controllers nunca usam try-catch.** Todas as exceções são tratadas pelo middleware de exceções global. Controllers apenas retornam `NotFound()` quando o serviço retorna `null` ou `false`.

---

## Injeção de Dependência (Program.cs)

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default"))
           .UseLowerCaseNamingConvention());

// Por bounded context:
builder.Services.AddScoped<IXxxRepositorio, XxxRepositorio>();
builder.Services.AddScoped<IXxxService, XxxService>();

// Domain event handlers (quando existem domain events):
builder.Services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
builder.Services.AddScoped<IDomainEventHandler<XxxEvent>, XxxHandler>();
```

---

## Adicionando um Novo Bounded Context

### 1. Entidade e Aggregate Root (`Domain/Entities/`)

```csharp
public class SuaEntidade : EntidadeBase<SuaEntidade>, IAggregateRoot
{
    public string Nome { get; set; } = string.Empty;
}
```

### 2. Interface de repositório (`Domain/Interfaces/`)

```csharp
public interface ISuaEntidadeRepositorio
{
    Task<SuaEntidade?> GetByIdAsync(int id);
    Task<IEnumerable<SuaEntidade>> GetAllAsync();
    Task<int> InsertAsync(SuaEntidade entidade);
    Task<bool> UpdateAsync(SuaEntidade entidade);
    Task<bool> DeleteAsync(int id);
}
```

### 3. Repositório (`Infrastructure/Repositories/`)

```csharp
public class SuaEntidadeRepositorio : BaseRepository<SuaEntidade>, ISuaEntidadeRepositorio
{
    public SuaEntidadeRepositorio(AppDbContext context) : base(context) { }
}
```

### 4. Interface de serviço (`Application/Services/Interfaces/`)

```csharp
public interface ISuaEntidadeService
{
    Task<SuaEntidadeResponseDTO?> ObterPorIdAsync(int id);
    Task<IEnumerable<SuaEntidadeResponseDTO>> ObterTodosAsync();
    Task<int> CriarAsync(SuaEntidadeRequestDTO dto);
    Task<bool> AtualizarAsync(int id, SuaEntidadeRequestDTO dto);
    Task<bool> ExcluirAsync(int id);
}
```

### 5. Serviço (`Application/Services/`)

```csharp
public class SuaEntidadeService : ISuaEntidadeService
{
    private readonly ISuaEntidadeRepositorio _repositorio;
    public SuaEntidadeService(ISuaEntidadeRepositorio repositorio) => _repositorio = repositorio;

    public async Task<SuaEntidadeResponseDTO?> ObterPorIdAsync(int id)
    {
        var e = await _repositorio.GetByIdAsync(id);
        return e is null ? null : MapearParaDTO(e);
    }

    public async Task<IEnumerable<SuaEntidadeResponseDTO>> ObterTodosAsync()
        => (await _repositorio.GetAllAsync()).Select(MapearParaDTO);

    public Task<int> CriarAsync(SuaEntidadeRequestDTO dto)
        => _repositorio.InsertAsync(MapearParaEntidade(dto));

    public async Task<bool> AtualizarAsync(int id, SuaEntidadeRequestDTO dto)
    {
        var e = MapearParaEntidade(dto);
        e.Id = id;
        return await _repositorio.UpdateAsync(e);
    }

    public Task<bool> ExcluirAsync(int id) => _repositorio.DeleteAsync(id);

    private static SuaEntidadeResponseDTO MapearParaDTO(SuaEntidade e) => new() { Id = e.Id, Nome = e.Nome };
    private static SuaEntidade MapearParaEntidade(SuaEntidadeRequestDTO dto) => new() { Nome = dto.Nome };
}
```

### 6. Registrar no DI (`Program.cs`)

```csharp
builder.Services.AddScoped<ISuaEntidadeRepositorio, SuaEntidadeRepositorio>();
builder.Services.AddScoped<ISuaEntidadeService, SuaEntidadeService>();
```

### 7. Registrar value object no AppDbContext (`OnModelCreating`)

```csharp
modelBuilder.Entity<SuaEntidade>(b =>
{
    b.Property(x => x.SeuValueObject)
        .HasConversion(v => v.Valor, v => new SeuValueObject(v));
});
```

### 8. Criar tabela (`database/init/`)

```sql
CREATE TABLE suaentidade (
    id   SERIAL PRIMARY KEY,
    nome VARCHAR(100) NOT NULL
);
```

---

## Banco de Dados

PostgreSQL 16 via Docker, porta 5432. Scripts de inicialização em `database/init/`.

Para adicionar tabelas: editar `database/init/01_schema.sql` e recriar o container:
```bash
docker-compose down && docker-compose up database
```

**Convenção EF Core**: todos os nomes de tabela e coluna são gerados em minúsculas via `UseLowerCaseNamingConvention()`. Tabelas com nome diferente do padrão EF usam `.ToTable("nome")` em `OnModelCreating`.

## Comandos de Build, Teste e Execução

Todos os comandos `dotnet` devem apontar para dentro de `src/` (onde está a solução).

```bash
# Build da solução completa
dotnet build src/TechChallenger-fase1.sln

# Executar a API
dotnet run --project src/Presentation/TechChallenger-fase1

# Watch mode
dotnet watch run --project src/Presentation/TechChallenger-fase1

# Rodar todos os testes de unidade
dotnet test src/TechChallenger-fase1.sln --filter "FullyQualifiedName~Tests"

# Rodar testes de um bounded context específico
dotnet test src/Testes/TestesDeUnidade/Atendimento.Tests/Atendimento.Tests.csproj

# Rodar um único teste pelo nome
dotnet test src/Testes/TestesDeUnidade/Atendimento.Tests/Atendimento.Tests.csproj \
  --filter "FullyQualifiedName~NomeDoTeste"

# Apenas banco de dados
docker-compose up database

# Stack completa (API + banco)
docker-compose up
```

Swagger UI disponível em `/swagger` no ambiente de desenvolvimento.

### Projetos de teste

Ficam em `src/Testes/TestesDeUnidade/[BoundedContext].Tests/`. Cada projeto usa **XUnit + Moq**, padrão **AAA**, e referencia apenas os projetos do próprio bounded context (sem acesso à infraestrutura). `GlobalUsings.cs` expõe `global using Xunit;` e `global using Moq;` para todo o projeto de testes.

## Configuração

**`appsettings.json`**:
```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=techchallenger;Username=postgres;Password=postgres"
  }
}
```

No Docker, a connection string é sobrescrita via variável de ambiente:
```
ConnectionStrings__Default=Host=postgres;Port=5432;Database=techchallenger;Username=postgres;Password=postgres
```

## Dependências Principais

- **EF Core 8** + `Npgsql.EntityFrameworkCore.PostgreSQL 8.0` — ORM principal
- **EFCore.NamingConventions 8.0** — `UseLowerCaseNamingConvention()`
- **Swashbuckle.AspNetCore** — Swagger/OpenAPI
- **Dapper** — ainda presente para `ItemServicoRepositorio` (legado, não usar em novos contextos)

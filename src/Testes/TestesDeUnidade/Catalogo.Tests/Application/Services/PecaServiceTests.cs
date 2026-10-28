using Catalogo.Application.DTOs;
using Catalogo.Application.Services;
using Catalogo.Domain.Entities;
using Catalogo.Domain.Interfaces;
using Catalogo.Domain.ValueObjects;
using Compartilhado.Domain.Entities;
using Compartilhado.Domain.ValueObjects;
using System.Linq.Expressions;

namespace Catalogo.Tests.Application.Services;

public class PecaServiceTests
{
    private readonly Mock<IPecaRepositorio> _repositorioMock;
    private readonly Mock<IDomainEventDispatcher> _dispatcherMock;
    private readonly PecaService _service;

    public PecaServiceTests()
    {
        _repositorioMock = new Mock<IPecaRepositorio>();
        _dispatcherMock = new Mock<IDomainEventDispatcher>();
        _service = new PecaService(_repositorioMock.Object, _dispatcherMock.Object);
    }

    private static Peca CriarPeca(int id = 1, ProdutoEstoque? estoque = null) => new()
    {
        Id = id,
        Nome = "Filtro de Óleo",
        Descricao = "Filtro de óleo 1.0",
        Custo = new Dinheiro(8.00m),
        PrecoVenda = new Dinheiro(15.00m),
        ProdutoEstoque = estoque
    };

    private static ProdutoEstoque CriarEstoque(int id = 1, int quantidadeAtual = 10) => new()
    {
        Id = id,
        PecaId = 1,
        QuantidadeAtual = quantidadeAtual,
        QuantidadeMinima = 2,
        PrecoCustoMedio = new Dinheiro(5.00m)
    };

    // --- Queries CRUD ---

    [Fact]
    public async Task ObterPorIdAsync_PecaExiste_RetornaDto()
    {
        // Arrange
        _repositorioMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CriarPeca(1));

        // Act
        var resultado = await _service.ObterPorIdAsync(1);

        // Assert
        Assert.NotNull(resultado);
        Assert.Equal(1, resultado.Id);
        Assert.Equal("Filtro de Óleo", resultado.Nome);
    }

    [Fact]
    public async Task ObterPorIdAsync_PecaNaoExiste_RetornaNull()
    {
        // Arrange
        _repositorioMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Peca?)null);

        // Act
        var resultado = await _service.ObterPorIdAsync(99);

        // Assert
        Assert.Null(resultado);
    }

    [Fact]
    public async Task ObterTodosAsync_RetornaTodosMapeados()
    {
        // Arrange
        var pecas = new List<Peca> { CriarPeca(1), CriarPeca(2) };
        _repositorioMock.Setup(r => r.GetAllAsync()).ReturnsAsync(pecas);

        // Act
        var resultado = (await _service.ObterTodosAsync()).ToList();

        // Assert
        Assert.Equal(2, resultado.Count);
    }

    [Fact]
    public async Task CriarAsync_ChamaInsertComEntidadeCorreta()
    {
        // Arrange
        var dto = new PecaRequestDto
        {
            Nome = "Vela de Ignição", Descricao = "Vela NGK",
            Custo = 12.00m, PrecoVenda = 25.00m
        };
        _repositorioMock
            .Setup(r => r.InsertAsync(It.Is<Peca>(p => p.Nome == "Vela de Ignição")))
            .ReturnsAsync(5);

        // Act
        var id = await _service.CriarAsync(dto);

        // Assert
        Assert.Equal(5, id);
        _repositorioMock.Verify(r => r.InsertAsync(It.Is<Peca>(p => p.Nome == "Vela de Ignição")), Times.Once);
    }

    [Fact]
    public async Task AtualizarAsync_PecaExiste_RetornaTrue()
    {
        // Arrange
        var dto = new PecaRequestDto { Nome = "Filtro", Descricao = "X", Custo = 5m, PrecoVenda = 10m };
        _repositorioMock.Setup(r => r.UpdateAsync(It.IsAny<Peca>())).ReturnsAsync(true);

        // Act
        var resultado = await _service.AtualizarAsync(1, dto);

        // Assert
        Assert.True(resultado);
    }

    [Fact]
    public async Task AtualizarAsync_PecaNaoExiste_RetornaFalse()
    {
        // Arrange
        var dto = new PecaRequestDto { Nome = "Filtro", Descricao = "X", Custo = 5m, PrecoVenda = 10m };
        _repositorioMock.Setup(r => r.UpdateAsync(It.IsAny<Peca>())).ReturnsAsync(false);

        // Act
        var resultado = await _service.AtualizarAsync(99, dto);

        // Assert
        Assert.False(resultado);
    }

    [Fact]
    public async Task ExcluirAsync_DelegaAoRepositorio()
    {
        // Arrange
        _repositorioMock.Setup(r => r.DeleteAsync(1)).ReturnsAsync(true);

        // Act
        var resultado = await _service.ExcluirAsync(1);

        // Assert
        Assert.True(resultado);
        _repositorioMock.Verify(r => r.DeleteAsync(1), Times.Once);
    }

    // --- ObterEstoqueTodosAsync ---

    [Fact]
    public async Task ObterEstoqueTodosAsync_PecaSemEstoque_RetornaNullsNoDto()
    {
        // Arrange — peça sem ProdutoEstoque associado
        var pecas = new List<Peca> { CriarPeca(1, estoque: null) };
        _repositorioMock.Setup(r => r.GetAllComEstoqueAsync()).ReturnsAsync(pecas);

        // Act
        var resultado = (await _service.ObterEstoqueTodosAsync()).ToList();

        // Assert
        Assert.Single(resultado);
        Assert.Null(resultado[0].QuantidadeAtual);
        Assert.Null(resultado[0].QuantidadeMinima);
        Assert.Null(resultado[0].PrecoCustoMedio);
    }

    [Fact]
    public async Task ObterEstoqueTodosAsync_PecaComEstoque_RetornaDadosCorretos()
    {
        // Arrange
        var estoque = CriarEstoque(id: 1, quantidadeAtual: 10);
        var pecas = new List<Peca> { CriarPeca(1, estoque) };
        _repositorioMock.Setup(r => r.GetAllComEstoqueAsync()).ReturnsAsync(pecas);

        // Act
        var resultado = (await _service.ObterEstoqueTodosAsync()).First();

        // Assert
        Assert.Equal(10, resultado.QuantidadeAtual);
        Assert.Equal(2, resultado.QuantidadeMinima);
        Assert.Equal(5.00m, resultado.PrecoCustoMedio);
    }

    // --- AdicionarEstoqueAsync ---

    [Fact]
    public async Task AdicionarEstoqueAsync_PecaNaoEncontrada_LancaKeyNotFoundException()
    {
        // Arrange
        _repositorioMock.Setup(r => r.GetByIdComEstoqueAsync(99)).ReturnsAsync((Peca?)null);
        var dto = new EntradaEstoqueRequestDto { PecaId = 99, Quantidade = 5, PrecoCusto = 10m };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.AdicionarEstoqueAsync(dto));
    }

    [Fact]
    public async Task AdicionarEstoqueAsync_EstoqueNovo_ChamaInsert()
    {
        // Arrange — ProdutoEstoque.Id == 0 indica estoque ainda não persistido
        var peca = CriarPeca(1, estoque: null);
        _repositorioMock.Setup(r => r.GetByIdComEstoqueAsync(1)).ReturnsAsync(peca);
        _repositorioMock.Setup(r => r.InsertProdutoEstoqueAsync(It.IsAny<ProdutoEstoque>()))
            .Returns(Task.CompletedTask);
        var dto = new EntradaEstoqueRequestDto { PecaId = 1, Quantidade = 10, PrecoCusto = 5m };

        // Act
        await _service.AdicionarEstoqueAsync(dto);

        // Assert — novo estoque → Insert, não Update
        _repositorioMock.Verify(r => r.InsertProdutoEstoqueAsync(It.IsAny<ProdutoEstoque>()), Times.Once);
        _repositorioMock.Verify(r => r.UpdateProdutoEstoqueAsync(It.IsAny<ProdutoEstoque>()), Times.Never);
    }

    [Fact]
    public async Task AdicionarEstoqueAsync_EstoqueExistente_ChamaUpdate()
    {
        // Arrange — ProdutoEstoque.Id > 0 indica estoque já persistido
        var estoque = CriarEstoque(id: 5, quantidadeAtual: 10);
        var peca = CriarPeca(1, estoque);
        _repositorioMock.Setup(r => r.GetByIdComEstoqueAsync(1)).ReturnsAsync(peca);
        _repositorioMock.Setup(r => r.UpdateProdutoEstoqueAsync(It.IsAny<ProdutoEstoque>()))
            .ReturnsAsync(true);
        var dto = new EntradaEstoqueRequestDto { PecaId = 1, Quantidade = 5, PrecoCusto = 8m };

        // Act
        await _service.AdicionarEstoqueAsync(dto);

        // Assert — estoque existente → Update, não Insert
        _repositorioMock.Verify(r => r.UpdateProdutoEstoqueAsync(It.IsAny<ProdutoEstoque>()), Times.Once);
        _repositorioMock.Verify(r => r.InsertProdutoEstoqueAsync(It.IsAny<ProdutoEstoque>()), Times.Never);
    }

    [Fact]
    public async Task AdicionarEstoqueAsync_RetornaDTOComDadosCorretos()
    {
        // Arrange
        var peca = CriarPeca(1, estoque: null);
        _repositorioMock.Setup(r => r.GetByIdComEstoqueAsync(1)).ReturnsAsync(peca);
        _repositorioMock.Setup(r => r.InsertProdutoEstoqueAsync(It.IsAny<ProdutoEstoque>()))
            .Returns(Task.CompletedTask);
        var dto = new EntradaEstoqueRequestDto { PecaId = 1, Quantidade = 10, PrecoCusto = 5m };

        // Act
        var resultado = await _service.AdicionarEstoqueAsync(dto);

        // Assert
        Assert.Equal(10, resultado.QuantidadeAtual);
        Assert.Equal(5.00m, resultado.PrecoCustoMedio);
    }

    // --- DarBaixaAsync ---

    [Fact]
    public async Task DarBaixaAsync_PecaNaoEncontrada_RetornaNull()
    {
        // Arrange
        _repositorioMock.Setup(r => r.GetByIdComEstoqueAsync(99)).ReturnsAsync((Peca?)null);
        var dto = new BaixaEstoqueRequestDto { PecaId = 99, Quantidade = 1 };

        // Act
        var resultado = await _service.DarBaixaAsync(dto);

        // Assert
        Assert.Null(resultado);
        _dispatcherMock.Verify(
            d => d.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task DarBaixaAsync_PecaEncontrada_ChamaUpdateEstoque()
    {
        // Arrange
        var estoque = CriarEstoque(id: 1, quantidadeAtual: 10);
        var peca = CriarPeca(1, estoque);
        _repositorioMock.Setup(r => r.GetByIdComEstoqueAsync(1)).ReturnsAsync(peca);
        _repositorioMock.Setup(r => r.UpdateProdutoEstoqueAsync(It.IsAny<ProdutoEstoque>()))
            .ReturnsAsync(true);
        _dispatcherMock.Setup(d => d.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var dto = new BaixaEstoqueRequestDto { PecaId = 1, Quantidade = 3 };

        // Act
        await _service.DarBaixaAsync(dto);

        // Assert
        _repositorioMock.Verify(r => r.UpdateProdutoEstoqueAsync(It.IsAny<ProdutoEstoque>()), Times.Once);
    }

    [Fact]
    public async Task DarBaixaAsync_PecaEncontrada_DispatchaEventos()
    {
        // Arrange
        var estoque = CriarEstoque(id: 1, quantidadeAtual: 10);
        var peca = CriarPeca(1, estoque);
        _repositorioMock.Setup(r => r.GetByIdComEstoqueAsync(1)).ReturnsAsync(peca);
        _repositorioMock.Setup(r => r.UpdateProdutoEstoqueAsync(It.IsAny<ProdutoEstoque>())).ReturnsAsync(true);
        _dispatcherMock.Setup(d => d.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var dto = new BaixaEstoqueRequestDto { PecaId = 1, Quantidade = 3 };

        // Act
        await _service.DarBaixaAsync(dto);

        // Assert
        _dispatcherMock.Verify(
            d => d.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DarBaixaAsync_PecaEncontrada_LimpaEventosAposDispatch()
    {
        // Arrange
        var estoque = CriarEstoque(id: 1, quantidadeAtual: 10);
        var peca = CriarPeca(1, estoque);
        _repositorioMock.Setup(r => r.GetByIdComEstoqueAsync(1)).ReturnsAsync(peca);
        _repositorioMock.Setup(r => r.UpdateProdutoEstoqueAsync(It.IsAny<ProdutoEstoque>())).ReturnsAsync(true);
        _dispatcherMock.Setup(d => d.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var dto = new BaixaEstoqueRequestDto { PecaId = 1, Quantidade = 3 };

        // Act
        await _service.DarBaixaAsync(dto);

        // Assert — eventos limpos após dispatch
        Assert.Empty(peca.GetDomainEvents());
    }

    [Fact]
    public async Task DarBaixaAsync_PecaEncontrada_RetornaDTODoEstoque()
    {
        // Arrange
        var estoque = CriarEstoque(id: 1, quantidadeAtual: 10);
        var peca = CriarPeca(1, estoque);
        _repositorioMock.Setup(r => r.GetByIdComEstoqueAsync(1)).ReturnsAsync(peca);
        _repositorioMock.Setup(r => r.UpdateProdutoEstoqueAsync(It.IsAny<ProdutoEstoque>())).ReturnsAsync(true);
        _dispatcherMock.Setup(d => d.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var dto = new BaixaEstoqueRequestDto { PecaId = 1, Quantidade = 3 };

        // Act
        var resultado = await _service.DarBaixaAsync(dto);

        // Assert — 10 - 3 = 7
        Assert.NotNull(resultado);
        Assert.Equal(7, resultado.QuantidadeAtual);
        Assert.Equal(1, resultado.PecaId);
    }
}

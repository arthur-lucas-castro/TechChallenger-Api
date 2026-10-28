using Atendimento.Application.DTOs;
using Atendimento.Application.Services;
using Atendimento.Domain.Interfaces;
using Atendimento.Domain.ValueObjects;
using Compartilhado.Domain.Entities;
using Compartilhado.Domain.ValueObjects;
using System.Linq.Expressions;
using ClienteEntity = Atendimento.Domain.Entities.Cliente;

namespace Atendimento.Tests.Application.Services;

public class ClienteServiceTests
{
    private readonly Mock<IClienteRepositorio> _repositorioMock;
    private readonly Mock<IDomainEventDispatcher> _dispatcherMock;
    private readonly ClienteService _service;

    public ClienteServiceTests()
    {
        _repositorioMock = new Mock<IClienteRepositorio>();
        _dispatcherMock = new Mock<IDomainEventDispatcher>();
        _service = new ClienteService(_repositorioMock.Object, _dispatcherMock.Object);
    }

    private static ClienteEntity CriarCliente(int id = 1) =>
        new("João", "Silva",
            new Telefone("11987654321"),
            new Email("joao@email.com"),
            new Documento("52998224725"),
            TipoPessoa.F)
        { Id = id };

    // --- Queries ---

    [Fact]
    public async Task ObterPorIdAsync_ClienteExiste_RetornaDto()
    {
        // Arrange
        _repositorioMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CriarCliente(1));

        // Act
        var resultado = await _service.ObterPorIdAsync(1);

        // Assert
        Assert.NotNull(resultado);
        Assert.Equal(1, resultado.Id);
        Assert.Equal("João", resultado.Nome);
        Assert.Equal("Silva", resultado.Sobrenome);
    }

    [Fact]
    public async Task ObterPorIdAsync_ClienteNaoExiste_RetornaNull()
    {
        // Arrange
        _repositorioMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((ClienteEntity?)null);

        // Act
        var resultado = await _service.ObterPorIdAsync(99);

        // Assert
        Assert.Null(resultado);
    }

    [Fact]
    public async Task ObterTodosAsync_RetornaTodosOsClientesMapeados()
    {
        // Arrange
        var clientes = new List<ClienteEntity> { CriarCliente(1), CriarCliente(2) };
        _repositorioMock.Setup(r => r.GetAllAsync()).ReturnsAsync(clientes);

        // Act
        var resultado = (await _service.ObterTodosAsync()).ToList();

        // Assert
        Assert.Equal(2, resultado.Count);
    }

    [Fact]
    public async Task ObterPorIdsAsync_FiltrarPorIds_RetornaSubconjunto()
    {
        // Arrange
        var clientes = new List<ClienteEntity> { CriarCliente(1) };
        _repositorioMock
            .Setup(r => r.GetByExpressionAsync(It.IsAny<Expression<Func<ClienteEntity, bool>>>()))
            .ReturnsAsync(clientes);

        // Act
        var resultado = (await _service.ObterPorIdsAsync([1])).ToList();

        // Assert
        Assert.Single(resultado);
        Assert.Equal(1, resultado[0].Id);
    }

    [Fact]
    public async Task ObterPorNumeroDocumentoAsync_DocumentoExiste_RetornaDto()
    {
        // Arrange
        _repositorioMock
            .Setup(r => r.ObterPorNumeroDocumentoAsync("52998224725"))
            .ReturnsAsync(CriarCliente(1));

        // Act
        var resultado = await _service.ObterPorNumeroDocumentoAsync("52998224725");

        // Assert
        Assert.NotNull(resultado);
        Assert.Equal("João", resultado.Nome);
    }

    [Fact]
    public async Task ObterPorNumeroDocumentoAsync_DocumentoNaoExiste_RetornaNull()
    {
        // Arrange
        _repositorioMock
            .Setup(r => r.ObterPorNumeroDocumentoAsync(It.IsAny<string>()))
            .ReturnsAsync((ClienteEntity?)null);

        // Act
        var resultado = await _service.ObterPorNumeroDocumentoAsync("00000000000");

        // Assert
        Assert.Null(resultado);
    }

    // --- Commands ---

    [Fact]
    public async Task CriarAsync_DeveChamarInsertComEntidadeCorreta()
    {
        // Arrange
        var dto = new ClienteRequestDto
        {
            Nome = "Maria", Sobrenome = "Santos",
            Telefone = "11999998888", Email = "maria@email.com",
            NumeroDocumento = "52998224725", TipoPessoa = TipoPessoa.F
        };
        _repositorioMock
            .Setup(r => r.InsertAsync(It.Is<ClienteEntity>(c => c.Nome == "Maria" && c.Sobrenome == "Santos")))
            .ReturnsAsync(5);

        // Act
        var id = await _service.CriarAsync(dto);

        // Assert
        Assert.Equal(5, id);
        _repositorioMock.Verify(r => r.InsertAsync(It.Is<ClienteEntity>(c => c.Nome == "Maria")), Times.Once);
    }

    [Fact]
    public async Task AtualizarAsync_ClienteExiste_RetornaTrue()
    {
        // Arrange
        var dto = new ClienteRequestDto
        {
            Nome = "João", Sobrenome = "Atualizado",
            Telefone = "11987654321", Email = "joao@email.com",
            NumeroDocumento = "52998224725", TipoPessoa = TipoPessoa.F
        };
        _repositorioMock.Setup(r => r.UpdateAsync(It.IsAny<ClienteEntity>())).ReturnsAsync(true);

        // Act
        var resultado = await _service.AtualizarAsync(1, dto);

        // Assert
        Assert.True(resultado);
    }

    [Fact]
    public async Task AtualizarAsync_ClienteNaoExiste_RetornaFalse()
    {
        // Arrange
        var dto = new ClienteRequestDto
        {
            Nome = "João", Sobrenome = "Silva",
            Telefone = "11987654321", Email = "joao@email.com",
            NumeroDocumento = "52998224725", TipoPessoa = TipoPessoa.F
        };
        _repositorioMock.Setup(r => r.UpdateAsync(It.IsAny<ClienteEntity>())).ReturnsAsync(false);

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

    // --- ResponderOrcamento (fluxo com domain event) ---

    [Fact]
    public async Task ResponderOrcamentoAsync_ClienteNaoExiste_RetornaFalse()
    {
        // Arrange
        _repositorioMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((ClienteEntity?)null);
        var dto = new ResponderOrcamentoDto { Aprovado = true };

        // Act
        var resultado = await _service.ResponderOrcamentoAsync(clienteId: 99, ordemServicoId: 10, dto);

        // Assert
        Assert.False(resultado);
        _dispatcherMock.Verify(
            d => d.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ResponderOrcamentoAsync_ClienteExiste_DispatchaEvento()
    {
        // Arrange
        _repositorioMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CriarCliente(1));
        _dispatcherMock
            .Setup(d => d.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var dto = new ResponderOrcamentoDto { Aprovado = true };

        // Act
        await _service.ResponderOrcamentoAsync(clienteId: 1, ordemServicoId: 10, dto);

        // Assert — dispatcher deve ser chamado exatamente uma vez
        _dispatcherMock.Verify(
            d => d.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ResponderOrcamentoAsync_ClienteExiste_LimpaEventosAposDispatch()
    {
        // Arrange
        var cliente = CriarCliente(1);
        _repositorioMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(cliente);
        _dispatcherMock
            .Setup(d => d.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var dto = new ResponderOrcamentoDto { Aprovado = true };

        // Act
        await _service.ResponderOrcamentoAsync(clienteId: 1, ordemServicoId: 10, dto);

        // Assert — domain events devem ser limpos após o dispatch
        Assert.Empty(cliente.GetDomainEvents());
    }

    [Fact]
    public async Task ResponderOrcamentoAsync_ClienteExiste_RetornaTrue()
    {
        // Arrange
        _repositorioMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CriarCliente(1));
        _dispatcherMock
            .Setup(d => d.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var dto = new ResponderOrcamentoDto { Aprovado = false };

        // Act
        var resultado = await _service.ResponderOrcamentoAsync(clienteId: 1, ordemServicoId: 10, dto);

        // Assert
        Assert.True(resultado);
    }
}

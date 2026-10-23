using Atendimento.Application.Services.Events;
using Atendimento.Domain.Entities;
using Atendimento.Domain.Interfaces;
using Atendimento.Domain.ValueObjects;
using Compartilhado.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Operacao.Domain.Entities.Events;

namespace Atendimento.Tests.Application.Services.Events;

public class DiagnosticoFinalizadoHandlerTests
{
    private readonly Mock<IClienteRepositorio> _clienteRepoMock;
    private readonly Mock<IVeiculoRepositorio> _veiculoRepoMock;
    private readonly Mock<ILogger<DiagnosticoFinalizadoHandler>> _loggerMock;
    private readonly DiagnosticoFinalizadoHandler _handler;

    public DiagnosticoFinalizadoHandlerTests()
    {
        _clienteRepoMock = new Mock<IClienteRepositorio>();
        _veiculoRepoMock = new Mock<IVeiculoRepositorio>();
        _loggerMock = new Mock<ILogger<DiagnosticoFinalizadoHandler>>();
        _handler = new DiagnosticoFinalizadoHandler(
            _clienteRepoMock.Object,
            _veiculoRepoMock.Object,
            _loggerMock.Object);
    }

    private static OrdemServicoDiagnosticoFinalizadoEvent CriarEvento() =>
        new(OrdemServicoId: 1, ClienteId: 10, VeiculoId: 5,
            OrcamentoId: 100, PrecoTotal: 1500.00m,
            DataCriacao: new DateTime(2026, 1, 15, 10, 0, 0));

    private static Cliente CriarCliente(int id = 10) =>
        new("Maria", "Santos",
            new Telefone("11999998888"),
            new Email("maria@email.com"),
            new Documento("52998224725"),
            TipoPessoa.F)
        { Id = id };

    private static Veiculo CriarVeiculo(int id = 5) => new()
    {
        Id = id, Modelo = "Gol", Placa = new Placa("ABC1234"), Marca = "Volkswagen", Ano = 2020
    };

    [Fact]
    public async Task HandleAsync_ClienteEVeiculoExistem_LogaInformacaoCompleta()
    {
        // Arrange
        _clienteRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(CriarCliente());
        _veiculoRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(CriarVeiculo());

        // Act
        await _handler.HandleAsync(CriarEvento());

        // Assert — LogInformation deve ser emitido exatamente uma vez
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ClienteNaoExiste_NaoDeveLancarExcecao()
    {
        // Arrange — cliente retorna null; handler deve usar "desconhecido" como fallback
        _clienteRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Cliente?)null);
        _veiculoRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(CriarVeiculo());

        // Act & Assert — não deve lançar exceção
        await _handler.HandleAsync(CriarEvento());

        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_VeiculoNaoExiste_NaoDeveLancarExcecao()
    {
        // Arrange — veículo retorna null; handler deve usar "desconhecido" como fallback
        _clienteRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(CriarCliente());
        _veiculoRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Veiculo?)null);

        // Act & Assert — não deve lançar exceção
        await _handler.HandleAsync(CriarEvento());

        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}

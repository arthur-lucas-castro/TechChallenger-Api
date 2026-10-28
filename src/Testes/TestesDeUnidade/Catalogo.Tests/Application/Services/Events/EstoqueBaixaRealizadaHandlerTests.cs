using Catalogo.Application.Services.Events;
using Catalogo.Domain.Entities.Events;
using Microsoft.Extensions.Logging;

namespace Catalogo.Tests.Application.Services.Events;

public class EstoqueBaixaRealizadaHandlerTests
{
    private readonly Mock<ILogger<EstoqueBaixaRealizadaHandler>> _loggerMock;
    private readonly EstoqueBaixaRealizadaHandler _handler;

    public EstoqueBaixaRealizadaHandlerTests()
    {
        _loggerMock = new Mock<ILogger<EstoqueBaixaRealizadaHandler>>();
        _handler = new EstoqueBaixaRealizadaHandler(_loggerMock.Object);
    }

    private static EstoqueBaixaRealizadaEvent CriarEvento(
        int produtoId = 1,
        string nomeProduto = "Filtro de óleo",
        int quantidadeAtual = 8,
        int quantidadeMinima = 5)
        => new(produtoId, nomeProduto, quantidadeAtual, quantidadeMinima);

    [Fact]
    public async Task HandleAsync_EventoValido_LogaInformation()
    {
        // Arrange
        var evento = CriarEvento();

        // Act
        await _handler.HandleAsync(evento);

        // Assert
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
    public async Task HandleAsync_EventoValido_NaoLancaExcecao()
    {
        // Arrange
        var evento = CriarEvento();

        // Act & Assert
        var excecao = await Record.ExceptionAsync(() => _handler.HandleAsync(evento));
        Assert.Null(excecao);
    }

    [Fact]
    public async Task HandleAsync_EventoValido_RetornaTaskConcluida()
    {
        // Arrange
        var evento = CriarEvento();

        // Act
        var resultado = _handler.HandleAsync(evento);

        // Assert
        await resultado;
        Assert.True(resultado.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task HandleAsync_ComCancellationToken_NaoLancaExcecao()
    {
        // Arrange
        var evento = CriarEvento();
        using var cts = new CancellationTokenSource();

        // Act & Assert
        var excecao = await Record.ExceptionAsync(() => _handler.HandleAsync(evento, cts.Token));
        Assert.Null(excecao);
    }
}

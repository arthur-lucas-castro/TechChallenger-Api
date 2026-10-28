using Catalogo.Application.Services.Events;
using Catalogo.Domain.Entities.Events;
using Microsoft.Extensions.Logging;

namespace Catalogo.Tests.Application.Services.Events;

public class EstoqueAbaixoMinimoHandlerTests
{
    private readonly Mock<ILogger<EstoqueAbaixoMinimoHandler>> _loggerMock;
    private readonly EstoqueAbaixoMinimoHandler _handler;

    public EstoqueAbaixoMinimoHandlerTests()
    {
        _loggerMock = new Mock<ILogger<EstoqueAbaixoMinimoHandler>>();
        _handler = new EstoqueAbaixoMinimoHandler(_loggerMock.Object);
    }

    private static EstoqueAbaixoMinimoEvent CriarEvento(
        int produtoId = 1,
        string nomeProduto = "Filtro de óleo",
        int quantidadeAtual = 2,
        int quantidadeMinima = 5)
        => new(produtoId, nomeProduto, quantidadeAtual, quantidadeMinima);

    [Fact]
    public async Task HandleAsync_EventoValido_LogaWarning()
    {
        // Arrange
        var evento = CriarEvento();

        // Act
        await _handler.HandleAsync(evento);

        // Assert
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Warning,
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

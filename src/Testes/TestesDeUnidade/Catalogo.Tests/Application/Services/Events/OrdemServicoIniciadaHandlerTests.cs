using Catalogo.Application.DTOs;
using Catalogo.Application.Services.Events;
using Catalogo.Application.Services.Interfaces;
using Compartilhado.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Operacao.Domain.Entities.Events;

namespace Catalogo.Tests.Application.Services.Events;

public class OrdemServicoIniciadaHandlerTests
{
    private readonly Mock<IPecaService> _pecaServiceMock;
    private readonly Mock<ILogger<OrdemServicoIniciadaHandler>> _loggerMock;
    private readonly OrdemServicoIniciadaHandler _handler;

    public OrdemServicoIniciadaHandlerTests()
    {
        _pecaServiceMock = new Mock<IPecaService>();
        _loggerMock = new Mock<ILogger<OrdemServicoIniciadaHandler>>();
        _handler = new OrdemServicoIniciadaHandler(_pecaServiceMock.Object, _loggerMock.Object);
    }

    private static EstoqueResponseDto CriarEstoqueDto() => new()
    {
        Id = 1, PecaId = 1, QuantidadeAtual = 7, QuantidadeMinima = 2, PrecoCustoMedio = 5.00m
    };

    [Fact]
    public async Task HandleAsync_SemPecas_NaoChamaDarBaixa()
    {
        // Arrange
        var evento = new OrdemServicoIniciadaEvent(
            OrdemServicoId: 1,
            Pecas: Array.Empty<PecaOrdemServicoItem>().ToList().AsReadOnly());

        // Act
        await _handler.HandleAsync(evento);

        // Assert
        _pecaServiceMock.Verify(
            s => s.DarBaixaAsync(It.IsAny<BaixaEstoqueRequestDto>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ComUmaPeca_ChamaDarBaixaUmaVez()
    {
        // Arrange
        _pecaServiceMock
            .Setup(s => s.DarBaixaAsync(It.IsAny<BaixaEstoqueRequestDto>()))
            .ReturnsAsync(CriarEstoqueDto());

        var evento = new OrdemServicoIniciadaEvent(
            OrdemServicoId: 1,
            Pecas: new List<PecaOrdemServicoItem> { new(PecaId: 10, Quantidade: 2) }.AsReadOnly());

        // Act
        await _handler.HandleAsync(evento);

        // Assert
        _pecaServiceMock.Verify(
            s => s.DarBaixaAsync(It.Is<BaixaEstoqueRequestDto>(dto =>
                dto.PecaId == 10 && dto.Quantidade == 2)),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ComMultiplasPecas_ChamaDarBaixaParaCada()
    {
        // Arrange
        _pecaServiceMock
            .Setup(s => s.DarBaixaAsync(It.IsAny<BaixaEstoqueRequestDto>()))
            .ReturnsAsync(CriarEstoqueDto());

        var pecas = new List<PecaOrdemServicoItem>
        {
            new(PecaId: 10, Quantidade: 2),
            new(PecaId: 20, Quantidade: 1),
            new(PecaId: 30, Quantidade: 4)
        };
        var evento = new OrdemServicoIniciadaEvent(OrdemServicoId: 5, Pecas: pecas.AsReadOnly());

        // Act
        await _handler.HandleAsync(evento);

        // Assert — uma chamada por peça
        _pecaServiceMock.Verify(
            s => s.DarBaixaAsync(It.IsAny<BaixaEstoqueRequestDto>()),
            Times.Exactly(3));
    }

    [Fact]
    public async Task HandleAsync_SemPecas_NaoDeveLancarExcecao()
    {
        // Arrange
        var evento = new OrdemServicoIniciadaEvent(
            OrdemServicoId: 1,
            Pecas: new List<PecaOrdemServicoItem>().AsReadOnly());

        // Act & Assert — lista vazia não deve quebrar
        var excecao = await Record.ExceptionAsync(() => _handler.HandleAsync(evento));
        Assert.Null(excecao);
    }

    [Fact]
    public async Task HandleAsync_AposProcessar_LogaInformacao()
    {
        // Arrange
        _pecaServiceMock
            .Setup(s => s.DarBaixaAsync(It.IsAny<BaixaEstoqueRequestDto>()))
            .ReturnsAsync(CriarEstoqueDto());

        var evento = new OrdemServicoIniciadaEvent(
            OrdemServicoId: 99,
            Pecas: new List<PecaOrdemServicoItem> { new(PecaId: 1, Quantidade: 1) }.AsReadOnly());

        // Act
        await _handler.HandleAsync(evento);

        // Assert — log emitido após processar todas as peças
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}

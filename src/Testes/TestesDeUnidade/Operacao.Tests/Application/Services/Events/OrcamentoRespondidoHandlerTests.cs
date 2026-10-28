using Atendimento.Domain.Entities.Events;
using Compartilhado.Domain.Entities;
using Compartilhado.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Operacao.Application.Services.Events;
using Operacao.Domain.Entities;
using Operacao.Domain.Interfaces;

namespace Operacao.Tests.Application.Services.Events;

public class OrcamentoRespondidoHandlerTests
{
    private readonly Mock<IOrdemServicoRepositorio> _repositorioMock;
    private readonly Mock<IDomainEventDispatcher> _dispatcherMock;
    private readonly Mock<ILogger<OrcamentoRespondidoHandler>> _loggerMock;
    private readonly OrcamentoRespondidoHandler _handler;

    public OrcamentoRespondidoHandlerTests()
    {
        _repositorioMock = new Mock<IOrdemServicoRepositorio>();
        _dispatcherMock = new Mock<IDomainEventDispatcher>();
        _loggerMock = new Mock<ILogger<OrcamentoRespondidoHandler>>();
        _handler = new OrcamentoRespondidoHandler(_repositorioMock.Object, _dispatcherMock.Object, _loggerMock.Object);
    }

    private static OrdemServico CriarOrdemAguardandoAprovacao(int id = 1)
    {
        var os = new OrdemServico
        {
            Id = id, VeiculoId = 10, ClienteId = 20,
            Status = StatusOrdemServico.Recebida,
            DataCriacao = DateTime.UtcNow
        };
        os.FinalizarDiagnostico();
        os.ClearDomainEvents();
        return os;
    }

    private static OrcamentoRespondidoEvent CriarEvento(bool aprovado, int ordemServicoId = 1) =>
        new(ClienteId: 20, OrdemServicoId: ordemServicoId, Aprovado: aprovado);

    // ── Ordem não encontrada ─────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_OrdemNaoEncontrada_NaoFazCommit()
    {
        // Arrange
        _repositorioMock.Setup(r => r.GetByIdComItensAsync(99)).ReturnsAsync((OrdemServico?)null);

        // Act
        await _handler.HandleAsync(CriarEvento(aprovado: true, ordemServicoId: 99));

        // Assert — early return sem commit
        _repositorioMock.Verify(r => r.CommitAsync(), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_OrdemNaoEncontrada_LogaWarning()
    {
        // Arrange
        _repositorioMock.Setup(r => r.GetByIdComItensAsync(99)).ReturnsAsync((OrdemServico?)null);

        // Act
        await _handler.HandleAsync(CriarEvento(aprovado: true, ordemServicoId: 99));

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

    // ── Orçamento aprovado ───────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_Aprovado_ChamaAprovarOrcamentoECommit()
    {
        // Arrange
        var os = CriarOrdemAguardandoAprovacao(1);
        _repositorioMock.Setup(r => r.GetByIdComItensAsync(1)).ReturnsAsync(os);
        _repositorioMock.Setup(r => r.CommitAsync()).ReturnsAsync(true);

        // Act
        await _handler.HandleAsync(CriarEvento(aprovado: true, ordemServicoId: 1));

        // Assert — AprovarOrcamento() transitiona para EmExecucao
        Assert.Equal(StatusOrdemServico.EmExecucao, os.Status);
        _repositorioMock.Verify(r => r.CommitAsync(), Times.Once);
        _dispatcherMock.Verify(d => d.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Empty(os.GetDomainEvents());
    }

    // ── Orçamento recusado ───────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_Recusado_ChamaRecusarOrcamentoECommit()
    {
        // Arrange
        var os = CriarOrdemAguardandoAprovacao(1);
        _repositorioMock.Setup(r => r.GetByIdComItensAsync(1)).ReturnsAsync(os);
        _repositorioMock.Setup(r => r.CommitAsync()).ReturnsAsync(true);

        // Act
        await _handler.HandleAsync(CriarEvento(aprovado: false, ordemServicoId: 1));

        // Assert — RecusarOrcamento() muda apenas o orçamento, OS permanece AguardandoAprovacao
        Assert.Equal(StatusOrcamento.Recusado, os.Orcamento!.Status);
        Assert.Equal(StatusOrdemServico.AguardandoAprovacao, os.Status);
        _repositorioMock.Verify(r => r.CommitAsync(), Times.Once);
        _dispatcherMock.Verify(d => d.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_Aprovado_LogaInformacao()
    {
        // Arrange
        var os = CriarOrdemAguardandoAprovacao(1);
        _repositorioMock.Setup(r => r.GetByIdComItensAsync(1)).ReturnsAsync(os);
        _repositorioMock.Setup(r => r.CommitAsync()).ReturnsAsync(true);

        // Act
        await _handler.HandleAsync(CriarEvento(aprovado: true, ordemServicoId: 1));

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
}

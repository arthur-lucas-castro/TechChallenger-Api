using Atendimento.Application.Services.Events;
using Atendimento.Domain.Entities;
using Atendimento.Domain.Interfaces;
using Atendimento.Domain.ValueObjects;
using Compartilhado.Application.Services.Interfaces;
using Compartilhado.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Operacao.Domain.Entities.Events;

namespace Atendimento.Tests.Application.Services.Events;

public class NotificacaoStatusOrdemServicoHandlerTests
{
    private readonly Mock<IClienteRepositorio> _clienteRepoMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<ILogger<NotificacaoStatusOrdemServicoHandler>> _loggerMock;
    private readonly NotificacaoStatusOrdemServicoHandler _handler;

    public NotificacaoStatusOrdemServicoHandlerTests()
    {
        _clienteRepoMock = new Mock<IClienteRepositorio>();
        _emailServiceMock = new Mock<IEmailService>();
        _loggerMock = new Mock<ILogger<NotificacaoStatusOrdemServicoHandler>>();
        _handler = new NotificacaoStatusOrdemServicoHandler(
            _clienteRepoMock.Object, _emailServiceMock.Object, _loggerMock.Object);
    }

    private static Cliente CriarCliente(int id = 10) =>
        new("Maria", "Santos",
            new Telefone("11999998888"),
            new Email("maria@email.com"),
            new Documento("52998224725"),
            TipoPessoa.F)
        { Id = id };

    private static OrdemServicoStatusAlteradoEvent CriarEventoStatus(
        StatusOrdemServico anterior, StatusOrdemServico novo, int clienteId = 10, int ordemServicoId = 1) =>
        new(ordemServicoId, clienteId, anterior, novo, DateTime.UtcNow);

    // ── OrdemServicoStatusAlteradoEvent — status com template ────────────────

    [Fact]
    public async Task HandleAsync_StatusComTemplate_EnviaEmailParaClienteCorreto()
    {
        // Arrange
        _clienteRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(CriarCliente());

        // Act
        await _handler.HandleAsync(CriarEventoStatus(StatusOrdemServico.Recebida, StatusOrdemServico.EmDiagnostico));

        // Assert
        _emailServiceMock.Verify(e => e.EnviarAsync(
            "maria@email.com", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(StatusOrdemServico.EmDiagnostico)]
    [InlineData(StatusOrdemServico.AguardandoAprovacao)]
    [InlineData(StatusOrdemServico.EmExecucao)]
    [InlineData(StatusOrdemServico.Finalizada)]
    [InlineData(StatusOrdemServico.Entregue)]
    public async Task HandleAsync_StatusComTemplate_ChamaEnviarAsyncUmaVez(StatusOrdemServico novoStatus)
    {
        // Arrange
        _clienteRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(CriarCliente());

        // Act
        await _handler.HandleAsync(CriarEventoStatus(StatusOrdemServico.Recebida, novoStatus));

        // Assert
        _emailServiceMock.Verify(e => e.EnviarAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── OrdemServicoStatusAlteradoEvent — status sem template ────────────────

    [Fact]
    public async Task HandleAsync_StatusSemTemplate_NaoEnviaEmail()
    {
        // Arrange — Recebida é o estado inicial, sem template de notificação
        _clienteRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(CriarCliente());

        // Act
        await _handler.HandleAsync(CriarEventoStatus(StatusOrdemServico.Recebida, StatusOrdemServico.Recebida));

        // Assert
        _emailServiceMock.Verify(e => e.EnviarAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Cliente não encontrado ────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_ClienteNaoEncontrado_NaoEnviaEmailNemLancaExcecao()
    {
        // Arrange
        _clienteRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Cliente?)null);

        // Act & Assert — não deve lançar exceção
        await _handler.HandleAsync(CriarEventoStatus(StatusOrdemServico.Recebida, StatusOrdemServico.EmDiagnostico));

        _emailServiceMock.Verify(e => e.EnviarAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Falha no envio de e-mail ─────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_EmailServiceLancaExcecao_NaoPropaga()
    {
        // Arrange
        _clienteRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(CriarCliente());
        _emailServiceMock
            .Setup(e => e.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SMTP indisponível"));

        // Act & Assert — a exceção é capturada e logada, nunca propagada
        await _handler.HandleAsync(CriarEventoStatus(StatusOrdemServico.Recebida, StatusOrdemServico.EmDiagnostico));
    }

    // ── OrcamentoRecusadoEvent ───────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_OrcamentoRecusadoEvent_EnviaEmailDeRecusa()
    {
        // Arrange
        _clienteRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(CriarCliente());

        // Act
        await _handler.HandleAsync(new OrcamentoRecusadoEvent(OrdemServicoId: 1, ClienteId: 10, OrcamentoId: 100));

        // Assert
        _emailServiceMock.Verify(e => e.EnviarAsync(
            "maria@email.com", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

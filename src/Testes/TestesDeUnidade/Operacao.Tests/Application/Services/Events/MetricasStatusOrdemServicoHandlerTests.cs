using Compartilhado.Application.Services.Interfaces;
using Compartilhado.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Operacao.Application.Services.Events;
using Operacao.Domain.Entities.Events;

namespace Operacao.Tests.Application.Services.Events;

public class MetricasStatusOrdemServicoHandlerTests
{
    private readonly Mock<IMetricasService> _metricasMock;
    private readonly MetricasStatusOrdemServicoHandler _handler;

    public MetricasStatusOrdemServicoHandlerTests()
    {
        _metricasMock = new Mock<IMetricasService>();
        _handler = new MetricasStatusOrdemServicoHandler(
            _metricasMock.Object, new Mock<ILogger<MetricasStatusOrdemServicoHandler>>().Object);
    }

    private static OrdemServicoStatusAlteradoEvent CriarEvento(
        StatusOrdemServico anterior, StatusOrdemServico novo, DateTime entrada, DateTime alteracao)
        => new(1, 20, anterior, novo, alteracao, entrada);

    [Fact]
    public async Task HandleAsync_RegistraTempoNoStatusAnterior()
    {
        // Arrange
        var entrada = new DateTime(2026, 1, 10, 8, 0, 0, DateTimeKind.Utc);
        var evento = CriarEvento(
            StatusOrdemServico.EmExecucao, StatusOrdemServico.Finalizada, entrada, entrada.AddMinutes(90));

        // Act
        await _handler.HandleAsync(evento);

        // Assert
        _metricasMock.Verify(m => m.TempoNoStatus("EmExecucao", TimeSpan.FromMinutes(90)), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_DuracaoNegativa_NaoRegistraMetrica()
    {
        // Arrange
        var entrada = new DateTime(2026, 1, 10, 8, 0, 0, DateTimeKind.Utc);
        var evento = CriarEvento(
            StatusOrdemServico.EmDiagnostico, StatusOrdemServico.AguardandoAprovacao, entrada, entrada.AddHours(-3));

        // Act
        await _handler.HandleAsync(evento);

        // Assert
        _metricasMock.Verify(m => m.TempoNoStatus(It.IsAny<string>(), It.IsAny<TimeSpan>()), Times.Never);
    }
}

using Compartilhado.Infrastructure.Metricas;
using StatsdClient;

namespace Compartilhado.Tests.Infrastructure;

public class DogStatsdMetricasServiceTests
{
    private readonly Mock<IDogStatsd> _statsdMock = new();
    private readonly DogStatsdMetricasService _service;

    public DogStatsdMetricasServiceTests() => _service = new DogStatsdMetricasService(_statsdMock.Object);

    [Fact]
    public void OrdemServicoCriada_IncrementaContador()
    {
        // Act
        _service.OrdemServicoCriada();

        // Assert
        _statsdMock.Verify(s => s.Increment("os.criada", 1, 1.0, null, null, null), Times.Once);
    }

    [Fact]
    public void TempoNoStatus_EnviaDistribuicaoEmSegundosComTagDeStatus()
    {
        // Act
        _service.TempoNoStatus("EmDiagnostico", TimeSpan.FromMinutes(2));

        // Assert
        _statsdMock.Verify(s => s.Distribution(
            "os.tempo_no_status", 120.0, 1.0,
            It.Is<string[]>(t => t.SequenceEqual(new[] { "status:EmDiagnostico" })), null), Times.Once);
    }

    [Fact]
    public void FalhaRequisicao_IncrementaContadorComTags()
    {
        // Act
        _service.FalhaRequisicao("OrdemServico", "IniciarDiagnostico", "dominio");

        // Assert
        _statsdMock.Verify(s => s.Increment(
            "api.falha", 1, 1.0,
            It.Is<string[]>(t => t.SequenceEqual(new[] { "controller:OrdemServico", "acao:IniciarDiagnostico", "tipo:dominio" })), null, null), Times.Once);
    }
}

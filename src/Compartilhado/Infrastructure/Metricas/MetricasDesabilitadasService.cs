using Compartilhado.Application.Services.Interfaces;

namespace Compartilhado.Infrastructure.Metricas
{
    /// <summary>
    /// Usado quando não há agente Datadog (sem DD_DOGSTATSD_URL nem DD_AGENT_HOST), como na execução local e no docker-compose.
    /// </summary>
    public class MetricasDesabilitadasService : IMetricasService
    {
        public void OrdemServicoCriada() { }

        public void TempoNoStatus(string status, TimeSpan duracao) { }

        public void FalhaRequisicao(string controller, string acao, string tipo) { }

        public void FalhaIntegracao(string integracao, string operacao) { }

        public void RetentativaIntegracao(string integracao, string operacao) { }
    }
}

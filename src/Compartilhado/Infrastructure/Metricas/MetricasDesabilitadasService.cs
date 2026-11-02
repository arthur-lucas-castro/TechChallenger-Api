using Compartilhado.Application.Services.Interfaces;

namespace Compartilhado.Infrastructure.Metricas
{
    /// <summary>
    /// Usado quando não há agente Datadog (DD_AGENT_HOST vazio), como na execução local e no docker-compose.
    /// </summary>
    public class MetricasDesabilitadasService : IMetricasService
    {
        public void OrdemServicoCriada() { }

        public void TempoNoStatus(string status, TimeSpan duracao) { }

        public void FalhaRequisicao(string controller, string acao, string tipo) { }
    }
}

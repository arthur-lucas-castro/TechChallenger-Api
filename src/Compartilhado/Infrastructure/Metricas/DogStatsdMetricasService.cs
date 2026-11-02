using Compartilhado.Application.Services.Interfaces;
using StatsdClient;

namespace Compartilhado.Infrastructure.Metricas
{
    /// <summary>
    /// Envia as métricas por UDP ao agente Datadog do node (DD_AGENT_HOST, porta 8125).
    /// O cliente acrescenta as tags env, service e version a partir de DD_ENV, DD_SERVICE e DD_VERSION.
    /// </summary>
    public class DogStatsdMetricasService : IMetricasService
    {
        public const string Prefixo = "oficina";

        private readonly IDogStatsd _statsd;

        public DogStatsdMetricasService(IDogStatsd statsd) => _statsd = statsd;

        public void OrdemServicoCriada()
            => _statsd.Increment("os.criada");

        public void TempoNoStatus(string status, TimeSpan duracao)
            => _statsd.Distribution("os.tempo_no_status", duracao.TotalSeconds, tags: [$"status:{status}"]);

        public void FalhaRequisicao(string controller, string acao, string tipo)
            => _statsd.Increment("api.falha", tags: [$"controller:{controller}", $"acao:{acao}", $"tipo:{tipo}"]);

        public void FalhaIntegracao(string integracao, string operacao)
            => _statsd.Distribution("integracao.falha", 1, tags: [$"integracao:{integracao}", $"operacao:{operacao}"]);

        public void RetentativaIntegracao(string integracao, string operacao)
            => _statsd.Increment("integracao.retentativa", tags: [$"integracao:{integracao}", $"operacao:{operacao}"]);
    }
}

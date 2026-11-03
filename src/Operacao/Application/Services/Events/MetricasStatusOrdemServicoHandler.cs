using Compartilhado.Application.Services.Interfaces;
using Compartilhado.Domain.Entities;
using Microsoft.Extensions.Logging;
using Operacao.Domain.Entities.Events;

namespace Operacao.Application.Services.Events
{
    /// <summary>
    /// Registra quanto tempo a ordem ficou no status que está deixando. Alimenta o
    /// dashboard de tempo médio por status (Diagnóstico, Execução, Finalização).
    /// </summary>
    public class MetricasStatusOrdemServicoHandler : IDomainEventHandler<OrdemServicoStatusAlteradoEvent>
    {
        private readonly IMetricasService _metricas;
        private readonly ILogger<MetricasStatusOrdemServicoHandler> _logger;

        public MetricasStatusOrdemServicoHandler(IMetricasService metricas, ILogger<MetricasStatusOrdemServicoHandler> logger)
        {
            _metricas = metricas;
            _logger = logger;
        }

        public Task HandleAsync(OrdemServicoStatusAlteradoEvent domainEvent, CancellationToken ct = default)
        {
            var duracao = domainEvent.DataAlteracao - domainEvent.DataEntradaStatusAnterior;

            // Datas antigas gravadas com outro fuso podem gerar duração negativa: ignora em vez de distorcer a média.
            if (duracao < TimeSpan.Zero)
            {
                _logger.LogWarning(
                    "Duração negativa no status {Status} da ordem de serviço {OrdemServicoId}; métrica não registrada.",
                    domainEvent.StatusAnterior, domainEvent.OrdemServicoId);
                return Task.CompletedTask;
            }

            _metricas.TempoNoStatus(domainEvent.StatusAnterior.ToString(), duracao);

            _logger.LogInformation(
                "Ordem de serviço {OrdemServicoId} passou de {StatusAnterior} para {NovoStatus} após {DuracaoSegundos} s.",
                domainEvent.OrdemServicoId, domainEvent.StatusAnterior, domainEvent.NovoStatus, Math.Round(duracao.TotalSeconds, 1));

            return Task.CompletedTask;
        }
    }
}

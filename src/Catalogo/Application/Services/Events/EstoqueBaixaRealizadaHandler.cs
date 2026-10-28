using Compartilhado.Domain.Entities;
using Catalogo.Domain.Entities.Events;
using Microsoft.Extensions.Logging;

namespace Catalogo.Application.Services.Events
{
    public class EstoqueBaixaRealizadaHandler : IDomainEventHandler<EstoqueBaixaRealizadaEvent>
    {
        private readonly ILogger<EstoqueBaixaRealizadaHandler> _logger;

        public EstoqueBaixaRealizadaHandler(ILogger<EstoqueBaixaRealizadaHandler> logger)
            => _logger = logger;

        public Task HandleAsync(EstoqueBaixaRealizadaEvent domainEvent, CancellationToken ct = default)
        {
            _logger.LogInformation(
                "Baixa realizada: Produto {NomeProduto} (Id {ProdutoId}). Estoque atual: {QuantidadeAtual}, mínimo: {QuantidadeMinima}.",
                domainEvent.NomeProduto,
                domainEvent.ProdutoId,
                domainEvent.QuantidadeAtual,
                domainEvent.QuantidadeMinima);

            return Task.CompletedTask;
        }
    }
}

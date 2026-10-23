using Compartilhado.Domain.Entities;
using Catalogo.Domain.Entities.Events;
using Microsoft.Extensions.Logging;

namespace Catalogo.Application.Services.Events
{
    public class EstoqueAbaixoMinimoHandler : IDomainEventHandler<EstoqueAbaixoMinimoEvent>
    {
        private readonly ILogger<EstoqueAbaixoMinimoHandler> _logger;

        public EstoqueAbaixoMinimoHandler(ILogger<EstoqueAbaixoMinimoHandler> logger)
            => _logger = logger;

        public Task HandleAsync(EstoqueAbaixoMinimoEvent domainEvent, CancellationToken ct = default)
        {
            _logger.LogWarning(
                "Estoque abaixo do mínimo: {NomeProduto} (Id {ProdutoId}). Atual: {QuantidadeAtual}, mínimo: {QuantidadeMinima}.",
                domainEvent.NomeProduto,
                domainEvent.ProdutoId,
                domainEvent.QuantidadeAtual,
                domainEvent.QuantidadeMinima);

            return Task.CompletedTask;
        }
    }
}

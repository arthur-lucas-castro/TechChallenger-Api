using Operacao.Domain.Interfaces;
using Atendimento.Domain.Entities.Events;
using Compartilhado.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Operacao.Application.Services.Events
{
    public class OrcamentoRespondidoHandler : IDomainEventHandler<OrcamentoRespondidoEvent>
    {
        private readonly IOrdemServicoRepositorio _repositorio;
        private readonly IDomainEventDispatcher _dispatcher;
        private readonly ILogger<OrcamentoRespondidoHandler> _logger;

        public OrcamentoRespondidoHandler(IOrdemServicoRepositorio repositorio, IDomainEventDispatcher dispatcher, ILogger<OrcamentoRespondidoHandler> logger)
        {
            _repositorio = repositorio;
            _dispatcher = dispatcher;
            _logger = logger;
        }

        public async Task HandleAsync(OrcamentoRespondidoEvent domainEvent, CancellationToken ct = default)
        {
            var ordem = await _repositorio.GetByIdComItensAsync(domainEvent.OrdemServicoId);

            if (ordem is null)
            {
                _logger.LogWarning(
                    "Ordem de serviço {OrdemServicoId} não encontrada ao processar resposta de orçamento do cliente {ClienteId}.",
                    domainEvent.OrdemServicoId, domainEvent.ClienteId);
                return;
            }

            if (domainEvent.Aprovado)
                ordem.AprovarOrcamento();
            else
                ordem.RecusarOrcamento();

            await _repositorio.CommitAsync();
            await _dispatcher.DispatchAsync(ordem.GetDomainEvents());
            ordem.ClearDomainEvents();

            _logger.LogInformation(
                "Orçamento da ordem {OrdemServicoId} {Decisao} pelo cliente {ClienteId}.",
                domainEvent.OrdemServicoId,
                domainEvent.Aprovado ? "aprovado" : "recusado",
                domainEvent.ClienteId);
        }
    }
}

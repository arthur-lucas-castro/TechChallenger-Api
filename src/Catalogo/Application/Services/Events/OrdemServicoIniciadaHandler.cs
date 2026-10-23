using Operacao.Domain.Entities.Events;
using Compartilhado.Domain.Entities;
using Catalogo.Application.DTOs;
using Catalogo.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Catalogo.Application.Services.Events
{
    public class OrdemServicoIniciadaHandler : IDomainEventHandler<OrdemServicoIniciadaEvent>
    {
        private readonly IPecaService _pecaService;
        private readonly ILogger<OrdemServicoIniciadaHandler> _logger;

        public OrdemServicoIniciadaHandler(IPecaService pecaService, ILogger<OrdemServicoIniciadaHandler> logger)
        {
            _pecaService = pecaService;
            _logger = logger;
        }

        public async Task HandleAsync(OrdemServicoIniciadaEvent domainEvent, CancellationToken ct = default)
        {
            foreach (var peca in domainEvent.Pecas)
            {
                await _pecaService.DarBaixaAsync(new BaixaEstoqueRequestDto
                {
                    PecaId = peca.PecaId,
                    Quantidade = peca.Quantidade
                });
            }

            _logger.LogInformation(
                "Baixa de Estoque realizada para {QuantidadePecas} tipo(s) de peça(s) da ordem de serviço {OrdemServicoId}.",
                domainEvent.Pecas.Count,
                domainEvent.OrdemServicoId);
        }
    }
}

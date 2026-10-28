using Operacao.Domain.Entities.Events;
using Atendimento.Domain.Interfaces;
using Compartilhado.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Atendimento.Application.Services.Events
{
    public class DiagnosticoFinalizadoHandler : IDomainEventHandler<OrdemServicoDiagnosticoFinalizadoEvent>
    {
        private readonly IClienteRepositorio _clienteRepositorio;
        private readonly IVeiculoRepositorio _veiculoRepositorio;
        private readonly ILogger<DiagnosticoFinalizadoHandler> _logger;

        public DiagnosticoFinalizadoHandler(
            IClienteRepositorio clienteRepositorio,
            IVeiculoRepositorio veiculoRepositorio,
            ILogger<DiagnosticoFinalizadoHandler> logger)
        {
            _clienteRepositorio = clienteRepositorio;
            _veiculoRepositorio = veiculoRepositorio;
            _logger = logger;
        }

        public async Task HandleAsync(OrdemServicoDiagnosticoFinalizadoEvent domainEvent, CancellationToken ct = default)
        {
            var cliente = await _clienteRepositorio.GetByIdAsync(domainEvent.ClienteId);
            var veiculo = await _veiculoRepositorio.GetByIdAsync(domainEvent.VeiculoId);

            _logger.LogInformation(
                "Diagnóstico finalizado para cliente {ClienteNome} (Id {ClienteId}), veículo {VeiculoMarca} {VeiculoModelo} placa {Placa} (Id {VeiculoId}). " +
                "Orçamento {OrcamentoId} criado em {DataCriacao:dd/MM/yyyy HH:mm} no valor de {PrecoTotal:C2}.",
                cliente?.Nome ?? "desconhecido",
                domainEvent.ClienteId,
                veiculo?.Marca ?? "desconhecido",
                veiculo?.Modelo ?? "desconhecido",
                veiculo?.Placa.Valor ?? "desconhecida",
                domainEvent.VeiculoId,
                domainEvent.OrcamentoId,
                domainEvent.DataCriacao,
                domainEvent.PrecoTotal);
        }
    }
}

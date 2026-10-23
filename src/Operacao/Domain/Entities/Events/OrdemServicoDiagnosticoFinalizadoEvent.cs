using Compartilhado.Domain.Entities;

namespace Operacao.Domain.Entities.Events
{
    public record OrdemServicoDiagnosticoFinalizadoEvent(
        int OrdemServicoId,
        int ClienteId,
        int VeiculoId,
        int OrcamentoId,
        decimal PrecoTotal,
        DateTime DataCriacao
    ) : IDomainEvent;
}

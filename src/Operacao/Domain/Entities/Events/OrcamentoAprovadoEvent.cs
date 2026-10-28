using Compartilhado.Domain.Entities;

namespace Operacao.Domain.Entities.Events
{
    public record OrcamentoAprovadoEvent(
        int OrdemServicoId,
        int ClienteId,
        int OrcamentoId,
        decimal PrecoTotal
    ) : IDomainEvent;
}

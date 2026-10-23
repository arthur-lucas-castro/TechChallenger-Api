using Compartilhado.Domain.Entities;

namespace Operacao.Domain.Entities.Events
{
    public record OrcamentoRecusadoEvent(
        int OrdemServicoId,
        int ClienteId,
        int OrcamentoId
    ) : IDomainEvent;
}

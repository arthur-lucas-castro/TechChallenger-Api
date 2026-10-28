using Compartilhado.Domain.Entities;

namespace Atendimento.Domain.Entities.Events
{
    public record OrcamentoRespondidoEvent(
        int ClienteId,
        int OrdemServicoId,
        bool Aprovado
    ) : IDomainEvent;
}

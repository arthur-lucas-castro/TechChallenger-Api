using Compartilhado.Domain.Entities;
using Compartilhado.Domain.ValueObjects;

namespace Operacao.Domain.Entities.Events
{
    public record OrdemServicoStatusAlteradoEvent(
        int OrdemServicoId,
        int ClienteId,
        StatusOrdemServico StatusAnterior,
        StatusOrdemServico NovoStatus,
        DateTime DataAlteracao
    ) : IDomainEvent;
}

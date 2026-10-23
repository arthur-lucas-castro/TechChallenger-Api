using Compartilhado.Domain.Entities;

namespace Operacao.Domain.Entities.Events
{
    public record PecaOrdemServicoItem(int PecaId, int Quantidade);

    public record OrdemServicoIniciadaEvent(
        int OrdemServicoId,
        IReadOnlyCollection<PecaOrdemServicoItem> Pecas
    ) : IDomainEvent;
}

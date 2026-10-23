using Compartilhado.Domain.Entities;

namespace Catalogo.Domain.Entities.Events
{
    public record EstoqueBaixaRealizadaEvent(
        int ProdutoId,
        string NomeProduto,
        int QuantidadeAtual,
        int QuantidadeMinima
    ) : IDomainEvent;
}

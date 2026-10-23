using Compartilhado.Domain.Entities;
using Compartilhado.Domain.ValueObjects;

namespace Catalogo.Domain.Entities
{
    public class Servico : EntidadeBase, IAggregateRoot
    {
        public string Nome { get; set; } = string.Empty;
        public Dinheiro PrecoVenda { get; set; } = null!;
        public int TempoEstimadoEmMinutos { get; set; }
    }
}

using Atendimento.Domain.ValueObjects;
using Compartilhado.Domain.Entities;

namespace Atendimento.Domain.Entities
{
    public class Veiculo : EntidadeBase, IAggregateRoot
    {
        public string Modelo { get; set; } = string.Empty;
        public Placa Placa { get; set; } = null!;
        public string Marca { get; set; } = string.Empty;
        public int Ano { get; set; }
    }
}

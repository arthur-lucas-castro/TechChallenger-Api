using Compartilhado.Domain.ValueObjects;

namespace Compartilhado.Domain.Entities
{
    public class Usuario : EntidadeBase, IAggregateRoot
    {
        public string Email { get; set; } = string.Empty;
        public string SenhaHash { get; set; } = string.Empty;
        public TipoUsuario Tipo { get; set; }
    }
}

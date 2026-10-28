using Compartilhado.Domain.ValueObjects;

namespace Atendimento.Application.DTOs
{
    public class ClienteRequestDto
    {
        public string Nome { get; set; } = string.Empty;
        public string Sobrenome { get; set; } = string.Empty;
        public string Telefone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string NumeroDocumento { get; set; } = string.Empty;
        public TipoPessoa TipoPessoa { get; set; }
    }
}

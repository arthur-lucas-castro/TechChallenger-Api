using Compartilhado.Domain.Entities;
using Compartilhado.Domain.ValueObjects;
using Atendimento.Domain.ValueObjects;

namespace Atendimento.Domain.Entities
{
    public class Cliente : EntidadeBase, IAggregateRoot
    {
        public string Nome { get; set; } = string.Empty;
        public string Sobrenome { get; set; } = string.Empty;
        public Telefone Telefone { get; set; } = null!;
        public Email Email { get; set; } = null!;
        public Documento NumeroDocumento { get; set; } = null!;
        public TipoPessoa TipoPessoa { get; set; }

        public Cliente() { }
        public Cliente(string nome, string sobrenome, Telefone telefone, Email email,
                       Documento numeroDocumento, TipoPessoa tipoPessoa)
        {
            Nome = nome; Sobrenome = sobrenome; Telefone = telefone;
            Email = email; NumeroDocumento = numeroDocumento; TipoPessoa = tipoPessoa;
        }

        public void ResponderOrcamento(int ordemServicoId, bool aprovado)
        {
            AddDomainEvent(new Events.OrcamentoRespondidoEvent(Id, ordemServicoId, aprovado));
        }
    }
}

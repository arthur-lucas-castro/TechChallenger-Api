using Operacao.Domain.Excecoes;
using Compartilhado.Domain.Entities;
using Compartilhado.Domain.ValueObjects;

namespace Operacao.Domain.Entities
{
    public class Orcamento : EntidadeBase
    {
        public int OrdemServicoId { get; set; }
        public Dinheiro PrecoTotal { get; set; } = null!;
        public StatusOrcamento Status { get; set; }
        public DateTime DataCriacao { get; set; }
        public DateTime? DataEnvio { get; set; }
        public DateTime? DataAprovacao { get; set; }

        public void Aprovar()
        {
            if (Status != StatusOrcamento.Pendente && Status != StatusOrcamento.Enviado)
                throw new TransicaoStatusInvalidaException($"Não é possível aprovar um orçamento com status '{Status}'.");

            Status = StatusOrcamento.Aprovado;
            DataAprovacao = DateTime.UtcNow;
        }

        public void Recusar()
        {
            if (Status != StatusOrcamento.Pendente && Status != StatusOrcamento.Enviado)
                throw new TransicaoStatusInvalidaException($"Não é possível recusar um orçamento com status '{Status}'.");

            Status = StatusOrcamento.Recusado;
        }
    }
}

using Compartilhado.Domain.Entities;
using Compartilhado.Domain.ValueObjects;

namespace Operacao.Domain.Entities
{
    public class ServicoExecucao : EntidadeBase
    {
        public int ServicoSolicitadoId { get; set; }
        public StatusServicoExecucao Status { get; set; }
        public DateTime? DataInicio { get; set; }
        public DateTime? DataFinalizacao { get; set; }
    }
}

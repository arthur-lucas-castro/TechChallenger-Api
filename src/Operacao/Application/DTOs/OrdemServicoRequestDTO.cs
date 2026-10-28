namespace Operacao.Application.DTOs
{
    public class OrdemServicoRequestDto
    {
        public int VeiculoId { get; set; }
        public int ClienteId { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<ServicoSolicitadoRequestDto> Servicos { get; set; } = [];
        public List<PecaSolicitadaRequestDto> Pecas { get; set; } = [];
    }
}

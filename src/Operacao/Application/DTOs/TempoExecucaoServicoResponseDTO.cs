namespace Operacao.Application.DTOs
{
    public class TempoExecucaoServicoResponseDto
    {
        public int ServicoId { get; set; }
        public string NomeServico { get; set; } = string.Empty;
        public double TempoMedioEmMinutos { get; set; }
        public double PiorTempoEmMinutos { get; set; }
    }
}

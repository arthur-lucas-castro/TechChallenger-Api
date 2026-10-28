namespace Catalogo.Application.DTOs
{
    public class ServicoRequestDto
    {
        public string Nome { get; set; } = string.Empty;
        public decimal PrecoVenda { get; set; }
        public int TempoEstimadoEmMinutos { get; set; }
    }
}

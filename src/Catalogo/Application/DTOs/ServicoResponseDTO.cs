namespace Catalogo.Application.DTOs
{
    public class ServicoResponseDto
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public decimal PrecoVenda { get; set; }
        public int TempoEstimadoEmMinutos { get; set; }
    }
}

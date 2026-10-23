namespace Catalogo.Application.DTOs
{
    public class PecaResponseDto
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public decimal Custo { get; set; }
        public decimal PrecoVenda { get; set; }
    }
}

namespace Catalogo.Application.DTOs
{
    public class EstoqueComPecaResponseDto
    {
        public int PecaId { get; set; }
        public string NomePeca { get; set; } = string.Empty;
        public string DescricaoPeca { get; set; } = string.Empty;
        public decimal PrecoVendaPeca { get; set; }
        public int? QuantidadeAtual { get; set; }
        public int? QuantidadeMinima { get; set; }
        public decimal? PrecoCustoMedio { get; set; }
    }
}

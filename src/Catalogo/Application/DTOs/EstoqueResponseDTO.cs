namespace Catalogo.Application.DTOs
{
    public class EstoqueResponseDto
    {
        public int Id { get; set; }
        public int PecaId { get; set; }
        public int QuantidadeAtual { get; set; }
        public int QuantidadeMinima { get; set; }
        public decimal PrecoCustoMedio { get; set; }
    }
}

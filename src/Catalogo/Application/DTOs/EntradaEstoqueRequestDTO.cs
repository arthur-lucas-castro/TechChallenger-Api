namespace Catalogo.Application.DTOs
{
    public class EntradaEstoqueRequestDto
    {
        public int PecaId { get; set; }
        public int Quantidade { get; set; }
        public decimal PrecoCusto { get; set; }
    }
}

using Compartilhado.Domain.ValueObjects;

namespace Catalogo.Domain.ValueObjects
{
    public class ProdutoEstoque 
    {
        public int Id { get; set; }
        public int PecaId { get; set; }
        public int QuantidadeAtual { get; set; }
        public int QuantidadeMinima { get; set; }
        public Dinheiro PrecoCustoMedio { get; set; } = null!;


        public void DarBaixa(int quantidade)
        {
            if (quantidade <= 0)
                throw new ArgumentException("A quantidade para baixa deve ser maior que zero.", nameof(quantidade));
            if (quantidade > QuantidadeAtual)
                throw new InvalidOperationException($"Estoque insuficiente. Disponível: {QuantidadeAtual}, solicitado: {quantidade}.");

            QuantidadeAtual -= quantidade;

        }
    }
}

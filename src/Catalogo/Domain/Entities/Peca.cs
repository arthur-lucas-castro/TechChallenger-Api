using Compartilhado.Domain.Entities;
using Compartilhado.Domain.ValueObjects;
using Catalogo.Domain.Entities.Events;
using Catalogo.Domain.ValueObjects;

namespace Catalogo.Domain.Entities
{
    public class Peca : EntidadeBase, IAggregateRoot
    {
        public string Nome { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public Dinheiro Custo { get; set; } = null!;
        public Dinheiro PrecoVenda { get; set; } = null!;
        public ProdutoEstoque? ProdutoEstoque { get; set; }

        public void AdicionarEstoque(int quantidade, decimal precoCusto)
        {
            if (ProdutoEstoque is null)
            {
                ProdutoEstoque = new ProdutoEstoque
                {
                    PecaId           = Id,
                    QuantidadeAtual  = quantidade,
                    QuantidadeMinima = 0,
                    PrecoCustoMedio  = precoCusto
                };
                return;
            }

            var novoPrecoMedio = (ProdutoEstoque.QuantidadeAtual * (decimal)ProdutoEstoque.PrecoCustoMedio
                                  + quantidade * precoCusto)
                                 / (ProdutoEstoque.QuantidadeAtual + quantidade);

            ProdutoEstoque.QuantidadeAtual += quantidade;
            ProdutoEstoque.PrecoCustoMedio  = Math.Round(novoPrecoMedio, 2);
        }

        public void DarBaixa(int quantidade)
        {
            if (ProdutoEstoque is null)
                throw new InvalidOperationException("Peça não possui Estoque cadastrado.");

            ProdutoEstoque.DarBaixa(quantidade);

            AddDomainEvent(new EstoqueBaixaRealizadaEvent(
               this.Id,
               this.Nome,
               ProdutoEstoque.QuantidadeAtual,
               ProdutoEstoque.QuantidadeMinima
           ));

            if (ProdutoEstoque.QuantidadeAtual < ProdutoEstoque.QuantidadeMinima)
                AddDomainEvent(new EstoqueAbaixoMinimoEvent(
                    this.Id,
                    this.Nome,
                    ProdutoEstoque.QuantidadeAtual,
                    ProdutoEstoque.QuantidadeMinima
                ));
        }
    }
}

using Catalogo.Domain.ValueObjects;
using Compartilhado.Domain.ValueObjects;

namespace Catalogo.Tests.Domain.ValueObjects;

public class ProdutoEstoqueTests
{
    private static ProdutoEstoque CriarEstoque(int quantidadeAtual = 10, int quantidadeMinima = 2) => new()
    {
        Id = 1,
        PecaId = 1,
        QuantidadeAtual = quantidadeAtual,
        QuantidadeMinima = quantidadeMinima,
        PrecoCustoMedio = new Dinheiro(5.00m)
    };

    [Fact]
    public void DarBaixa_QuantidadeValida_DecrementaEstoque()
    {
        // Arrange
        var estoque = CriarEstoque(quantidadeAtual: 10);

        // Act
        estoque.DarBaixa(3);

        // Assert
        Assert.Equal(7, estoque.QuantidadeAtual);
    }

    [Fact]
    public void DarBaixa_QuantidadeZero_DeveLancarArgumentException()
    {
        // Arrange
        var estoque = CriarEstoque();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => estoque.DarBaixa(0));
    }

    [Fact]
    public void DarBaixa_QuantidadeNegativa_DeveLancarArgumentException()
    {
        // Arrange
        var estoque = CriarEstoque();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => estoque.DarBaixa(-1));
    }

    [Fact]
    public void DarBaixa_QuantidadeMaiorQueDisponivel_DeveLancarInvalidOperationException()
    {
        // Arrange
        var estoque = CriarEstoque(quantidadeAtual: 5);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => estoque.DarBaixa(6));
    }

    [Fact]
    public void DarBaixa_QuantidadeExataDoEstoque_ZeraEstoque()
    {
        // Arrange
        var estoque = CriarEstoque(quantidadeAtual: 5);

        // Act
        estoque.DarBaixa(5);

        // Assert
        Assert.Equal(0, estoque.QuantidadeAtual);
    }
}

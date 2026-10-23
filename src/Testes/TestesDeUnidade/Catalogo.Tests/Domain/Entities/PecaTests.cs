using Catalogo.Domain.Entities;
using Catalogo.Domain.Entities.Events;
using Catalogo.Domain.ValueObjects;
using Compartilhado.Domain.ValueObjects;

namespace Catalogo.Tests.Domain.Entities;

public class PecaTests
{
    private static Peca CriarPeca(ProdutoEstoque? estoque = null) => new()
    {
        Id = 1,
        Nome = "Filtro de Óleo",
        Descricao = "Filtro de óleo para motor 1.0",
        Custo = new Dinheiro(8.00m),
        PrecoVenda = new Dinheiro(15.00m),
        ProdutoEstoque = estoque
    };

    private static ProdutoEstoque CriarEstoque(int quantidadeAtual = 10, int quantidadeMinima = 2, decimal precoCusto = 5.00m) => new()
    {
        Id = 1,
        PecaId = 1,
        QuantidadeAtual = quantidadeAtual,
        QuantidadeMinima = quantidadeMinima,
        PrecoCustoMedio = new Dinheiro(precoCusto)
    };

    // --- AdicionarEstoque ---

    [Fact]
    public void AdicionarEstoque_ProdutoEstoqueNull_CriaProdutoEstoqueNovo()
    {
        // Arrange
        var peca = CriarPeca(estoque: null);

        // Act
        peca.AdicionarEstoque(quantidade: 5, precoCusto: 10.00m);

        // Assert
        Assert.NotNull(peca.ProdutoEstoque);
    }

    [Fact]
    public void AdicionarEstoque_ProdutoEstoqueNull_DefineQuantidadeCorreta()
    {
        // Arrange
        var peca = CriarPeca(estoque: null);

        // Act
        peca.AdicionarEstoque(quantidade: 5, precoCusto: 10.00m);

        // Assert
        Assert.Equal(5, peca.ProdutoEstoque!.QuantidadeAtual);
    }

    [Fact]
    public void AdicionarEstoque_ProdutoEstoqueNull_DefinePrecoCorretamente()
    {
        // Arrange
        var peca = CriarPeca(estoque: null);

        // Act
        peca.AdicionarEstoque(quantidade: 5, precoCusto: 10.00m);

        // Assert
        Assert.Equal(10.00m, (decimal)peca.ProdutoEstoque!.PrecoCustoMedio);
    }

    [Fact]
    public void AdicionarEstoque_ProdutoEstoqueExistente_SomaQuantidade()
    {
        // Arrange — 10 unidades existentes
        var estoque = CriarEstoque(quantidadeAtual: 10, precoCusto: 5.00m);
        var peca = CriarPeca(estoque);

        // Act — adicionar mais 5
        peca.AdicionarEstoque(quantidade: 5, precoCusto: 8.00m);

        // Assert
        Assert.Equal(15, peca.ProdutoEstoque!.QuantidadeAtual);
    }

    [Fact]
    public void AdicionarEstoque_ProdutoEstoqueExistente_CalculaPrecoMedioPonderado()
    {
        // Arrange — 10 unid @ R$5,00 existentes
        var estoque = CriarEstoque(quantidadeAtual: 10, precoCusto: 5.00m);
        var peca = CriarPeca(estoque);

        // Act — adicionar 5 unid @ R$8,00
        // Esperado: (10 * 5 + 5 * 8) / (10 + 5) = (50 + 40) / 15 = 6,00
        peca.AdicionarEstoque(quantidade: 5, precoCusto: 8.00m);

        // Assert
        Assert.Equal(6.00m, (decimal)peca.ProdutoEstoque!.PrecoCustoMedio);
    }

    [Fact]
    public void AdicionarEstoque_ProdutoEstoqueExistente_ArredondaParaDuasCasas()
    {
        // Arrange — 10 unid @ R$5,00 existentes
        var estoque = CriarEstoque(quantidadeAtual: 10, precoCusto: 5.00m);
        var peca = CriarPeca(estoque);

        // Act — adicionar 3 unid @ R$7,00
        // (10 * 5 + 3 * 7) / 13 = (50 + 21) / 13 = 71 / 13 = 5,4615...  → arredonda para 5,46
        peca.AdicionarEstoque(quantidade: 3, precoCusto: 7.00m);

        // Assert
        Assert.Equal(5.46m, (decimal)peca.ProdutoEstoque!.PrecoCustoMedio);
    }

    // --- DarBaixa ---

    [Fact]
    public void DarBaixa_SemEstoque_DeveLancarInvalidOperationException()
    {
        // Arrange
        var peca = CriarPeca(estoque: null);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => peca.DarBaixa(1));
    }

    [Fact]
    public void DarBaixa_ComEstoque_PublicaEstoqueBaixaRealizadaEvent()
    {
        // Arrange — 10 unid, mínimo 2; após baixa de 5 ainda acima do mínimo
        var estoque = CriarEstoque(quantidadeAtual: 10, quantidadeMinima: 2);
        var peca = CriarPeca(estoque);

        // Act
        peca.DarBaixa(5);

        // Assert — evento de baixa sempre publicado
        Assert.Contains(peca.GetDomainEvents(), e => e is EstoqueBaixaRealizadaEvent);
    }

    [Fact]
    public void DarBaixa_EstoqueAcimaMinimo_PublicaApenasUmEvento()
    {
        // Arrange — após baixa (10 - 5 = 5), ainda acima do mínimo (2)
        var estoque = CriarEstoque(quantidadeAtual: 10, quantidadeMinima: 2);
        var peca = CriarPeca(estoque);

        // Act
        peca.DarBaixa(5);

        // Assert — somente EstoqueBaixaRealizadaEvent, sem EstoqueAbaixoMinimoEvent
        Assert.Single(peca.GetDomainEvents());
        Assert.DoesNotContain(peca.GetDomainEvents(), e => e is EstoqueAbaixoMinimoEvent);
    }

    [Fact]
    public void DarBaixa_EstoqueAbaixoMinimo_PublicaDoisEventos()
    {
        // Arrange — após baixa (10 - 9 = 1), abaixo do mínimo (2)
        var estoque = CriarEstoque(quantidadeAtual: 10, quantidadeMinima: 2);
        var peca = CriarPeca(estoque);

        // Act
        peca.DarBaixa(9);

        // Assert — ambos os eventos publicados
        Assert.Equal(2, peca.GetDomainEvents().Count);
        Assert.Contains(peca.GetDomainEvents(), e => e is EstoqueBaixaRealizadaEvent);
        Assert.Contains(peca.GetDomainEvents(), e => e is EstoqueAbaixoMinimoEvent);
    }

    [Fact]
    public void DarBaixa_EstoqueAbaixoMinimo_EventoTemDadosCorretos()
    {
        // Arrange
        var estoque = CriarEstoque(quantidadeAtual: 10, quantidadeMinima: 5);
        var peca = CriarPeca(estoque);

        // Act — baixa 8; resultado: QuantidadeAtual = 2, abaixo do mínimo (5)
        peca.DarBaixa(8);

        // Assert
        var evento = peca.GetDomainEvents()
            .OfType<EstoqueAbaixoMinimoEvent>()
            .Single();

        Assert.Equal(1, evento.ProdutoId);
        Assert.Equal("Filtro de Óleo", evento.NomeProduto);
        Assert.Equal(2, evento.QuantidadeAtual);
        Assert.Equal(5, evento.QuantidadeMinima);
    }
}

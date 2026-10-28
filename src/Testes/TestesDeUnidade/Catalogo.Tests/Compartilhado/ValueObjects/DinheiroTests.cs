using Compartilhado.Domain.ValueObjects;

namespace Catalogo.Tests.Compartilhado.ValueObjects;

public class DinheiroTests
{
    [Fact]
    public void ValorPositivo_DeveCriarDinheiro()
    {
        // Arrange & Act
        var dinheiro = new Dinheiro(10.50m);

        // Assert
        Assert.Equal(10.50m, dinheiro.Valor);
    }

    [Fact]
    public void ValorZero_DeveCriarDinheiro()
    {
        // Arrange & Act
        var dinheiro = new Dinheiro(0m);

        // Assert
        Assert.Equal(0m, dinheiro.Valor);
    }

    [Fact]
    public void ValorNegativo_DeveLancarArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Dinheiro(-0.01m));
    }

    [Fact]
    public void ConversaoImplicita_DecimalParaDinheiro()
    {
        // Arrange
        decimal valor = 25.99m;

        // Act
        Dinheiro dinheiro = valor;

        // Assert
        Assert.Equal(valor, dinheiro.Valor);
    }

    [Fact]
    public void ConversaoImplicita_DinheiroParaDecimal()
    {
        // Arrange
        var dinheiro = new Dinheiro(99.90m);

        // Act
        decimal valor = dinheiro;

        // Assert
        Assert.Equal(99.90m, valor);
    }
}

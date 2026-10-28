using Compartilhado.Domain.ValueObjects;

namespace Compartilhado.Tests.Domain.ValueObjects;

public class DinheiroTests
{
    [Fact]
    public void ValorNegativo_LancaArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Dinheiro(-0.01m));
    }

    [Fact]
    public void ValorZero_CriaComSucesso()
    {
        // Arrange & Act
        var dinheiro = new Dinheiro(0m);

        // Assert
        Assert.Equal(0m, dinheiro.Valor);
    }

    [Fact]
    public void ValorPositivo_CriaComSucesso()
    {
        // Arrange & Act
        var dinheiro = new Dinheiro(150.75m);

        // Assert
        Assert.Equal(150.75m, dinheiro.Valor);
    }

    [Fact]
    public void ConversaoImplicita_DecimalParaDinheiro()
    {
        // Arrange
        decimal valor = 99.90m;

        // Act
        Dinheiro dinheiro = valor;

        // Assert
        Assert.Equal(valor, dinheiro.Valor);
    }

    [Fact]
    public void ConversaoImplicita_DinheiroParaDecimal()
    {
        // Arrange
        var dinheiro = new Dinheiro(49.99m);

        // Act
        decimal valor = dinheiro;

        // Assert
        Assert.Equal(49.99m, valor);
    }

    [Fact]
    public void IgualdadePorValor_MesmosValores_SaoIguais()
    {
        // Arrange
        var a = new Dinheiro(100m);
        var b = new Dinheiro(100m);

        // Act & Assert — record usa igualdade estrutural
        Assert.Equal(a, b);
    }
}

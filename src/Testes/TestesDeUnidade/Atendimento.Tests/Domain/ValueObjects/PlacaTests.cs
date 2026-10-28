using Atendimento.Domain.ValueObjects;

namespace Atendimento.Tests.Domain.ValueObjects;

public class PlacaTests
{
    [Fact]
    public void PlacaAntigaValida_DeveCriarPlaca()
    {
        // Arrange
        var valor = "ABC1234";

        // Act
        var placa = new Placa(valor);

        // Assert
        Assert.Equal(valor, placa.Valor);
    }

    [Fact]
    public void PlacaMercosulValida_DeveCriarPlaca()
    {
        // Arrange — formato AAA9A99
        var valor = "ABC1D23";

        // Act
        var placa = new Placa(valor);

        // Assert
        Assert.Equal(valor, placa.Valor);
    }

    [Fact]
    public void PlacaComHifen_DeveNormalizar()
    {
        // Arrange
        var valorComHifen = "ABC-1234";

        // Act
        var placa = new Placa(valorComHifen);

        // Assert — hífen deve ser removido
        Assert.Equal("ABC1234", placa.Valor);
    }

    [Fact]
    public void PlacaMinuscula_DeveNormalizarParaMaiuscula()
    {
        // Arrange
        var valorMinusculo = "abc1234";

        // Act
        var placa = new Placa(valorMinusculo);

        // Assert — letras devem ser convertidas para maiúsculo
        Assert.Equal("ABC1234", placa.Valor);
    }

    [Fact]
    public void PlacaVazia_DeveLancarArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Placa(""));
    }

    [Fact]
    public void PlacaWhitespace_DeveLancarArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Placa("   "));
    }

    [Fact]
    public void PlacaFormatoInvalido_DeveLancarArgumentException()
    {
        // Arrange — 6 caracteres, não satisfaz nenhum formato
        var valorInvalido = "ABC123";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Placa(valorInvalido));
    }

    [Fact]
    public void ConversaoImplicita_StringParaPlaca()
    {
        // Arrange
        string valor = "ABC1234";

        // Act
        Placa placa = valor;

        // Assert
        Assert.Equal(valor, placa.Valor);
    }
}

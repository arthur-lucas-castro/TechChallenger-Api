using Compartilhado.Domain.ValueObjects;

namespace Compartilhado.Tests.Domain.ValueObjects;

public class TelefoneTests
{
    [Fact]
    public void TelefoneComDezDigitos_CriaComSucesso()
    {
        // Arrange — fixo com DDD (10 dígitos)
        var telefone = new Telefone("1132345678");

        // Assert
        Assert.Equal("1132345678", telefone.Valor);
    }

    [Fact]
    public void TelefoneComOnzeDigitos_CriaComSucesso()
    {
        // Arrange — celular com DDD + dígito 9 (11 dígitos)
        var telefone = new Telefone("11987654321");

        // Assert
        Assert.Equal("11987654321", telefone.Valor);
    }

    [Fact]
    public void TelefoneComNoveDigitos_LancaArgumentException()
    {
        // Act & Assert — abaixo do mínimo de 10 dígitos
        Assert.Throws<ArgumentException>(() => new Telefone("119876543"));
    }

    [Fact]
    public void TelefoneComDozeDigitos_LancaArgumentException()
    {
        // Act & Assert — acima do máximo de 11 dígitos
        Assert.Throws<ArgumentException>(() => new Telefone("119876543211"));
    }

    [Fact]
    public void TelefoneComFormatacao_NormalizaParaSoDigitos()
    {
        // Arrange — entrada com parênteses, espaço e hífen
        var telefone = new Telefone("(11) 98765-4321");

        // Assert — apenas dígitos devem ser mantidos
        Assert.Equal("11987654321", telefone.Valor);
    }

    [Fact]
    public void ConversaoImplicita_StringParaTelefone()
    {
        // Arrange
        string numero = "11987654321";

        // Act
        Telefone telefone = numero;

        // Assert
        Assert.Equal(numero, telefone.Valor);
    }
}

using Compartilhado.Domain.ValueObjects;

namespace Atendimento.Tests.Compartilhado.ValueObjects;

public class TelefoneTests
{
    [Fact]
    public void TelefoneComDezDigitos_DeveCriarTelefone()
    {
        // Arrange — fixo com DDD
        var numero = "1132345678";

        // Act
        var telefone = new Telefone(numero);

        // Assert
        Assert.Equal(numero, telefone.Valor);
    }

    [Fact]
    public void TelefoneComOnzeDigitos_DeveCriarTelefone()
    {
        // Arrange — celular com DDD + dígito 9
        var numero = "11987654321";

        // Act
        var telefone = new Telefone(numero);

        // Assert
        Assert.Equal(numero, telefone.Valor);
    }

    [Fact]
    public void TelefoneComNoveDigitos_DeveLancarArgumentException()
    {
        // Arrange — menos que o mínimo de 10 dígitos
        var numeroInvalido = "119876543";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Telefone(numeroInvalido));
    }

    [Fact]
    public void TelefoneComDozeDigitos_DeveLancarArgumentException()
    {
        // Arrange — mais que o máximo de 11 dígitos
        var numeroInvalido = "119876543211";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Telefone(numeroInvalido));
    }

    [Fact]
    public void TelefoneComFormatacao_DeveNormalizar()
    {
        // Arrange — formatação com parênteses, espaço e hífen
        var numeroFormatado = "(11) 98765-4321";

        // Act
        var telefone = new Telefone(numeroFormatado);

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

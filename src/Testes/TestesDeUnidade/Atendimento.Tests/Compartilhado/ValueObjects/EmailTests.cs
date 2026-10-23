using Compartilhado.Domain.ValueObjects;

namespace Atendimento.Tests.Compartilhado.ValueObjects;

public class EmailTests
{
    [Fact]
    public void EmailValido_DeveCriarEmail()
    {
        // Arrange
        var valor = "usuario@email.com";

        // Act
        var email = new Email(valor);

        // Assert
        Assert.Equal(valor, email.Valor);
    }

    [Fact]
    public void EmailSemArroba_DeveLancarArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Email("usuarioemail.com"));
    }

    [Fact]
    public void EmailSemPonto_DeveLancarArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Email("usuario@emailcom"));
    }

    [Fact]
    public void Email_DeveNormalizarParaMinusculo()
    {
        // Arrange
        var emailMaiusculo = "USUARIO@EMAIL.COM";

        // Act
        var email = new Email(emailMaiusculo);

        // Assert
        Assert.Equal("usuario@email.com", email.Valor);
    }

    [Fact]
    public void EmailVazio_DeveLancarArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Email(""));
    }

    [Fact]
    public void ConversaoImplicita_StringParaEmail()
    {
        // Arrange
        string valor = "usuario@email.com";

        // Act
        Email email = valor;

        // Assert
        Assert.Equal(valor, email.Valor);
    }
}

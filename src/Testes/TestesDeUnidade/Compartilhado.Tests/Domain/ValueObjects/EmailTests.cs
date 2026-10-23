using Compartilhado.Domain.ValueObjects;

namespace Compartilhado.Tests.Domain.ValueObjects;

public class EmailTests
{
    [Fact]
    public void EmailValido_CriaComSucesso()
    {
        // Arrange & Act
        var email = new Email("usuario@email.com");

        // Assert
        Assert.Equal("usuario@email.com", email.Valor);
    }

    [Fact]
    public void EmailVazio_LancaArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Email(""));
    }

    [Fact]
    public void EmailSemArroba_LancaArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Email("usuarioemail.com"));
    }

    [Fact]
    public void EmailSemPonto_LancaArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Email("usuario@emailcom"));
    }

    [Fact]
    public void Email_NormalizaParaLowercase()
    {
        // Arrange
        var emailMaiusculo = "USUARIO@EMAIL.COM";

        // Act
        var email = new Email(emailMaiusculo);

        // Assert — trim + lowercase
        Assert.Equal("usuario@email.com", email.Valor);
    }

    [Fact]
    public void ConversaoImplicita_StringParaEmail()
    {
        // Arrange
        string valor = "contato@empresa.com.br";

        // Act
        Email email = valor;

        // Assert
        Assert.Equal(valor, email.Valor);
    }
}

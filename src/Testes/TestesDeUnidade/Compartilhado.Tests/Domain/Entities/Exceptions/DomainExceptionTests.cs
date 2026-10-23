using System.Net;
using Compartilhado.Domain.Entities.Exceptions;

namespace Compartilhado.Tests.Domain.Entities.Exceptions;

public class DomainExceptionTests
{
    [Fact]
    public void Constructor_ComMensagem_StatusCodePadraoEhUnprocessableEntity()
    {
        // Arrange & Act
        var ex = new DomainException("erro de validação");

        // Assert — padrão é 422 Unprocessable Entity
        Assert.Equal(HttpStatusCode.UnprocessableEntity, ex.StatusCode);
    }

    [Fact]
    public void Constructor_ComStatusCodeCustomizado_ArmazenaCorretamente()
    {
        // Arrange & Act
        var ex = new DomainException("não encontrado", HttpStatusCode.NotFound);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, ex.StatusCode);
    }

    [Fact]
    public void Constructor_ComInnerException_PreservaExcecaoInterna()
    {
        // Arrange
        var inner = new InvalidOperationException("causa raiz");

        // Act
        var ex = new DomainException("erro de domínio", inner);

        // Assert
        Assert.Same(inner, ex.InnerException);
    }

    [Fact]
    public void Message_DeveConterMensagemPassada()
    {
        // Arrange
        const string mensagem = "regra de negócio violada";

        // Act
        var ex = new DomainException(mensagem);

        // Assert
        Assert.Equal(mensagem, ex.Message);
    }
}

using Compartilhado.Presentation.Middlewares;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Compartilhado.Tests.Presentation;

public class CorrelationIdMiddlewareTests
{
    private readonly Mock<ILogger<CorrelationIdMiddleware>> _logger = new();

    private CorrelationIdMiddleware CriarMiddleware(RequestDelegate? next = null)
        => new(next ?? (_ => Task.CompletedTask), _logger.Object);

    private static DefaultHttpContext CriarContexto(string? correlationId = null)
    {
        var context = new DefaultHttpContext();
        if (correlationId is not null)
            context.Request.Headers[CorrelationIdMiddleware.Header] = correlationId;
        return context;
    }

    [Fact]
    public async Task InvokeAsync_ComHeaderValido_ReutilizaIdRecebido()
    {
        // Arrange
        var context = CriarContexto("a1B2-c3_d4.e5=");
        var middleware = CriarMiddleware();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal("a1B2-c3_d4.e5=", context.TraceIdentifier);
        Assert.Equal("a1B2-c3_d4.e5=", context.Response.Headers[CorrelationIdMiddleware.Header].ToString());
    }

    [Fact]
    public async Task InvokeAsync_SemHeader_GeraNovoId()
    {
        // Arrange
        var context = CriarContexto();
        var middleware = CriarMiddleware();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var gerado = context.Response.Headers[CorrelationIdMiddleware.Header].ToString();
        Assert.Equal(32, gerado.Length);
        Assert.Equal(gerado, context.TraceIdentifier);
    }

    [Theory]
    [InlineData("id com espaco")]
    [InlineData("id\r\nX-Injetado: 1")]
    [InlineData("<script>")]
    public async Task InvokeAsync_ComHeaderInvalido_GeraNovoId(string recebido)
    {
        // Arrange
        var context = CriarContexto(recebido);
        var middleware = CriarMiddleware();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.NotEqual(recebido, context.TraceIdentifier);
        Assert.Equal(32, context.TraceIdentifier.Length);
    }

    [Fact]
    public async Task InvokeAsync_ComHeaderMaiorQue128Caracteres_GeraNovoId()
    {
        // Arrange
        var recebido = new string('a', 129);
        var context = CriarContexto(recebido);
        var middleware = CriarMiddleware();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.NotEqual(recebido, context.TraceIdentifier);
    }

    [Fact]
    public async Task InvokeAsync_AbreEscopoDeLogComCorrelationId()
    {
        // Arrange
        Dictionary<string, object>? escopo = null;
        _logger.Setup(l => l.BeginScope(It.IsAny<Dictionary<string, object>>()))
            .Callback<Dictionary<string, object>>(s => escopo = s)
            .Returns(Mock.Of<IDisposable>());
        var context = CriarContexto("req-123");
        var middleware = CriarMiddleware();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.NotNull(escopo);
        Assert.Equal("req-123", escopo![CorrelationIdMiddleware.PropriedadeLog]);
    }

    [Fact]
    public async Task InvokeAsync_ChamaProximoMiddleware()
    {
        // Arrange
        var chamado = false;
        var middleware = CriarMiddleware(_ => { chamado = true; return Task.CompletedTask; });

        // Act
        await middleware.InvokeAsync(CriarContexto());

        // Assert
        Assert.True(chamado);
    }
}

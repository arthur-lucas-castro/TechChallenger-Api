using System.Data.Common;
using System.Net;
using Compartilhado.Application.Services.Interfaces;
using Compartilhado.Domain.Entities.Exceptions;
using Compartilhado.Presentation.Middlewares;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Compartilhado.Tests.Presentation;

public class ExceptionMiddlewareTests
{
    private readonly Mock<IMetricasService> _metricasMock = new();

    private ExceptionMiddleware CriarMiddleware(Exception? excecao)
        => new(
            _ => excecao is null ? Task.CompletedTask : throw excecao,
            new Mock<ILogger<ExceptionMiddleware>>().Object,
            _metricasMock.Object);

    private static DefaultHttpContext CriarContexto(string? controller = "OrdemServico", string? action = "IniciarDiagnostico")
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        if (controller is not null) context.Request.RouteValues["controller"] = controller;
        if (action is not null) context.Request.RouteValues["action"] = action;
        return context;
    }

    public static TheoryData<Exception, string, int> Excecoes => new()
    {
        { new DomainException("regra violada"), "dominio", (int)HttpStatusCode.UnprocessableEntity },
        { new ArgumentException("inválido"), "validacao", (int)HttpStatusCode.BadRequest },
        { new KeyNotFoundException("não existe"), "nao_encontrado", (int)HttpStatusCode.NotFound },
        { new InvalidOperationException("transição"), "operacao_invalida", (int)HttpStatusCode.BadRequest },
        { new Exception("falha"), "erro_interno", (int)HttpStatusCode.InternalServerError },
    };

    [Theory]
    [MemberData(nameof(Excecoes))]
    public async Task InvokeAsync_ComExcecao_RegistraFalhaComTipoEStatus(Exception excecao, string tipo, int statusEsperado)
    {
        // Arrange
        var context = CriarContexto();
        var middleware = CriarMiddleware(excecao);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(statusEsperado, context.Response.StatusCode);
        _metricasMock.Verify(m => m.FalhaRequisicao("OrdemServico", "IniciarDiagnostico", tipo), Times.Once);
    }

    private class FalhaBancoException(string mensagem) : DbException(mensagem);

    public static TheoryData<Exception> FalhasDeBanco => new()
    {
        new FalhaBancoException("connection refused"),
        new InvalidOperationException("An exception has been raised that is likely due to a transient failure.",
            new FalhaBancoException("connection refused")),
        new Exception("externa", new InvalidOperationException("intermediária", new FalhaBancoException("timeout"))),
    };

    [Theory]
    [MemberData(nameof(FalhasDeBanco))]
    public async Task InvokeAsync_FalhaDeBanco_Retorna500ERegistraErroInterno(Exception excecao)
    {
        // Arrange
        var context = CriarContexto();
        var middleware = CriarMiddleware(excecao);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.InternalServerError, context.Response.StatusCode);
        _metricasMock.Verify(m => m.FalhaRequisicao("OrdemServico", "IniciarDiagnostico", "erro_interno"), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_FalhaDeBanco_NaoExpoeMensagemDoProvider()
    {
        // Arrange
        var context = CriarContexto();
        var middleware = CriarMiddleware(new InvalidOperationException("transient failure",
            new FalhaBancoException("Host=techchallenger-db.rds.amazonaws.com recusou a conexão")));

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Body.Position = 0;
        var corpo = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.DoesNotContain("rds.amazonaws.com", corpo);
        Assert.DoesNotContain("transient", corpo);
    }

    [Fact]
    public async Task InvokeAsync_SemRota_RegistraFalhaComoDesconhecida()
    {
        // Arrange
        var context = CriarContexto(controller: null, action: null);
        var middleware = CriarMiddleware(new Exception("falha"));

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _metricasMock.Verify(m => m.FalhaRequisicao("desconhecido", "desconhecida", "erro_interno"), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_SemExcecao_NaoRegistraFalha()
    {
        // Arrange
        var middleware = CriarMiddleware(null);

        // Act
        await middleware.InvokeAsync(CriarContexto());

        // Assert
        _metricasMock.Verify(m => m.FalhaRequisicao(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}

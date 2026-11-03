using Compartilhado.Infrastructure.Metricas;

namespace Compartilhado.Tests.Infrastructure;

public class EnderecoDogStatsdTests
{
    [Fact]
    public void Resolver_SocketUnix_UsaUrlComoServidor()
    {
        // Act
        var (servidor, porta) = EnderecoDogStatsd.Resolver("unix:///var/run/datadog/dsd.socket", null);

        // Assert
        Assert.Equal("unix:///var/run/datadog/dsd.socket", servidor);
        Assert.Null(porta);
    }

    [Fact]
    public void Resolver_UrlUdp_SeparaHostEPorta()
    {
        // Act
        var (servidor, porta) = EnderecoDogStatsd.Resolver("udp://10.0.1.5:8125", null);

        // Assert
        Assert.Equal("10.0.1.5", servidor);
        Assert.Equal(8125, porta);
    }

    [Fact]
    public void Resolver_SoAgentHost_UsaHost()
    {
        // Act
        var (servidor, porta) = EnderecoDogStatsd.Resolver(null, "10.0.1.5");

        // Assert
        Assert.Equal("10.0.1.5", servidor);
        Assert.Null(porta);
    }

    [Fact]
    public void Resolver_UrlTemPrioridadeSobreAgentHost()
    {
        // Act
        var (servidor, _) = EnderecoDogStatsd.Resolver("unix:///var/run/datadog/dsd.socket", "10.0.1.5");

        // Assert
        Assert.Equal("unix:///var/run/datadog/dsd.socket", servidor);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "  ")]
    [InlineData("http://invalido", null)]
    public void Resolver_SemAgente_RetornaServidorNulo(string? url, string? host)
    {
        // Act
        var (servidor, porta) = EnderecoDogStatsd.Resolver(url, host);

        // Assert
        Assert.Null(servidor);
        Assert.Null(porta);
    }
}

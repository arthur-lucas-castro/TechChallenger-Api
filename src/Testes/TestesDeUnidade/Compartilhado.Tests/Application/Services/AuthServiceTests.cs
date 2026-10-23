using System.IdentityModel.Tokens.Jwt;
using Compartilhado.Application.DTOs;
using Compartilhado.Application.Services;
using Compartilhado.Domain.Entities;
using Compartilhado.Domain.Entities.Interfaces;
using Compartilhado.Domain.ValueObjects;
using Microsoft.Extensions.Configuration;

namespace Compartilhado.Tests.Application.Services;

public class AuthServiceTests
{
    // Chave com ≥ 32 bytes em UTF-8 (requisito mínimo do HmacSha256)
    private const string JwtSecretKey = "chave-secreta-minimo-32-caracteres-ok!!";
    private const string JwtIssuer    = "test-issuer";
    private const string JwtAudience  = "test-audience";

    private readonly Mock<IUsuarioRepositorio> _repositorioMock;
    private readonly Mock<IConfiguration> _configMock;
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        _repositorioMock = new Mock<IUsuarioRepositorio>();
        _configMock      = new Mock<IConfiguration>();

        _configMock.Setup(c => c["Jwt:SecretKey"]).Returns(JwtSecretKey);
        _configMock.Setup(c => c["Jwt:Issuer"]).Returns(JwtIssuer);
        _configMock.Setup(c => c["Jwt:Audience"]).Returns(JwtAudience);
        _configMock.Setup(c => c["Jwt:ExpirationMinutes"]).Returns("60");

        _service = new AuthService(_repositorioMock.Object, _configMock.Object);
    }

    private static Usuario CriarUsuario(string senha = "senha123", TipoUsuario tipo = TipoUsuario.Adm) => new()
    {
        Id = 7,
        Email = "admin@email.com",
        SenhaHash = BCrypt.Net.BCrypt.HashPassword(senha),
        Tipo = tipo
    };

    private static LoginRequestDto CriarRequest(string email = "admin@email.com", string senha = "senha123") =>
        new() { Email = email, Senha = senha };

    // ── Retorno null ─────────────────────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_EmailNaoEncontrado_RetornaNull()
    {
        // Arrange
        _repositorioMock.Setup(r => r.ObterPorEmailAsync(It.IsAny<string>())).ReturnsAsync((Usuario?)null);

        // Act
        var resultado = await _service.LoginAsync(CriarRequest());

        // Assert
        Assert.Null(resultado);
    }

    [Fact]
    public async Task LoginAsync_SenhaIncorreta_RetornaNull()
    {
        // Arrange — hash de "correta", mas passamos "errada"
        var usuario = CriarUsuario(senha: "correta");
        _repositorioMock.Setup(r => r.ObterPorEmailAsync(It.IsAny<string>())).ReturnsAsync(usuario);

        // Act
        var resultado = await _service.LoginAsync(CriarRequest(senha: "errada"));

        // Assert
        Assert.Null(resultado);
    }

    // ── Happy path ───────────────────────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_CredenciaisValidas_RetornaTokenNaoNulo()
    {
        // Arrange
        _repositorioMock.Setup(r => r.ObterPorEmailAsync("admin@email.com")).ReturnsAsync(CriarUsuario());

        // Act
        var resultado = await _service.LoginAsync(CriarRequest());

        // Assert
        Assert.NotNull(resultado);
        Assert.NotEmpty(resultado.Token);
    }

    [Fact]
    public async Task LoginAsync_NormalizaEmailParaLowercase()
    {
        // Arrange — email com maiúsculas no request
        _repositorioMock.Setup(r => r.ObterPorEmailAsync("admin@email.com")).ReturnsAsync(CriarUsuario());

        // Act
        await _service.LoginAsync(CriarRequest(email: "ADMIN@EMAIL.COM"));

        // Assert — repositório chamado com email em lowercase
        _repositorioMock.Verify(r => r.ObterPorEmailAsync("admin@email.com"), Times.Once);
    }

    // ── Claims do JWT ────────────────────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_TokenContemSubClaimComUserId()
    {
        // Arrange
        _repositorioMock.Setup(r => r.ObterPorEmailAsync(It.IsAny<string>())).ReturnsAsync(CriarUsuario());

        // Act
        var resultado = await _service.LoginAsync(CriarRequest());

        // Assert
        var token = new JwtSecurityTokenHandler().ReadJwtToken(resultado!.Token);
        Assert.Contains(token.Claims, c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == "7");
    }

    [Fact]
    public async Task LoginAsync_TokenContemEmailClaim()
    {
        // Arrange
        _repositorioMock.Setup(r => r.ObterPorEmailAsync(It.IsAny<string>())).ReturnsAsync(CriarUsuario());

        // Act
        var resultado = await _service.LoginAsync(CriarRequest());

        // Assert
        var token = new JwtSecurityTokenHandler().ReadJwtToken(resultado!.Token);
        Assert.Contains(token.Claims, c => c.Type == JwtRegisteredClaimNames.Email && c.Value == "admin@email.com");
    }

    [Fact]
    public async Task LoginAsync_TokenContemRoleClaimComTipoUsuario()
    {
        // Arrange
        _repositorioMock.Setup(r => r.ObterPorEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(CriarUsuario(tipo: TipoUsuario.Funcionario));

        // Act
        var resultado = await _service.LoginAsync(CriarRequest());

        // Assert — ClaimTypes.Role é mapeado pelo JwtSecurityTokenHandler para "role"
        var token = new JwtSecurityTokenHandler().ReadJwtToken(resultado!.Token);
        Assert.Contains(token.Claims, c =>
            (c.Type == "role" || c.Type.EndsWith("/role")) && c.Value == "Funcionario");
    }

    [Fact]
    public async Task LoginAsync_TokenContemJtiClaim()
    {
        // Arrange
        _repositorioMock.Setup(r => r.ObterPorEmailAsync(It.IsAny<string>())).ReturnsAsync(CriarUsuario());

        // Act
        var resultado = await _service.LoginAsync(CriarRequest());

        // Assert — Jti deve ser um GUID não vazio
        var token = new JwtSecurityTokenHandler().ReadJwtToken(resultado!.Token);
        var jti = token.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti);
        Assert.NotNull(jti);
        Assert.True(Guid.TryParse(jti.Value, out _));
    }

    // ── Expiração ────────────────────────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_ExpiracaoUsaValorDoConfig()
    {
        // Arrange — 120 minutos configurado
        _configMock.Setup(c => c["Jwt:ExpirationMinutes"]).Returns("120");
        var service = new AuthService(_repositorioMock.Object, _configMock.Object);
        _repositorioMock.Setup(r => r.ObterPorEmailAsync(It.IsAny<string>())).ReturnsAsync(CriarUsuario());
        var antes = DateTime.UtcNow;

        // Act
        var resultado = await service.LoginAsync(CriarRequest());

        // Assert — expiração entre 119 e 121 minutos a partir de agora
        Assert.NotNull(resultado);
        Assert.InRange(resultado.Expiracao, antes.AddMinutes(119), antes.AddMinutes(121));
    }

    [Fact]
    public async Task LoginAsync_ExpiracaoUsaDefault60SeConfigNula()
    {
        // Arrange — config retorna null para ExpirationMinutes
        _configMock.Setup(c => c["Jwt:ExpirationMinutes"]).Returns((string?)null);
        var service = new AuthService(_repositorioMock.Object, _configMock.Object);
        _repositorioMock.Setup(r => r.ObterPorEmailAsync(It.IsAny<string>())).ReturnsAsync(CriarUsuario());
        var antes = DateTime.UtcNow;

        // Act
        var resultado = await service.LoginAsync(CriarRequest());

        // Assert — fallback de 60 minutos
        Assert.NotNull(resultado);
        Assert.InRange(resultado.Expiracao, antes.AddMinutes(59), antes.AddMinutes(61));
    }

    // ── Dto de resposta ──────────────────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_TipoNoResponseDTOCorrespondeAoUsuario()
    {
        // Arrange
        _repositorioMock.Setup(r => r.ObterPorEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(CriarUsuario(tipo: TipoUsuario.Funcionario));

        // Act
        var resultado = await _service.LoginAsync(CriarRequest());

        // Assert
        Assert.Equal("Funcionario", resultado!.Tipo);
    }
}

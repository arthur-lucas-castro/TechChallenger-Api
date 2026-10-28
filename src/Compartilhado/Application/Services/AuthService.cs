using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Compartilhado.Application.DTOs;
using Compartilhado.Application.Services.Interfaces;
using Compartilhado.Domain.Entities.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Compartilhado.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUsuarioRepositorio _repositorio;
        private readonly IConfiguration _config;

        public AuthService(IUsuarioRepositorio repositorio, IConfiguration config)
        {
            _repositorio = repositorio;
            _config = config;
        }

        public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto dto)
        {
            var usuario = await _repositorio.ObterPorEmailAsync(dto.Email.ToLowerInvariant());
            if (usuario is null || !BCrypt.Net.BCrypt.Verify(dto.Senha, usuario.SenhaHash))
                return null;

            var expiracao = DateTime.UtcNow.AddMinutes(
                Convert.ToDouble(_config["Jwt:ExpirationMinutes"] ?? "60"));

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub,  usuario.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, usuario.Email),
                new Claim(JwtRegisteredClaimNames.Jti,  Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat,
                    DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                    ClaimValueTypes.Integer64),
                new Claim(ClaimTypes.Role, usuario.Tipo.ToString())
            };

            var secretKey = _config["Jwt:SecretKey"]
                ?? Environment.GetEnvironmentVariable("Jwt__SecretKey")
                ?? string.Empty;
            var chave = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(secretKey));

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: expiracao,
                signingCredentials: new SigningCredentials(chave, SecurityAlgorithms.HmacSha256));

            return new LoginResponseDto
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                Expiracao = expiracao,
                Tipo = usuario.Tipo.ToString()
            };
        }
    }
}

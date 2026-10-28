using Compartilhado.Application.DTOs;
using Compartilhado.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Compartilhado.Presentation.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _service;

        public AuthController(IAuthService service) => _service = service;

        /// <summary>Autentica um usuário e retorna o token JWT.</summary>
        /// <param name="dto">Credenciais de acesso: <c>email</c> e <c>senha</c>.</param>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
        {
            var resultado = await _service.LoginAsync(dto);
            if (resultado is null) return Unauthorized();
            return Ok(resultado);
        }
    }
}

using System.Reflection;
using Compartilhado.Application.DTOs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace Compartilhado.Presentation.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class VersaoController : ControllerBase
    {
        private readonly IWebHostEnvironment _ambiente;

        public VersaoController(IWebHostEnvironment ambiente) => _ambiente = ambiente;

        /// <summary>Retorna a versão atual da API.</summary>
        [HttpGet]
        public IActionResult Get()
        {
            var versao = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "1.0.0.0";
            return Ok(new VersaoResponseDto { Versao = versao, Ambiente = _ambiente.EnvironmentName });
            
        }
    }
}

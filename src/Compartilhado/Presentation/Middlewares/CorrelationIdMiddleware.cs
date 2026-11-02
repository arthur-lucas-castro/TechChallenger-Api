using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Compartilhado.Presentation.Middlewares
{
    /// <summary>
    /// Propaga o id de correlação da requisição. O API Gateway envia no header
    /// <c>x-correlation-id</c> o mesmo requestId dos seus access logs; sem o header
    /// (ex.: execução local), um id novo é gerado. O id entra em todos os logs da
    /// requisição e volta no header da resposta.
    /// </summary>
    public class CorrelationIdMiddleware
    {
        public const string Header = "x-correlation-id";
        public const string PropriedadeLog = "CorrelationId";

        // Valor vindo do cliente: limita tamanho e caracteres antes de logar e devolver.
        private static readonly Regex _formatoValido = new(@"^[A-Za-z0-9._\-=]{1,128}$", RegexOptions.Compiled);

        private readonly RequestDelegate _next;
        private readonly ILogger<CorrelationIdMiddleware> _logger;

        public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var correlationId = ObterOuGerar(context.Request);

            context.TraceIdentifier = correlationId;
            context.Response.Headers[Header] = correlationId;

            using (_logger.BeginScope(new Dictionary<string, object> { [PropriedadeLog] = correlationId }))
            {
                await _next(context);
            }
        }

        private static string ObterOuGerar(HttpRequest request)
        {
            var recebido = request.Headers[Header].ToString();
            return _formatoValido.IsMatch(recebido) ? recebido : Guid.NewGuid().ToString("N");
        }
    }
}

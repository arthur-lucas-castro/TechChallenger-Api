using System.Net;
using System.Text.Json;
using Compartilhado.Domain.Entities.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Compartilhado.Presentation.Middlewares
{
    public class ExceptionMiddleware
    {
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (DomainException ex)
            {
                _logger.LogWarning(ex, "Exceção de domínio: {Message}", ex.Message);
                await EscreverRespostaAsync(context, ex.StatusCode, ex.Message);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Erro de validação: {Message}", ex.Message);
                await EscreverRespostaAsync(context, HttpStatusCode.BadRequest, ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Recurso não encontrado: {Message}", ex.Message);
                await EscreverRespostaAsync(context, HttpStatusCode.NotFound, ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Operação inválida: {Message}", ex.Message);
                await EscreverRespostaAsync(context, HttpStatusCode.BadRequest, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado: {Message}", ex.Message);
                await EscreverRespostaAsync(context, HttpStatusCode.InternalServerError, "Ocorreu um erro interno. Tente novamente mais tarde.");
            }
        }

        private static async Task EscreverRespostaAsync(HttpContext context, HttpStatusCode statusCode, string mensagem)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            var resposta = new
            {
                status = (int)statusCode,
                erro = mensagem
            };

            var json = JsonSerializer.Serialize(resposta, _jsonOptions);

            await context.Response.WriteAsync(json);
        }
    }
}

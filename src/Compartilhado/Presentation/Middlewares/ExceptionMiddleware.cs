using System.Data.Common;
using System.Net;
using System.Text.Json;
using Compartilhado.Application.Services.Interfaces;
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
        private readonly IMetricasService _metricas;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IMetricasService metricas)
        {
            _next = next;
            _logger = logger;
            _metricas = metricas;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            // Antes dos demais: o EF Core embrulha a falha de conexão numa InvalidOperationException
            // ("transient failure"), que cairia como 400 e esconderia a indisponibilidade do banco.
            catch (Exception ex) when (EhFalhaDeBanco(ex))
            {
                _logger.LogError(ex, "Falha de acesso ao banco de dados: {Message}", ex.Message);
                RegistrarFalha(context, "erro_interno");
                await EscreverRespostaAsync(context, HttpStatusCode.InternalServerError, "Ocorreu um erro interno. Tente novamente mais tarde.");
            }
            catch (DomainException ex)
            {
                _logger.LogWarning(ex, "Exceção de domínio: {Message}", ex.Message);
                RegistrarFalha(context, "dominio");
                await EscreverRespostaAsync(context, ex.StatusCode, ex.Message);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Erro de validação: {Message}", ex.Message);
                RegistrarFalha(context, "validacao");
                await EscreverRespostaAsync(context, HttpStatusCode.BadRequest, ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Recurso não encontrado: {Message}", ex.Message);
                RegistrarFalha(context, "nao_encontrado");
                await EscreverRespostaAsync(context, HttpStatusCode.NotFound, ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Operação inválida: {Message}", ex.Message);
                RegistrarFalha(context, "operacao_invalida");
                await EscreverRespostaAsync(context, HttpStatusCode.BadRequest, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado: {Message}", ex.Message);
                RegistrarFalha(context, "erro_interno");
                await EscreverRespostaAsync(context, HttpStatusCode.InternalServerError, "Ocorreu um erro interno. Tente novamente mais tarde.");
            }
        }

        // DbException é a base das exceções dos providers ADO.NET, como a NpgsqlException.
        private static bool EhFalhaDeBanco(Exception ex)
        {
            for (var atual = ex; atual is not null; atual = atual.InnerException)
            {
                if (atual is DbException)
                    return true;
            }
            return false;
        }

        // Controller e ação vêm da rota, para o alerta filtrar as falhas de ordens de serviço.
        private void RegistrarFalha(HttpContext context, string tipo)
        {
            var controller = context.Request.RouteValues["controller"]?.ToString() ?? "desconhecido";
            var acao = context.Request.RouteValues["action"]?.ToString() ?? "desconhecida";
            _metricas.FalhaRequisicao(controller, acao, tipo);
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

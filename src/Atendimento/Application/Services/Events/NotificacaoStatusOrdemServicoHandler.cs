using Atendimento.Domain.Interfaces;
using Compartilhado.Application.Services.Interfaces;
using Compartilhado.Domain.Entities;
using Compartilhado.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Operacao.Domain.Entities.Events;

namespace Atendimento.Application.Services.Events
{
    public class NotificacaoStatusOrdemServicoHandler :
        IDomainEventHandler<OrdemServicoStatusAlteradoEvent>,
        IDomainEventHandler<OrcamentoRecusadoEvent>
    {
        private readonly IClienteRepositorio _clienteRepositorio;
        private readonly IEmailService _emailService;
        private readonly ILogger<NotificacaoStatusOrdemServicoHandler> _logger;

        private static readonly Dictionary<StatusOrdemServico, (string Assunto, string Corpo)> Templates = new()
        {
            [StatusOrdemServico.EmDiagnostico] = (
                "Sua ordem de serviço #{0} entrou em diagnóstico",
                "Olá {1}, sua ordem de serviço #{0} está agora em diagnóstico. Em breve enviaremos o orçamento."),
            [StatusOrdemServico.AguardandoAprovacao] = (
                "Orçamento disponível para a ordem de serviço #{0}",
                "Olá {1}, o diagnóstico da sua ordem #{0} foi concluído e o orçamento está aguardando sua aprovação."),
            [StatusOrdemServico.EmExecucao] = (
                "Sua ordem de serviço #{0} entrou em execução",
                "Olá {1}, o orçamento foi aprovado e sua ordem #{0} está em execução."),
            [StatusOrdemServico.Finalizada] = (
                "Sua ordem de serviço #{0} foi finalizada",
                "Olá {1}, os serviços da sua ordem #{0} foram concluídos. Aguardamos você para retirada do veículo."),
            [StatusOrdemServico.Entregue] = (
                "Veículo entregue — ordem de serviço #{0}",
                "Olá {1}, confirmamos a entrega do seu veículo referente à ordem #{0}. Obrigado pela confiança!"),
        };

        public NotificacaoStatusOrdemServicoHandler(
            IClienteRepositorio clienteRepositorio,
            IEmailService emailService,
            ILogger<NotificacaoStatusOrdemServicoHandler> logger)
        {
            _clienteRepositorio = clienteRepositorio;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task HandleAsync(OrdemServicoStatusAlteradoEvent domainEvent, CancellationToken ct = default)
        {
            if (!Templates.TryGetValue(domainEvent.NovoStatus, out var template))
                return;

            await EnviarSeguroAsync(domainEvent.ClienteId, domainEvent.OrdemServicoId, template.Assunto, template.Corpo, ct);
        }

        public async Task HandleAsync(OrcamentoRecusadoEvent domainEvent, CancellationToken ct = default)
        {
            await EnviarSeguroAsync(
                domainEvent.ClienteId, domainEvent.OrdemServicoId,
                "Orçamento recusado — ordem de serviço #{0}",
                "Olá {1}, identificamos a recusa do orçamento da sua ordem #{0}. Entre em contato para mais detalhes.",
                ct);
        }

        private async Task EnviarSeguroAsync(int clienteId, int ordemServicoId, string assuntoTemplate, string corpoTemplate, CancellationToken ct)
        {
            try
            {
                var cliente = await _clienteRepositorio.GetByIdAsync(clienteId);
                if (cliente is null)
                {
                    _logger.LogWarning("Cliente {ClienteId} não encontrado para notificação da OS {OrdemServicoId}.", clienteId, ordemServicoId);
                    return;
                }

                var assunto = string.Format(assuntoTemplate, ordemServicoId);
                var corpo = string.Format(corpoTemplate, ordemServicoId, cliente.Nome);

                await _emailService.EnviarAsync(cliente.Email, assunto, corpo, ct);

                _logger.LogInformation("E-mail de notificação enviado para {Email} (OS {OrdemServicoId}).", cliente.Email.Valor, ordemServicoId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao enviar e-mail de notificação para o cliente {ClienteId} (OS {OrdemServicoId}).", clienteId, ordemServicoId);
            }
        }
    }
}

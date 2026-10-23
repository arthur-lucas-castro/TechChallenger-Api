using System.Net.Sockets;
using Compartilhado.Application.Services.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Polly;

namespace Compartilhado.Infrastructure.Email
{
    public class SmtpEmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<SmtpEmailService> _logger;
        private readonly IAsyncPolicy _retryPolicy;

        public SmtpEmailService(IOptions<EmailSettings> options, ILogger<SmtpEmailService> logger)
        {
            _settings = options.Value;
            _logger = logger;
            _retryPolicy = Policy
                .Handle<SocketException>()
                .Or<SmtpCommandException>()
                .Or<SmtpProtocolException>()
                .WaitAndRetryAsync(
                    _settings.TentativasMaximas,
                    tentativa => TimeSpan.FromSeconds(_settings.DelayInicialSegundos * Math.Pow(2, tentativa - 1)),
                    (ex, delay, tentativa, _) =>
                        _logger.LogWarning(ex, "Falha ao enviar e-mail (tentativa {Tentativa}), retry em {Delay}.", tentativa, delay));
        }

        public async Task EnviarAsync(string destinatario, string assunto, string corpo, CancellationToken ct = default)
        {
            if (!_settings.Habilitado)
            {
                _logger.LogInformation("Envio de e-mail desabilitado (Email:Habilitado=false). Assunto: {Assunto}", assunto);
                return;
            }

            var mensagem = new MimeMessage();
            mensagem.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));
            mensagem.To.Add(MailboxAddress.Parse(destinatario));
            mensagem.Subject = assunto;
            mensagem.Body = new TextPart("plain") { Text = corpo };

            await _retryPolicy.ExecuteAsync(async token =>
            {
                using var client = new SmtpClient();
                await client.ConnectAsync(_settings.Host, _settings.Port,
                    _settings.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None, token);

                if (!string.IsNullOrEmpty(_settings.Username))
                    await client.AuthenticateAsync(_settings.Username, _settings.Password ?? string.Empty, token);

                await client.SendAsync(mensagem, token);
                await client.DisconnectAsync(true, token);
            }, ct);
        }
    }
}

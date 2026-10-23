namespace Compartilhado.Application.Services.Interfaces
{
    public interface IEmailService
    {
        Task EnviarAsync(string destinatario, string assunto, string corpo, CancellationToken ct = default);
    }
}

namespace Compartilhado.Infrastructure.Email
{
    public class EmailSettings
    {
        public bool Habilitado { get; set; } = true;
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 587;
        public bool UseSsl { get; set; } = true;
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string FromAddress { get; set; } = string.Empty;
        public string FromName { get; set; } = "TechChallenger Oficina";
        public int TentativasMaximas { get; set; } = 3;
        public int DelayInicialSegundos { get; set; } = 2;
    }
}

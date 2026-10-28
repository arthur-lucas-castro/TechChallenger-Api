namespace Compartilhado.Domain.ValueObjects
{
    public record Email
    {
        public string Valor { get; }

        public Email(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor) || !valor.Contains('@') || !valor.Contains('.'))
                throw new ArgumentException("E-mail inválido.", nameof(valor));

            Valor = valor.Trim().ToLowerInvariant();
        }

        public static implicit operator Email(string valor) => new(valor);
        public static implicit operator string(Email email) => email.Valor;

        public override string ToString() => Valor;
    }
}

namespace Compartilhado.Domain.ValueObjects
{
    public record Telefone
    {
        public string Valor { get; }

        public Telefone(string valor)
        {
            var digitos = new string(valor.Where(char.IsDigit).ToArray());

            if (digitos.Length < 10 || digitos.Length > 11)
                throw new ArgumentException("Telefone deve ter 10 ou 11 dígitos.", nameof(valor));

            Valor = digitos;
        }

        public static implicit operator Telefone(string valor) => new(valor);
        public static implicit operator string(Telefone telefone) => telefone.Valor;

        public override string ToString() => Valor;
    }
}

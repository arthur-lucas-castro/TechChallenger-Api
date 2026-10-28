namespace Compartilhado.Domain.ValueObjects
{
    public record Dinheiro
    {
        public decimal Valor { get; }

        public Dinheiro(decimal valor)
        {
            if (valor < 0)
                throw new ArgumentException("Valor monetário não pode ser negativo.", nameof(valor));

            Valor = valor;
        }

        public static implicit operator Dinheiro(decimal valor) => new(valor);
        public static implicit operator decimal(Dinheiro dinheiro) => dinheiro.Valor;

        public override string ToString() => Valor.ToString("C2");
    }
}

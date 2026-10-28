using System.Text.RegularExpressions;

namespace Atendimento.Domain.ValueObjects
{
    public record Placa
    {

        private static readonly Regex _padraoAntigo   = new(@"^[A-Z]{3}\d{4}$", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));
        private static readonly Regex _padraoMercosul = new(@"^[A-Z]{3}\d[A-Z]\d{2}$", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));

        public string Valor { get; }

        public Placa(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                throw new ArgumentException("Placa não pode ser vazia.", nameof(valor));

            var normalizado = valor.Replace("-", "").ToUpperInvariant();

            if (!_padraoAntigo.IsMatch(normalizado) && !_padraoMercosul.IsMatch(normalizado))
                throw new ArgumentException(
                    "Placa inválida. Formatos aceitos: AAA9999 (antigo) ou AAA9A99 (Mercosul).",
                    nameof(valor));

            Valor = normalizado;
        }

        public static implicit operator Placa(string valor) => new(valor);
        public static implicit operator string(Placa placa) => placa.Valor;

        public override string ToString() => Valor;
    }
}

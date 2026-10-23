namespace Atendimento.Domain.ValueObjects
{
    public record Documento
    {
        public string Numero { get; }

        public Documento(string numero)
        {
            var digitos = new string(numero.Where(char.IsDigit).ToArray());

            if (digitos.Length == 11 && ValidarCpf(digitos))
            {
                Numero = digitos;
                return;
            }

            if (digitos.Length == 14 && ValidarCnpj(digitos))
            {
                Numero = digitos;
                return;
            }

            throw new ArgumentException(
                "Documento inválido. Informe um CPF (11 dígitos) ou CNPJ (14 dígitos) válido.",
                nameof(numero));
        }

        public static implicit operator Documento(string numero) => new(numero);
        public static implicit operator string(Documento documento) => documento.Numero;

        public bool EhCpf  => Numero.Length == 11;
        public bool EhCnpj => Numero.Length == 14;

        public override string ToString() => Numero;

        private static bool ValidarCpf(string cpf)
        {
            if (cpf.Distinct().Count() == 1) return false;

            int Digito(string s, int[] pesos)
            {
                var soma = s.Zip(pesos, (c, p) => (c - '0') * p).Sum();
                var resto = soma % 11;
                return resto < 2 ? 0 : 11 - resto;
            }

            int d1 = Digito(cpf[..9],  [10, 9, 8, 7, 6, 5, 4, 3, 2]);
            int d2 = Digito(cpf[..10], [11, 10, 9, 8, 7, 6, 5, 4, 3, 2]);

            return cpf[9] - '0' == d1 && cpf[10] - '0' == d2;
        }

        private static bool ValidarCnpj(string cnpj)
        {
            if (cnpj.Distinct().Count() == 1) return false;

            int Digito(string s, int[] pesos)
            {
                var soma = s.Zip(pesos, (c, p) => (c - '0') * p).Sum();
                var resto = soma % 11;
                return resto < 2 ? 0 : 11 - resto;
            }

            int d1 = Digito(cnpj[..12], [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2]);
            int d2 = Digito(cnpj[..13], [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2]);

            return cnpj[12] - '0' == d1 && cnpj[13] - '0' == d2;
        }
    }
}

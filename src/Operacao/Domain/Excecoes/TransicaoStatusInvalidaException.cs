using Compartilhado.Domain.Entities.Exceptions;

namespace Operacao.Domain.Excecoes;

public class TransicaoStatusInvalidaException : DomainException
{
    public TransicaoStatusInvalidaException(string mensagem) : base(mensagem) { }
}

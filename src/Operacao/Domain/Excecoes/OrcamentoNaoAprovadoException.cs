using Compartilhado.Domain.Entities.Exceptions;

namespace Operacao.Domain.Excecoes;

public class OrcamentoNaoAprovadoException : DomainException
{
    public OrcamentoNaoAprovadoException(string mensagem) : base(mensagem) { }
}

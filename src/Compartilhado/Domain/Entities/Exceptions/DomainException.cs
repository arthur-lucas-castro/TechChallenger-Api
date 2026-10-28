using System.Net;

namespace Compartilhado.Domain.Entities.Exceptions
{
    public class DomainException : Exception
    {
        public HttpStatusCode StatusCode { get; }

        public DomainException(string mensagem, HttpStatusCode statusCode = HttpStatusCode.UnprocessableEntity)
            : base(mensagem)
        {
            StatusCode = statusCode;
        }

        public DomainException(string mensagem, Exception innerException, HttpStatusCode statusCode = HttpStatusCode.UnprocessableEntity)
            : base(mensagem, innerException)
        {
            StatusCode = statusCode;
        }
    }
}

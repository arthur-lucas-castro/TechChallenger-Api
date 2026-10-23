using System.Data;

namespace Compartilhado.Infrastructure.Repositories.Interface
{
    public interface IDbConnectionFactory
    {
        IDbConnection CreateConnection();
    }
}

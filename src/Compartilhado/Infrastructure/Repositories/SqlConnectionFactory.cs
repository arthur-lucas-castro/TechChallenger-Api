using Npgsql;
using Microsoft.Extensions.Configuration;
using Compartilhado.Infrastructure.Repositories.Interface;
using System.Data;

namespace Compartilhado.Infrastructure.Repositories
{
    public class SqlConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;

        public SqlConnectionFactory(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("Default")!;
        }

        public IDbConnection CreateConnection()
            => new NpgsqlConnection(_connectionString);
    }
}

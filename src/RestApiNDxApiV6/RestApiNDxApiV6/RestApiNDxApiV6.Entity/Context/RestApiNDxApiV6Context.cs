using Dapper;
using System.Data;
using System.Data.SqlClient;

namespace RestApiNDxApiV6.Entity.Context
{
    public interface IDbConnectionProvider
    {
        IDbConnection Connection { get; }
    }

    public class RestApiNDxApiV6Context : IDbConnectionProvider
    {
        private readonly IDbConnection _connection;
        private IDbTransaction _transaction;
        public RestApiNDxApiV6Context(string connection)
        {
            _connection = new SqlConnection(connection);
        }

        public IDbConnection Connection { get => _connection; }
        public IDbTransaction Transaction { get => _transaction; set => _transaction = value; }

    }
}






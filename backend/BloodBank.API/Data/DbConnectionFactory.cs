using System.Data;
using Microsoft.Data.SqlClient;

namespace BloodBank.API.Data;

public sealed class DbConnectionFactory : IDbConnectionFactory
{
    public string ConnectionString { get; }

    public DbConnectionFactory(string connectionString)
    {
        ConnectionString = connectionString;
    }

    public IDbConnection CreateConnection()
    {
        return new SqlConnection(ConnectionString);
    }
}

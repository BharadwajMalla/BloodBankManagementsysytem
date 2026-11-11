using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;

namespace BloodBank.API.Data;

public sealed class DbConnectionFactory : IDbConnectionFactory
{
    public string ConnectionString { get; }
    public bool IsSqlite { get; }

    public DbConnectionFactory(string connectionString, bool isSqlite)
    {
        ConnectionString = connectionString;
        IsSqlite = isSqlite;
    }

    public IDbConnection CreateConnection()
    {
        if (IsSqlite)
        {
            var sqliteConn = new SqliteConnection(ConnectionString);
            return sqliteConn;
        }

        return new SqlConnection(ConnectionString);
    }
}

using System.Data;

namespace BloodBank.API.Data;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
    bool IsSqlite { get; }
    string ConnectionString { get; }
}

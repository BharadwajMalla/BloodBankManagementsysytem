using System.Data;
using Dapper;

namespace BloodBank.API.Data;

internal static class DapperTypeHandlers
{
    private static int _sqliteGuidHandlerRegistered;

    public static void Register(bool isSqlite)
    {
        if (isSqlite && Interlocked.Exchange(ref _sqliteGuidHandlerRegistered, 1) == 0)
        {
            SqlMapper.AddTypeHandler(new SqliteGuidTypeHandler());
        }
    }
}

internal sealed class SqliteGuidTypeHandler : SqlMapper.TypeHandler<Guid>
{
    public override Guid Parse(object value)
    {
        return Guid.Parse(value.ToString()!);
    }

    public override void SetValue(IDbDataParameter parameter, Guid value)
    {
        parameter.DbType = DbType.Guid;
        parameter.Value = value;
    }
}
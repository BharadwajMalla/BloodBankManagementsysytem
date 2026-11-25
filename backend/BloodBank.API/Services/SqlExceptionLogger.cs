using Dapper;
using BloodBank.API.Data;

namespace BloodBank.API.Services;

public sealed class SqlExceptionLogger(
    IDbConnectionFactory connectionFactory,
    ILogger<SqlExceptionLogger> logger) : ISqlExceptionLogger
{
    public async Task LogAsync(Exception exception, string context)
    {
        try
        {
            using var connection = connectionFactory.CreateConnection();
            var tableName = connectionFactory.IsSqlite ? "ExceptionLogs" : "[dbo].[ExceptionLogs]";
            var sql = $@"
                INSERT INTO {tableName}
                    (Id, Context, ExceptionType, Message, Details, OccurredAtUtc)
                VALUES
                    (@Id, @Context, @ExceptionType, @Message, @Details, @OccurredAtUtc)";

            await connection.ExecuteAsync(sql, new
            {
                Id = Guid.NewGuid().ToString("D"),
                Context = context,
                ExceptionType = exception.GetType().FullName ?? exception.GetType().Name,
                exception.Message,
                Details = exception.ToString(),
                OccurredAtUtc = DateTime.UtcNow
            });
        }
        catch (Exception loggingException)
        {
            logger.LogError(
                loggingException,
                "Could not write exception from {Context} to the SQL exception log. Original exception: {OriginalException}",
                context,
                exception);
        }
    }
}

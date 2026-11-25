using BloodBank.API.Data;
using BloodBank.API.Services;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BloodBank.Application.Tests;

public sealed class SqlExceptionLoggerTests
{
    [Fact]
    public async Task LogAsyncPersistsExceptionDetailsToSql()
    {
        var connectionString = $"Data Source=ExceptionLogs-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
        await using var anchor = new SqliteConnection(connectionString);
        await anchor.OpenAsync();
        await anchor.ExecuteAsync(@"
            CREATE TABLE ExceptionLogs (
                Id TEXT NOT NULL PRIMARY KEY,
                Context TEXT NOT NULL,
                ExceptionType TEXT NOT NULL,
                Message TEXT NOT NULL,
                Details TEXT NOT NULL,
                OccurredAtUtc TEXT NOT NULL
            )");

        var connectionFactory = new DbConnectionFactory(connectionString, isSqlite: true);
        var exceptionLogger = new SqlExceptionLogger(
            connectionFactory,
            NullLogger<SqlExceptionLogger>.Instance);
        var exception = new InvalidOperationException("Test failure details");

        await exceptionLogger.LogAsync(exception, "Test operation");

        using var connection = connectionFactory.CreateConnection();
        var log = await connection.QuerySingleAsync<dynamic>(
            "SELECT Context, ExceptionType, Message, Details FROM ExceptionLogs");

        Assert.Equal("Test operation", (string)log.Context);
        Assert.Equal(typeof(InvalidOperationException).FullName, (string)log.ExceptionType);
        Assert.Equal(exception.Message, (string)log.Message);
        Assert.Contains(exception.ToString(), (string)log.Details);
    }
}

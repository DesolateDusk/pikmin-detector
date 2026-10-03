using System.Data.Common;
using Microsoft.Extensions.Options;
using Npgsql;
using PikminDetector.Api.Lib.CustomException;
using PikminDetector.Api.Models;
using PikminDetector.Api.Models.Enum;

namespace PikminDetector.Api.Repositories;

public sealed class ConnectionProviderFactory(IOptions<Database> options) : IConnectionProviderFactory
{
    public DbConnection GetConnection(string providerName, bool pooling = true)
    {
        return providerName switch
        {
            ConnectionKeys.PostgreSql => CreatePostgreSqlConnection(pooling),
            _ => throw new NotSupportedException($"Unsupported provider: {providerName}")
        };
    }

    private NpgsqlConnection CreatePostgreSqlConnection(bool pooling)
    {
        var connectionString = options.Value.Pikmin;
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new CommonException(StatusCodes.Status503ServiceUnavailable, "Spot data service is not configured.");

        if (!pooling)
            connectionString = new NpgsqlConnectionStringBuilder(connectionString) { Pooling = false }.ConnectionString;

        return new NpgsqlConnection(connectionString);
    }
}

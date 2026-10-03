using Microsoft.Extensions.Options;
using Npgsql;
using PikminDetector.Api.Lib.CustomException;
using PikminDetector.Api.Models;
using PikminDetector.Api.Models.Enum;
using PikminDetector.Api.Repositories;

namespace PikminDetector.Api.Tests;

public sealed class ConnectionProviderFactoryTests
{
    private const string ConnectionString = "Host=db.example;Database=pikmin;Username=test";

    [Test]
    public void GetConnection_PostgreSql_CreatesIndependentUnopenedConnections()
    {
        var factory = CreateFactory(ConnectionString);
        using var first = factory.GetConnection(ConnectionKeys.PostgreSql);
        using var second = factory.GetConnection(ConnectionKeys.PostgreSql);

        Assert.That(first, Is.TypeOf<NpgsqlConnection>());
        Assert.That(second, Is.Not.SameAs(first));
        Assert.That(first.State, Is.EqualTo(System.Data.ConnectionState.Closed));
        Assert.That(new NpgsqlConnectionStringBuilder(first.ConnectionString).Pooling, Is.True);
    }
    [TestCase(false, false)]
    [TestCase(true, true)]
    public void GetConnection_ExplicitPoolingMode_PreservesRequestedPolicy(bool pooling, bool expected)
    {
        var factory = CreateFactory(ConnectionString);
        using var connection = factory.GetConnection(ConnectionKeys.PostgreSql, pooling);

        Assert.That(new NpgsqlConnectionStringBuilder(connection.ConnectionString).Pooling, Is.EqualTo(expected));
    }

    [Test]
    public void GetConnection_PoolingDisabledInConfiguration_DoesNotEnableItImplicitly()
    {
        var factory = CreateFactory(ConnectionString + ";Pooling=false");
        using var connection = factory.GetConnection(ConnectionKeys.PostgreSql);

        Assert.That(new NpgsqlConnectionStringBuilder(connection.ConnectionString).Pooling, Is.False);
    }

    [Test]
    public void GetConnection_MissingConfiguration_ReturnsServiceUnavailable()
    {
        var error = Assert.Throws<CommonException>(() => CreateFactory("").GetConnection(ConnectionKeys.PostgreSql));

        Assert.That(error!.StatusCode, Is.EqualTo(503));
    }

    [Test]
    public void GetConnection_UnsupportedProvider_FailsExplicitly()
    {
        Assert.Throws<NotSupportedException>(() => CreateFactory(ConnectionString).GetConnection("Unknown"));
    }

    private static ConnectionProviderFactory CreateFactory(string connectionString)
        => new(Options.Create(new Database { Pikmin = connectionString }));
}

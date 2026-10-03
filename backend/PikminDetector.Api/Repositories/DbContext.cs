using System.Data.Common;

namespace PikminDetector.Api.Repositories;

public sealed class DbContext(IConnectionProviderFactory factory) : IDbContext
{
    public DbConnection CreateConnection(string providerName, bool pooling = true)
        => factory.GetConnection(providerName, pooling);
}

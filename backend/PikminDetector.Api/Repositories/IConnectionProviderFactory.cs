using System.Data.Common;

namespace PikminDetector.Api.Repositories;

public interface IConnectionProviderFactory
{
    DbConnection GetConnection(string providerName, bool pooling = true);
}

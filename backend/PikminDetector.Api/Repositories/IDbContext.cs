using System.Data.Common;

namespace PikminDetector.Api.Repositories;

public interface IDbContext
{
    DbConnection CreateConnection(string providerName, bool pooling = true);
}

using SQ = PCAxis.Query;
using SQA = PxWeb.Api2.Server.Models;

namespace sq_migrate.StorageBackends
{
    public interface ISaveQueryStorageBackend
    {
        IAsyncEnumerable<SQ.SavedQuery> GetSavedQueries();

        bool AlreadyMigrated(string id);

        bool StoreMigratedQuery(SQA.SavedQuery query);
    }
}

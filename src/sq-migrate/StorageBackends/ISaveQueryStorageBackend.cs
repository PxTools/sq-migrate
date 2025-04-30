using SQ = PCAxis.Query;
using SQA = PxWeb.Api2.Server.Models;

namespace sq_migrate.StorageBackends
{
    public interface ISaveQueryStorageBackend
    {

        IEnumerator<SQ.SavedQuery> GetSavedQueries();

        bool SavedQueryExists(string id);

        bool StoreSavedQuery(SQA.SavedQuery query);
    }
}

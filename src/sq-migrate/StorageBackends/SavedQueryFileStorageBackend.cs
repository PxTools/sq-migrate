namespace sq_migrate.StorageBackends
{
    public class SavedQueryFileStorageBackend : ISaveQueryStorageBackend
    {

        private readonly string _location;

        public SavedQueryFileStorageBackend(string location)
        {
            _location = location;
        }

        public IEnumerator<PCAxis.Query.SavedQuery> GetSavedQueries()
        {
            throw new NotImplementedException();
        }

        public bool SavedQueryExists(string id)
        {
            throw new NotImplementedException();
        }

        public bool StoreSavedQuery(PxWeb.Api2.Server.Models.SavedQuery query)
        {
            throw new NotImplementedException();
        }
    }
}

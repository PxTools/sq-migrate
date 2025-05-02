namespace sq_migrate.StorageBackends.DatabaseAccessor
{
    public interface IDatabaseAccessor
    {
        /// <summary>
        /// Get all queries from the database
        /// </summary>
        /// <returns></returns>
        IAsyncEnumerable<(int, string)> GetQueries();

        bool Exists(int id);

        void Save(int id, string savedQuery, string mainTable);
    }
}

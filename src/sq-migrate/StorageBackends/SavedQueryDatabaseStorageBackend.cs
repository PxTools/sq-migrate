using Spectre.Console;
using sq_migrate.StorageBackends.DatabaseAccessor;
using System.Text.Json;
using SQ = PCAxis.Query;
using SQA = PxWeb.Api2.Server.Models;

namespace sq_migrate.StorageBackends
{
    public class SavedQueryDatabaseStorageBackend : ISaveQueryStorageBackend
    {
        private readonly string _connectionString;
        private readonly string _owner;
        private readonly IDatabaseAccessor _databaseAccessor;

        public SavedQueryDatabaseStorageBackend(DatabaseTypes type, string connectionString, string owner)
        {
            _connectionString = connectionString;
            _owner = owner;
            if (type == DatabaseTypes.MSSQL)
            {
                _databaseAccessor = new SqlServerDataAccessor(_connectionString);
            }
            else if (type == DatabaseTypes.Oracle)
            {
                //TODO fix when implemented
                //_databaseAccessor = new OracleDatabaseAccessor(_connectionString, _databaseId);
            }
            else
            {
                throw new NotImplementedException($"Database type {type} is not supported");
            }
        }
        public bool AlreadyMigrated(string id)
        {
            try
            {
                return _databaseAccessor.Exists(int.Parse(id));
            }
            catch (Exception)
            {
                return true;
            }
        }

        public async IAsyncEnumerable<SQ.SavedQuery> GetSavedQueries()
        {
            await foreach (var (id, query) in _databaseAccessor.GetQueries())
            {
                var sq = SQ.JsonHelper.Deserialize<SQ.SavedQuery>(query) as SQ.SavedQuery;
                if (sq != null)
                {
                    sq.LoadedQueryName = id.ToString();
                    yield return sq;
                }
                else
                {
                    AnsiConsole.Markup($"{id} [red]Failed to parse query[/]\n");
                    continue;
                }

                yield return sq;
            }
        }

        public bool StoreMigratedQuery(SQA.SavedQuery query)
        {
            var savedQueryString = JsonSerializer.Serialize(query);
            int id = int.Parse(query.Id ?? "0");

            //TODO fix maintable
            _databaseAccessor.Save(id, savedQueryString, "N/A");

            return true;
        }
    }
}

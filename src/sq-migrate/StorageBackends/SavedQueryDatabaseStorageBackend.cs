using Spectre.Console;
using sq_migrate.Datasource;
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
        private readonly string _databaseType;
        private readonly string _databaseId;
        private readonly HashSet<string> _skipList;
        private readonly string _lastIdFilePath;

        public SavedQueryDatabaseStorageBackend(DatabaseTypes type, string connectionString, string owner, string databaseType, string databaseId, HashSet<string> skipList)
        {
            _connectionString = connectionString;
            _owner = owner;
            _databaseType = databaseType;
            _databaseId = databaseId;
            _skipList = skipList;

            var appDataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "sq-migrate");
            Directory.CreateDirectory(appDataDirectory);
            _lastIdFilePath = Path.Combine(appDataDirectory, "last-id.txt");

            if (type == DatabaseTypes.MSSQL)
            {
                _databaseAccessor = new SqlServerDataAccessor(_connectionString);
            }
            else if (type == DatabaseTypes.Oracle)
            {
                _databaseAccessor = new OracleDataAccessor(_connectionString, _owner);
            }
            else
            {
                throw new NotImplementedException($"Database type {type} is not supported");
            }

            _skipList = skipList;
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
            int beginFromId = -1;
            if (File.Exists(_lastIdFilePath))
            {
                string text = File.ReadAllText(_lastIdFilePath).Trim();
                int.TryParse(text, out beginFromId);
            }

            int counter = 0;

            await foreach (var (id, query) in _databaseAccessor.GetQueries(beginFromId))
            {
                if (_skipList.Contains(id.ToString()))
                {
                    continue;
                }

                var sq = SQ.JsonHelper.Deserialize<SQ.SavedQuery>(query) as SQ.SavedQuery;
                if (sq != null)
                {
                    sq.LoadedQueryName = id.ToString();
                    yield return sq;

                    //Updated last position
                    counter++;
                    if (counter % 100 == 0)
                    {
                        File.WriteAllText(_lastIdFilePath, id.ToString());
                    }
                }
                else
                {
                    AnsiConsole.Markup($"{id} [red]Failed to parse query[/]\n");
                    continue;
                }
            }
        }

        public bool StoreMigratedQuery(SQA.SavedQuery query, IDatasource datasource)
        {
            var savedQueryString = JsonSerializer.Serialize(query);
            int id = int.Parse(query.Id ?? "0");

            var maintable = datasource.ReverseLookup(query.TableId) ?? query.TableId;
            _databaseAccessor.Save(id, savedQueryString, maintable, _databaseType, _databaseId);

            return true;
        }
    }
}

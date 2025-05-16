using PCAxis.Query;
using Spectre.Console;
using sq_migrate.Datasource;
using System.Text.Json;

namespace sq_migrate.StorageBackends
{
    public class SavedQueryFileStorageBackend : ISaveQueryStorageBackend
    {

        private readonly string _location;
        private readonly HashSet<string> _skipList;

        public SavedQueryFileStorageBackend(string location, HashSet<string> skipList)
        {
            _location = location;
            _skipList = skipList;
        }

        public async IAsyncEnumerable<PCAxis.Query.SavedQuery> GetSavedQueries()
        {
            foreach (var srcFile in Directory.GetFiles(_location, "*.pxsq", SearchOption.AllDirectories))
            {
                var name = Path.GetFileNameWithoutExtension(srcFile);

                if (_skipList.Contains(name))
                {
                    continue;
                }

                string query = await File.ReadAllTextAsync(srcFile);
                var sq = JsonHelper.Deserialize<PCAxis.Query.SavedQuery>(query) as PCAxis.Query.SavedQuery;
                if (sq != null)
                {
                    sq.LoadedQueryName = Path.GetFileNameWithoutExtension(name);
                    yield return sq;
                }
                else
                {
                    AnsiConsole.Markup($"{name} [red]Failed to parse query[/]\n");
                    continue;
                }

            }
        }

        public bool AlreadyMigrated(string id)
        {
            return File.Exists(Path.Combine(_location, id + ".sqa"));
        }

        public bool StoreMigratedQuery(PxWeb.Api2.Server.Models.SavedQuery query, IDatasource datasource)
        {
            var path = Path.Combine(_location, (query.Id ?? "NN").Substring(0, 2), query.Id + ".sqa");

            //Make sure the directory exists
            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            }

            var savedQueryString = JsonSerializer.Serialize(query);
            File.WriteAllText(path, savedQueryString);

            return true;
        }
    }
}

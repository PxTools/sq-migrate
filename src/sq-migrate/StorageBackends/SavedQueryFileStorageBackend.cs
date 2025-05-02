using PCAxis.Query;
using Spectre.Console;
using System.Text.Json;

namespace sq_migrate.StorageBackends
{
    public class SavedQueryFileStorageBackend : ISaveQueryStorageBackend
    {

        private readonly string _location;

        public SavedQueryFileStorageBackend(string location)
        {
            _location = location;
        }

        public async IAsyncEnumerable<PCAxis.Query.SavedQuery> GetSavedQueries()
        {
            foreach (var srcFile in Directory.GetFiles(_location, "*.pxsq", SearchOption.AllDirectories))
            {
                var name = Path.GetFileNameWithoutExtension(srcFile);

                {
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
        }

        public bool AlreadyMigrated(string id)
        {
            return File.Exists(Path.Combine(_location, id + ".sqa"));
        }

        public bool StoreMigratedQuery(PxWeb.Api2.Server.Models.SavedQuery query)
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

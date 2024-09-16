using PCAxis.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sq_migrate
{
    public class StatsFileBackend : IStatsBackend
    {
        private readonly string _path;
        private readonly string[] _quieries;

        public StatsFileBackend(string path)
        {
            _path = path;
            _quieries = Directory.GetFiles(_path, "*.pxsq", SearchOption.AllDirectories);
        }

        public int NumberOfQueries()
        {
            return _quieries.Length;
        }

        public async IAsyncEnumerable<SavedQuery> GetQueries()
        {
            foreach (var queryPath in _quieries)
            {
                string query = await File.ReadAllTextAsync(queryPath);
                SavedQuery? sq = JsonHelper.Deserialize<SavedQuery>(query) as SavedQuery;
                if (sq != null)
                {
                    sq.LoadedQueryName = Path.GetFileNameWithoutExtension(queryPath);
                    yield return sq;
                }
                else
                {
                    Console.WriteLine($"Failed to parse query: {queryPath}");
                }
            }
        }
    }
}

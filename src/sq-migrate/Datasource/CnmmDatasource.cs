using PCAxis.Paxiom;

namespace sq_migrate.Datasource
{
    public class CnmmDatasource : IDatasource
    {
        private readonly string _databaseId;
        private readonly Dictionary<string, string> _lookup;


        public CnmmDatasource(string databaseId)
        {
            _databaseId = databaseId;
            _lookup = new Dictionary<string, string>();
            InitializeLookup();
        }

        private void InitializeLookup()
        {
            //TODO : Fix hardcoded language
            var values = PCAxis.Sql.ApiUtils.ApiUtilStatic.GetMenuLookupTables("sv");
            foreach (var value in values)
            {
                var maintable = value.Value.Selection;
                var tableId = value.Key;
                if (!_lookup.ContainsKey(maintable))
                {
                    _lookup.Add(maintable, tableId);
                }
            }
        }


        public IPXModelBuilder? GetBuiler(string table, string language)
        {
            var builder = new PCAxis.PlugIn.Sql.PXSQLBuilder();
            string id = GetTableId(table);
            builder.SetPath($"{_databaseId}:{id}");
            builder.SetPreferredLanguage(language);
            return builder;

        }

        public string? ResolveTableId(string path)
        {
            var tableId = GetTableId(path);
            return _lookup[tableId];
        }

        private static string GetTableId(string table)
        {
            return table.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries).Last();
        }
    }
}

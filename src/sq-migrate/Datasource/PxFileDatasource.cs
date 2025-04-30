using PCAxis.Paxiom;
using System.Xml;

namespace sq_migrate.Datasource
{
    public class PxFileDatasource : IDatasource
    {
        private readonly string _databasePath;
        private readonly Dictionary<string, string> _lookup;
        public PxFileDatasource(string databasePath)
        {
            _lookup = GetMap(databasePath);
            // Removes the root folder from the database path
            _databasePath = Path.GetDirectoryName(databasePath) ?? "";
        }

        private static Dictionary<string, string> GetMap(string sourcePath)
        {
            var lookup = new Dictionary<string, string>();
            var menuFile = Path.Combine(sourcePath, "Menu.xml");

            var xdoc = new XmlDocument();
            xdoc.Load(menuFile);

            string xpath = "//Link";
            var nodeList = xdoc.SelectNodes(xpath);

            if (nodeList != null)
            {
                foreach (XmlElement childEl in nodeList)
                {
                    string selection = childEl.GetAttribute("selection").Replace('\\', '/');
                    string tableId = childEl.GetAttribute("tableId").ToUpper();
                    if (!lookup.ContainsKey(selection))
                    {
                        lookup.Add(selection, tableId);
                    }
                }

            }

            return lookup;
        }

        public IPXModelBuilder? GetBuiler(string table, string language)
        {
            var builder = new PCAxis.Paxiom.PXFileBuilder();

            var path = Path.Combine(_databasePath, table);

            if (!File.Exists(path))
            {
                return null;
            }

            builder.SetPath(path);
            builder.SetPreferredLanguage(language);
            return builder;
        }

        public string? ResolveTableId(string path)
        {
            return _lookup[path];
        }
    }
}

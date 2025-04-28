using PCAxis.Paxiom;

namespace sq_migrate.Datasource
{
    public class PxFileDatasource : IDatasource
    {


        private readonly string _databasePath;

        public PxFileDatasource(string databasePath)
        {
            // Removes the root folder from the database path
            _databasePath = Path.GetDirectoryName(databasePath);
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
    }
}

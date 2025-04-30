using PCAxis.Paxiom;

namespace sq_migrate.Datasource
{
    public class CnmmDatasource : IDatasource
    {


        public IPXModelBuilder? GetBuiler(string table, string language)
        {
            var builder = new PCAxis.PlugIn.Sql.PXSQLBuilder();

            builder.SetPath(table);
            builder.SetPreferredLanguage(language);
            return builder;

        }

        public string? ResolveTableId(string path)
        {
            //PCAxis.Sql.ApiUtils.ApiUtilStatic.GetMenuLookupTables(language)
            throw new NotImplementedException();
        }
    }
}

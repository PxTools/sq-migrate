using PCAxis.Paxiom;

namespace sq_migrate.Datasource
{
    public interface IDatasource
    {
        IPXModelBuilder? GetBuiler(string table, string language);

        string? ResolveTableId(string path);

        string? ReverseLookup(string tableId);
    }
}

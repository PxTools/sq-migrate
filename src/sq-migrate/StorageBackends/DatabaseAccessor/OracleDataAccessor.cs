using Oracle.ManagedDataAccess.Client;
using System.Text.RegularExpressions;

namespace sq_migrate.StorageBackends.DatabaseAccessor
{
    public class OracleDataAccessor : IDatabaseAccessor
    {

        private readonly string _schema;
        private readonly string _connectionString;

        public OracleDataAccessor(string connectionString, string schema)
        {
            _connectionString = connectionString;
            _schema = SanitizeSchema(schema);
        }

        public bool Exists(int id)
        {
            using (var conn = new OracleConnection(_connectionString))
            {
                conn.Open();
                var command = new OracleCommand($"SELECT COUNT(*) FROM {_schema}.SavedQueryMeta2 WHERE QueryId = :id", conn);
                command.Parameters.Add("id", id);
                var count = (int)command.ExecuteScalar();
                return count > 0;
            }
        }

        public async IAsyncEnumerable<(int, string)> GetQueries(int beginFromId)
        {
            using (var conn = new OracleConnection(_connectionString))
            {
                conn.Open();
                var command = new OracleCommand("SELECT QueryId, QueryText FROM SavedQueryMeta where QueryId > :id and QueryId not in (SELECT QueryId FROM SavedQueryMeta2)", conn);
                command.Parameters.Add("id", beginFromId);
                var reader = command.ExecuteReader();
                while (await reader.ReadAsync())
                {
                    var id = reader.GetInt32(0);
                    var query = reader.GetString(1);
                    yield return (id, query);
                }
            }
        }

        public void Save(int id, string savedQuery, string mainTable, string databaseType, string databaseId)
        {
            using (var conn = new OracleConnection(_connectionString))
            {
                conn.Open();
                var cmd = new OracleCommand(
                    $@" BEGIN
                        insert into 
                        {_schema}.SavedQueryMeta2
                        (
                            QueryId,
	                        DataSourceType, 
	                        DatabaseId, 
	                        DataSourceId, 
	                        ""Status"", 
	                        StatusUse, 
	                        StatusChange, 
	                        OwnerId, 
	                        MyDescription, 
	                        CreatedDate, 
	                        SavedQueryFormat, 
	                        SavedQueryStorage, 
	                        QueryText,
                            Runs,
                            Fails
                        )
                        values
                        (
                            :id,
	                        :databaseType,
	                        :databaseId,
	                        :mainTable,
	                        'A',
	                        'P',
	                        'P',
	                        'Anonymous',
	                        :title,
	                        sysdate,
	                        'SQA',
	                        'D',
	                        :query,
                            0,
	                        0
                        ); END;", conn);
                cmd.BindByName = true;
                cmd.Parameters.Add("id", id);
                cmd.Parameters.Add("databaseType", databaseType);
                cmd.Parameters.Add("databaseId", databaseId);
                cmd.Parameters.Add("mainTable", mainTable);
                cmd.Parameters.Add("title", " ");
                cmd.Parameters.Add("query", savedQuery);

                cmd.ExecuteNonQuery();
            }
        }

        public static string SanitizeSchema(string schema)
        {
            return Regex.Replace(schema, @"[^a-zA-Z0-9_-]", "", RegexOptions.None, TimeSpan.FromMicroseconds(100));
        }
    }
}

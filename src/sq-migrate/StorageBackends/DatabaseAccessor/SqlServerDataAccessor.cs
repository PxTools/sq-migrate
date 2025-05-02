using Microsoft.Data.SqlClient;

namespace sq_migrate.StorageBackends.DatabaseAccessor
{
    public class SqlServerDataAccessor : IDatabaseAccessor
    {
        private readonly string _connectionString;

        public SqlServerDataAccessor(string connectionString)
        {
            _connectionString = connectionString;
        }

        public bool Exists(int id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                var command = new SqlCommand("SELECT COUNT(*) FROM SavedQueryMeta2 WHERE QueryId = @id", conn);
                command.Parameters.AddWithValue("@id", id);
                var count = (int)command.ExecuteScalar();
                return count > 0;
            }
        }

        public async IAsyncEnumerable<(int, string)> GetQueries()
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                var command = new SqlCommand("SELECT QueryId, QueryText FROM SavedQueryMeta", conn);
                var reader = command.ExecuteReader();
                while (await reader.ReadAsync())
                {
                    var id = reader.GetInt32(0);
                    var query = reader.GetString(1);
                    yield return (id, query);
                }
            }
        }


        public void Save(int id, string savedQuery, string mainTable)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand(
                    @"  SET IDENTITY_INSERT SavedQueryMeta2 ON;
                        insert into 
                        SavedQueryMeta2
                        (
                            QueryId,
	                        DataSourceType, 
	                        DatabaseId, 
	                        DataSourceId, 
	                        [Status], 
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
                            @id,
	                        @databaseType,
	                        @databaseId,
	                        @mainTable,
	                        'A',
	                        'P',
	                        'P',
	                        'Anonymous',
	                        @title,
	                        @creationDate,
	                        'PXSJSON',
	                        'D',
	                        @query,
                            0,
	                        0
                        );SET IDENTITY_INSERT SavedQueryMeta2 OFF;", conn);
                cmd.Parameters.AddWithValue("id", id);
                //TODO fix hardcoded values
                cmd.Parameters.AddWithValue("databaseType", "CNMM");
                //TODO fix hardcoded values
                cmd.Parameters.AddWithValue("databaseId", "MyDB");
                cmd.Parameters.AddWithValue("mainTable", mainTable);
                cmd.Parameters.AddWithValue("title", "");
                cmd.Parameters.AddWithValue("creationDate", DateTime.Now);
                cmd.Parameters.AddWithValue("query", savedQuery);
                int rowAffected = Convert.ToInt32(cmd.ExecuteNonQuery());

            }

        }
    }
}

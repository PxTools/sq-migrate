using Microsoft.Data.SqlClient;

namespace sq_migrate.StorageBackends.DatabaseAccessor
{
    public class SqlServerDataAccessor : IDatabaseAccessor
    {
        private readonly string _connectionString;
        private readonly SqlConnection conn;

        public SqlServerDataAccessor(string connectionString)
        {
            _connectionString = connectionString;
            conn = new SqlConnection(_connectionString);
        }

        public bool Exists(int id)
        {
            //using (var conn = new SqlConnection(_connectionString))
            {
                if (conn.State != System.Data.ConnectionState.Open)
                {
                    conn.Open();
                }

                var command = new SqlCommand("SELECT COUNT(*) FROM SavedQueryMeta2 WHERE QueryId = @id", conn);
                command.Parameters.AddWithValue("@id", id);
                var count = (int)command.ExecuteScalar();
                return count > 0;
            }
        }

        public async IAsyncEnumerable<(int, string)> GetQueries(int beginFromId)
        {
            //using (var conn = new SqlConnection(_connectionString))
            {
                if (conn.State != System.Data.ConnectionState.Open)
                {
                    conn.Open();
                }
                var command = new SqlCommand("SELECT QueryId, QueryText FROM SavedQueryMeta where QueryId > @id", conn);
                command.Parameters.AddWithValue("@id", beginFromId);
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
            //using (var conn = new SqlConnection(_connectionString))
            {
                if (conn.State != System.Data.ConnectionState.Open)
                {
                    conn.Open();
                }
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
	                        'SQA',
	                        'D',
	                        @query,
                            0,
	                        0
                        );SET IDENTITY_INSERT SavedQueryMeta2 OFF;", conn);
                cmd.Parameters.AddWithValue("id", id);
                cmd.Parameters.AddWithValue("databaseType", databaseType);
                cmd.Parameters.AddWithValue("databaseId", databaseId);
                cmd.Parameters.AddWithValue("mainTable", mainTable);
                cmd.Parameters.AddWithValue("title", "");
                cmd.Parameters.AddWithValue("creationDate", DateTime.Now);
                cmd.Parameters.AddWithValue("query", savedQuery);
                int rowAffected = Convert.ToInt32(cmd.ExecuteNonQuery());

            }

        }
    }
}

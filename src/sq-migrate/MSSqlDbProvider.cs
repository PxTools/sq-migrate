using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sq_migrate
{
    internal class MSSqlDbProvider : IDbProvider
    {
        public DbCommand CreateCommand(string commandText, DbConnection connection)
        {
            return new Microsoft.Data.SqlClient.SqlCommand(commandText, connection as Microsoft.Data.SqlClient.SqlConnection);
        }

        public DbConnection CreateConnection(string connectionString)
        {
            return new Microsoft.Data.SqlClient.SqlConnection(connectionString);
        }
    }
}

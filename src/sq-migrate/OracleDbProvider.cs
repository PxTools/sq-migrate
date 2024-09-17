using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sq_migrate
{
    public class OracleDbProvider : IDbProvider
    {
        public DbCommand CreateCommand(string commandText, DbConnection connection)
        {
            return new Oracle.ManagedDataAccess.Client.OracleCommand(commandText, connection as Oracle.ManagedDataAccess.Client.OracleConnection);
        }

        public DbConnection CreateConnection(string connectionString)
        {
            return new Oracle.ManagedDataAccess.Client.OracleConnection(connectionString);
        }
    }
}

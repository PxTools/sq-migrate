using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sq_migrate
{
    public interface IDbProvider
    {
        DbConnection CreateConnection(string connectionString);
        DbCommand CreateCommand(string commandText, DbConnection connection);
    }
}

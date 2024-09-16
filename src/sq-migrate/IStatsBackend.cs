using PCAxis.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sq_migrate
{
    public interface IStatsBackend
    {
        int NumberOfQueries();
        IAsyncEnumerable<SavedQuery> GetQueries();
    }
}

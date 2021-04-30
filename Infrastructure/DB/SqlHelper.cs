using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.DB
{
    public interface SqlHelper
    {
        int ExecuteNonQuery(string sql, params object[] parameters);
        DataTable Query(string sql, params object[] parameters);
    }
}

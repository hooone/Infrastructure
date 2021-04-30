using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.DB
{
    [AttributeUsage(AttributeTargets.Method)]
    public class SqlKey : Attribute
    {
        public string Column { get; set;  }
        public SqlKey(string column)
        {

        }
    }
}

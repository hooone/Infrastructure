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
        public List<string> Column { get; set; }
        public SqlKey(params string[] column)
        {
            Column = column.ToList();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.DB
{
    [AttributeUsage(AttributeTargets.Method)]
    public class DbUpdate : Attribute
    {
    }
}

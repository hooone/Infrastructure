using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Infrastructure.DB
{
    [AttributeUsage(AttributeTargets.Method)]
    public class DbRead : Attribute
    {
    }
}

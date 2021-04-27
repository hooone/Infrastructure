using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Infrastructure.DB
{
    [AttributeUsage(AttributeTargets.Class)]
    public class DbTable : Attribute
    {
        public string Name { get; set; }
        public DbTable(string name)
        {
            Name = name;
        }
    }
}

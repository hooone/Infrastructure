using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Infrastructure.DB
{
    [AttributeUsage(AttributeTargets.Property)]
    public class DbConnectionString : Attribute
    {
        /// <summary>
        /// 
        /// </summary>
        public DbConnectionString(string database, string user, string password)
        {

        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Log
{
    public class NopLogger : ILog
    {
        public void Debug(string msg)
        {
        }

        public void Info(string msg)
        {
        }

        public void Release(string msg)
        {
        }
    }
}

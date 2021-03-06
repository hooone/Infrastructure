using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Log
{
    public interface ILog
    {
        void Info(string msg);
        void Debug(string msg);
        void Release(string msg);
        void Error(string msg);
        void Error(Exception e);
        void Error(string msg, Exception e);
        ILog SubLogger(string name);
    }
}

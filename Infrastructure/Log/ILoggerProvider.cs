using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Log
{
    public interface ILoggerProvider
    {
        ILog Logger { get; }
        void WithLogger(ILog logger);
    }
}

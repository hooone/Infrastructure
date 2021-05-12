using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.SocketClient
{
    public class ErrorEventArgs : EventArgs
    {
        public Exception Exception { get; private set; }

        public ErrorEventArgs(Exception exception)
        {
            Exception = exception;
        }
    }
}

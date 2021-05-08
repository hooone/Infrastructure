using Infrastructure.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.SocketServer.Server
{
    interface ISocketServer : ILoggerProvider
    {
        bool Start();

        bool IsRunning { get; }

        void Stop();
    }
}

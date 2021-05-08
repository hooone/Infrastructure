using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.SocketServer.Server
{
    internal interface IAsyncSocketSession
    {
        void ProcessReceive(SocketAsyncEventArgs e);
    }
}

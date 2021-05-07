using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.SocketServer
{
    interface IAsyncSocketSessionBase 
    {
        SocketAsyncEventArgsProxy SocketAsyncProxy { get; }

        Socket Client { get; }
    }

    interface IAsyncSocketSession : IAsyncSocketSessionBase
    {
        void ProcessReceive(SocketAsyncEventArgs e);
    }
}

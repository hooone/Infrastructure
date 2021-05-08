using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.SocketServer.Server
{
    internal interface ISocketSession : ISessionBase
    {
        IPEndPoint LocalEndPoint { get; }
        Action<ISocketSession, CloseReason> Closed { get; set; }
        void Initialize(IAppSession appSession);
        void Start();
        void Close(CloseReason reason);
    }
}

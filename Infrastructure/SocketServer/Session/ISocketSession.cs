using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.SocketServer
{
    internal interface ISocketSession : ISessionBase
    {
        IPEndPoint LocalEndPoint { get; }
        Action<ISocketSession, CloseReason> Closed { get; set; }
        void Initialize(IAppSession appSession);
        void Start();
        bool TrySend(ArraySegment<byte> segment);
        void Close(CloseReason reason);
    }
}

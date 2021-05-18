using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.SocketClient
{
    public interface IClientSession
    {
        Socket Socket { get; }

        bool Send(ArraySegment<byte> segment);
        void Close();
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.SocketClient
{
    public interface IClient
    {
        bool IsConnected { get; }
        event RequestHandler NewRequestReceived;
        Task<bool> ConnectAsync(string ip, int port);
        bool Send(byte[] data);
        Task<bool> Close();
    }
}

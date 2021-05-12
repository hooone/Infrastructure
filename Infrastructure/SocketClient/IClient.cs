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
        event EventHandler<byte[]> NewPackageReceived;
        Task<bool> ConnectAsync(EndPoint remoteEndPoint);
        void Send(byte[] data);
        Task<bool> Close();
    }
}

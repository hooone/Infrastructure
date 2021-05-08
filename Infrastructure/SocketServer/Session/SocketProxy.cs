using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.SocketServer.Server
{
    class SocketProxy
    {
        public SocketAsyncEventArgs SocketEventArgs { get; private set; }

        public int OrigOffset { get; private set; }

        public bool IsRecyclable { get; private set; }

        public SocketProxy(SocketAsyncEventArgs socketEventArgs)
        {
            SocketEventArgs = socketEventArgs;
            OrigOffset = socketEventArgs.Offset;
            SocketEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnReceive);
            IsRecyclable = true;
        }

        public void Initialize(IAsyncSocketSession socketSession)
        {
            SocketEventArgs.UserToken = socketSession;
        }

        public void Reset()
        {
            SocketEventArgs.UserToken = null;
        }

        static void OnReceive(object sender, SocketAsyncEventArgs e)
        {
            var socketSession = e.UserToken as IAsyncSocketSession;

            if (socketSession == null)
                return;

            if (e.LastOperation == SocketAsyncOperation.Receive)
            {
                Async.AsyncRun(() => socketSession.ProcessReceive(e));
            }
            else
            {
                throw new ArgumentException("The last operation completed on the socket was not a receive");
            }
        }

    }
}

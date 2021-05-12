using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.SocketClient
{
    public delegate void ConnectedCallback(Socket socket, object state, SocketAsyncEventArgs e, Exception exception);
    public static class ConnectAsyncExtension
    {
        internal static bool PreferIPv4Stack()
        {
            return Environment.GetEnvironmentVariable("PREFER_IPv4_STACK") != null;
        }

        public static void ConnectAsync(this EndPoint remoteEndPoint, EndPoint localEndPoint, ConnectedCallback callback, object state)
        {
            var e = CreateSocketAsyncEventArgs(remoteEndPoint, callback, state);

            var socket = PreferIPv4Stack()
                ? new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
                : new Socket(SocketType.Stream, ProtocolType.Tcp);

            if (localEndPoint != null)
            {
                try
                {
                    socket.ExclusiveAddressUse = false;
                    socket.Bind(localEndPoint);
                }
                catch (Exception exc)
                {
                    callback(null, state, null, exc);
                    return;
                }
            }

            socket.ConnectAsync(e);
        }
        class ConnectToken
        {
            public object State { get; set; }

            public ConnectedCallback Callback { get; set; }
        }

        static void SocketAsyncEventCompleted(object sender, SocketAsyncEventArgs e)
        {
            e.Completed -= SocketAsyncEventCompleted;
            var token = (ConnectToken)e.UserToken;
            e.UserToken = null;
            token.Callback(sender as Socket, token.State, e, null);
        }

        static SocketAsyncEventArgs CreateSocketAsyncEventArgs(EndPoint remoteEndPoint, ConnectedCallback callback, object state)
        {
            var e = new SocketAsyncEventArgs();

            e.UserToken = new ConnectToken
            {
                State = state,
                Callback = callback
            };

            e.RemoteEndPoint = remoteEndPoint;
            e.Completed += new EventHandler<SocketAsyncEventArgs>(SocketAsyncEventCompleted);

            return e;
        }
    }
}
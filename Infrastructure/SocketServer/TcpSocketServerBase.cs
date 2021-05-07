using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.SocketServer
{
    abstract class TcpSocketServerBase : SocketServerBase
    {
        private readonly byte[] m_KeepAliveOptionValues;
        private readonly byte[] m_KeepAliveOptionOutValues;
        private readonly int m_SendTimeOut;
        private readonly int m_ReceiveBufferSize;
        private readonly int m_SendBufferSize;

        public TcpSocketServerBase(ListenerInfo[] listeners)
            : base(listeners)
        {
            uint dummy = 0;
            m_KeepAliveOptionValues = new byte[Marshal.SizeOf(dummy) * 3];
            m_KeepAliveOptionOutValues = new byte[m_KeepAliveOptionValues.Length];
            //whether enable KeepAlive
            BitConverter.GetBytes((uint)1).CopyTo(m_KeepAliveOptionValues, 0);
            //how long will start first keep alive
            BitConverter.GetBytes((uint)(ServerConfig.DefaultKeepAliveTime * 1000)).CopyTo(m_KeepAliveOptionValues, Marshal.SizeOf(dummy));
            //keep alive interval
            BitConverter.GetBytes((uint)(ServerConfig.DefaultKeepAliveInterval * 1000)).CopyTo(m_KeepAliveOptionValues, Marshal.SizeOf(dummy) * 2);

            m_SendTimeOut = ServerConfig.DefaultSendTimeout;
            m_ReceiveBufferSize = ServerConfig.DefaultReceiveBufferSize;
            m_SendBufferSize = ServerConfig.DefaultSendBufferSize;
        }

        protected IAppSession CreateSession(Socket client, ISocketSession session)
        {
            if (m_SendTimeOut > 0)
                client.SendTimeout = m_SendTimeOut;

            if (m_ReceiveBufferSize > 0)
                client.ReceiveBufferSize = m_ReceiveBufferSize;

            if (m_SendBufferSize > 0)
                client.SendBufferSize = m_SendBufferSize;

            if (!Platform.SupportSocketIOControlByCodeEnum)
                client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, m_KeepAliveOptionValues);
            else
                client.IOControl(IOControlCode.KeepAliveValues, m_KeepAliveOptionValues, m_KeepAliveOptionOutValues);

            client.NoDelay = true;
            client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);

            return null;
            //return this.AppServer.CreateAppSession(session);
        }

        protected override ISocketListener CreateListener(ListenerInfo listenerInfo)
        {
            return new TcpAsyncSocketListener(listenerInfo);
        }
    }
}

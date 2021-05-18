using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.SocketClient
{
    public abstract class TcpClientSession : ClientSession
    {
        protected string HostName { get; private set; }

        private bool m_InConnecting = false;
        public override EndPoint LocalEndPoint
        {
            get
            {
                return base.LocalEndPoint;
            }

            set
            {
                if (m_InConnecting || IsConnected)
                    throw new Exception("You cannot set LocalEndPoint after you start the connection.");

                base.LocalEndPoint = value;
            }
        }
        public override int ReceiveBufferSize
        {
            get
            {
                return base.ReceiveBufferSize;
            }

            set
            {
                if (Buffer.Array != null)
                    throw new Exception("ReceiveBufferSize cannot be set after the socket has been connected!");

                base.ReceiveBufferSize = value;
            }
        }
        #region Connect
        public override void Connect(EndPoint remoteEndPoint)
        {
            if (remoteEndPoint == null)
                throw new ArgumentNullException("remoteEndPoint");

            var dnsEndPoint = remoteEndPoint as DnsEndPoint;

            if (dnsEndPoint != null)
            {
                var hostName = dnsEndPoint.Host;

                if (!string.IsNullOrEmpty(hostName))
                    HostName = hostName;
            }

            if (m_InConnecting)
                throw new Exception("The socket is connecting, cannot connect again!");

            if (Client != null)
                throw new Exception("The socket is connected, you needn't connect again!");

            m_InConnecting = true;

            remoteEndPoint.ConnectAsync(LocalEndPoint, ProcessConnect, null);
        }

        protected abstract void SocketEventArgsCompleted(object sender, SocketAsyncEventArgs e);
        protected abstract void OnGetSocket(SocketAsyncEventArgs e);
        protected void ProcessConnect(Socket socket, object state, SocketAsyncEventArgs e, Exception exception)
        {
            if (exception != null)
            {
                m_InConnecting = false;
                OnError(exception);

                if (e != null)
                    e.Dispose();

                return;
            }

            if (e != null && e.SocketError != SocketError.Success)
            {
                m_InConnecting = false;
                OnError(new SocketException((int)e.SocketError));
                e.Dispose();
                return;
            }

            if (socket == null)
            {
                m_InConnecting = false;
                OnError(new SocketException((int)SocketError.ConnectionAborted));
                return;
            }

            if (!socket.Connected)
            {
                m_InConnecting = false;

                var socketError = SocketError.HostUnreachable;

                try
                {
                    socketError = (SocketError)socket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Error);
                }
                catch (Exception)
                {
                    socketError = SocketError.HostUnreachable;
                }
                OnError(new SocketException((int)socketError));
                return;
            }

            if (e == null)
                e = new SocketAsyncEventArgs();

            e.Completed += SocketEventArgsCompleted;

            Client = socket;
            m_InConnecting = false;

            try
            {
                // mono may throw an exception here
                LocalEndPoint = socket.LocalEndPoint;
            }
            catch
            {
            }
            var finalRemoteEndPoint = e.RemoteEndPoint != null ? e.RemoteEndPoint : socket.RemoteEndPoint;

            if (string.IsNullOrEmpty(HostName))
            {
                HostName = GetHostOfEndPoint(finalRemoteEndPoint);
            }
            else// connect with DnsEndPoint
            {
                var finalDnsEndPoint = finalRemoteEndPoint as DnsEndPoint;

                if (finalDnsEndPoint != null)
                {
                    var hostName = finalDnsEndPoint.Host;

                    if (!string.IsNullOrEmpty(hostName) && !HostName.Equals(hostName, StringComparison.OrdinalIgnoreCase))
                        HostName = hostName;
                }
            }

            try
            {
                //Set keep alive
                Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            }
            catch
            {
            }

            OnGetSocket(e);
        }

        private string GetHostOfEndPoint(EndPoint endPoint)
        {
            var dnsEndPoint = endPoint as DnsEndPoint;

            if (dnsEndPoint != null)
            {
                return dnsEndPoint.Host;
            }

            var ipEndPoint = endPoint as IPEndPoint;

            if (ipEndPoint != null && ipEndPoint.Address != null)
                return ipEndPoint.Address.ToString();

            return string.Empty;
        }
        #endregion

        #region State

        private int m_IsSending = 0;

        protected bool IsSending
        {
            get { return m_IsSending == 1; }
        }
        protected override bool DetectConnected()
        {
            if (Client != null)
                return true;
            OnError(new SocketException((int)SocketError.NotConnected));
            return false;
        }

        protected bool EnsureSocketClosed()
        {
            return EnsureSocketClosed(null);
        }

        protected bool EnsureSocketClosed(Socket prevClient)
        {
            var client = Client;

            if (client == null)
                return false;

            var fireOnClosedEvent = true;

            if (prevClient != null && prevClient != client)//originalClient is previous disconnected socket, so we needn't fire event for it
            {
                client = prevClient;
                fireOnClosedEvent = false;
            }
            else
            {
                Client = null;
                m_IsSending = 0;
            }

            try
            {
                client.Shutdown(SocketShutdown.Both);
            }
            catch
            { }
            finally
            {
                try
                {
                    client.Close();
                }
                catch
                { }
            }

            return fireOnClosedEvent;
        }

        protected bool IsIgnorableSocketError(int errorCode)
        {
            //SocketError.Shutdown = 10058
            //SocketError.ConnectionAborted = 10053
            //SocketError.ConnectionReset = 10054
            //SocketError.OperationAborted = 995
            if (errorCode == 10058 || errorCode == 10053 || errorCode == 10054 || errorCode == 995)
                return true;

            return false;
        }

        protected virtual bool IsIgnorableException(Exception e)
        {
            if (e is System.ObjectDisposedException)
                return true;

            if (e is NullReferenceException)
                return true;

            return false;
        }
        #endregion

        #region Send
        protected abstract void SendInternal(PosList<ArraySegment<byte>> items);

        protected void OnSendingCompleted()
        {
            var sendingItems = GetSendingItems();
            sendingItems.Clear();
            sendingItems.Position = 0;

            if (!m_SendingQueue.TryDequeue(sendingItems))
            {
                m_IsSending = 0;
                return;
            }

            SendInternal(sendingItems);
        }
        public override bool TrySend(ArraySegment<byte> segment)
        {
            if (segment.Array == null || segment.Count == 0)
            {
                return true;
            }

            var isEnqueued = GetSendingQueue().Enqueue(segment);

            if (Interlocked.CompareExchange(ref m_IsSending, 1, 0) != 0)
                return isEnqueued;

            DequeueSend();

            return isEnqueued;
        }

        private IBatchQueue<ArraySegment<byte>> m_SendingQueue;

        private IBatchQueue<ArraySegment<byte>> GetSendingQueue()
        {
            if (m_SendingQueue != null)
                return m_SendingQueue;

            lock (this)
            {
                if (m_SendingQueue != null)
                    return m_SendingQueue;

                //Sending queue size must be greater than 3
                m_SendingQueue = new ConcurrentBatchQueue<ArraySegment<byte>>(Math.Max(SendingQueueSize, 1024), (t) => t.Array == null || t.Count == 0);
                return m_SendingQueue;
            }
        }

        private PosList<ArraySegment<byte>> m_SendingItems;

        private PosList<ArraySegment<byte>> GetSendingItems()
        {
            if (m_SendingItems == null)
                m_SendingItems = new PosList<ArraySegment<byte>>();

            return m_SendingItems;
        }
        private void DequeueSend()
        {
            var sendingItems = GetSendingItems();

            if (!m_SendingQueue.TryDequeue(sendingItems))
            {
                m_IsSending = 0;
                return;
            }

            SendInternal(sendingItems);
        }

        #endregion


        #region Close

        public override void Close()
        {
            if (EnsureSocketClosed())
                OnClosed();
        }
        #endregion
    }
}

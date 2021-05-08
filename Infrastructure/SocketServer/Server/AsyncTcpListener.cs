using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.SocketServer.Server
{
    class AsyncTcpListener : ISocketListener
    {
        public ListenerInfo Info { get; private set; }

        private int m_ListenBackLog;

        private Socket m_ListenSocket;

        private SocketAsyncEventArgs m_AcceptSAE;

        public AsyncTcpListener(ListenerInfo info)
        {
            Info = info;
            m_ListenBackLog = ServerConfig.DefaultListenBacklog;
        }

        public event NewClientAcceptHandler NewClientAccepted;
        public event ErrorHandler Error;
        public event EventHandler Stopped;
        protected virtual void OnNewClientAccepted(Socket socket, object state)
        {
            var handler = NewClientAccepted;

            if (handler != null)
                handler(this, socket, state);
        }
        protected void OnError(Exception e)
        {
            var handler = Error;

            if (handler != null)
                handler(this, e);
        }
        protected void OnStopped()
        {
            var handler = Stopped;

            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        public bool Start()
        {
            m_ListenSocket = new Socket(this.Info.EndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                m_ListenSocket.Bind(this.Info.EndPoint);
                m_ListenSocket.Listen(m_ListenBackLog);

                m_ListenSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                m_ListenSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);

                SocketAsyncEventArgs acceptEventArg = new SocketAsyncEventArgs();
                m_AcceptSAE = acceptEventArg;
                acceptEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(OnConnect);

                if (!m_ListenSocket.AcceptAsync(acceptEventArg))
                    ProcessConnect(acceptEventArg);

                return true;

            }
            catch (Exception e)
            {
                OnError(e);
                return false;
            }
        }
        void OnConnect(object sender, SocketAsyncEventArgs e)
        {
            ProcessConnect(e);
        }
        void ProcessConnect(SocketAsyncEventArgs e)
        {
            Socket socket = null;

            if (e.SocketError != SocketError.Success)
            {
                var errorCode = (int)e.SocketError;

                //The listen socket was closed
                if (errorCode == 995 || errorCode == 10004 || errorCode == 10038)
                    return;

                OnError(new SocketException(errorCode));
            }
            else
            {
                socket = e.AcceptSocket;
            }

            e.AcceptSocket = null;

            bool willRaiseEvent = false;

            try
            {
                willRaiseEvent = m_ListenSocket.AcceptAsync(e);
            }
            catch (ObjectDisposedException)
            {
                //The listener was stopped
                //Do nothing
                //make sure ProcessAccept won't be executed in this thread
                willRaiseEvent = true;
            }
            catch (NullReferenceException)
            {
                //The listener was stopped
                //Do nothing
                //make sure ProcessAccept won't be executed in this thread
                willRaiseEvent = true;
            }
            catch (Exception exc)
            {
                OnError(exc);
                //make sure ProcessAccept won't be executed in this thread
                willRaiseEvent = true;
            }

            if (socket != null)
                OnNewClientAccepted(socket, null);

            if (!willRaiseEvent)
                ProcessConnect(e);
        }


        public  void Stop()
        {
            if (m_ListenSocket == null)
                return;

            lock (this)
            {
                if (m_ListenSocket == null)
                    return;

                m_AcceptSAE.Completed -= new EventHandler<SocketAsyncEventArgs>(OnConnect);
                m_AcceptSAE.Dispose();
                m_AcceptSAE = null;

                try
                {
                    m_ListenSocket.Close();
                }
                finally
                {
                    m_ListenSocket = null;
                }
            }

            OnStopped();
        }
    }
}

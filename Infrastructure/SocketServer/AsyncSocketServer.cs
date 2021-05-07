using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.SocketServer
{
    class AsyncSocketServer : TcpSocketServerBase
    {
        public AsyncSocketServer(ListenerInfo[] listeners)
            : base(listeners)
        {

        }

        private BufferManager m_BufferManager;

        private ConcurrentStack<SocketAsyncEventArgsProxy> m_ReadWritePool;

        public override bool Start()
        {
            try
            {
                int bufferSize = ServerConfig.DefaultReceiveBufferSize;

                if (bufferSize <= 0)
                    bufferSize = 1024 * 4;

                m_BufferManager = new BufferManager(bufferSize * ServerConfig.DefaultMaxConnectionNumber, bufferSize);

                try
                {
                    m_BufferManager.InitBuffer();
                }
                catch (Exception e)
                {
                    return false;
                }

                // preallocate pool of SocketAsyncEventArgs objects
                SocketAsyncEventArgs socketEventArg;

                var socketArgsProxyList = new List<SocketAsyncEventArgsProxy>(ServerConfig.DefaultMaxConnectionNumber);

                for (int i = 0; i < ServerConfig.DefaultMaxConnectionNumber; i++)
                {
                    //Pre-allocate a set of reusable SocketAsyncEventArgs
                    socketEventArg = new SocketAsyncEventArgs();
                    m_BufferManager.SetBuffer(socketEventArg);

                    socketArgsProxyList.Add(new SocketAsyncEventArgsProxy(socketEventArg));
                }

                m_ReadWritePool = new ConcurrentStack<SocketAsyncEventArgsProxy>(socketArgsProxyList);

                if (!base.Start())
                    return false;

                IsRunning = true;
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }
        protected override void OnNewClientAccepted(ISocketListener listener, Socket client, object state)
        {
            if (IsStopped)
                return;

            ProcessNewClient(client);
        }

        /// <summary>
        /// 连接后处理，创建client对应的session
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        private IAppSession ProcessNewClient(Socket client)
        {
            //Get the socket for the accepted client connection and put it into the 
            //ReadEventArg object user token
            SocketAsyncEventArgsProxy socketEventArgsProxy;
            if (!m_ReadWritePool.TryPop(out socketEventArgsProxy))
            {
                Async.AsyncRun(client.SafeClose);

                return null;
            }

            ISocketSession socketSession = new AsyncSocketSession(client, socketEventArgsProxy);

            var session = CreateSession(client, socketSession);

            if (session == null)
            {
                socketEventArgsProxy.Reset();
                this.m_ReadWritePool.Push(socketEventArgsProxy);
                Async.AsyncRun(client.SafeClose);
                return null;
            }

            socketSession.Closed += SessionClosed;

            var negotiateSession = socketSession as INegotiateSocketSession;

            if (negotiateSession == null)
            {
                if (RegisterSession(session))
                {
                    Async.AsyncRun(() => socketSession.Start());
                }

                return session;
            }

            negotiateSession.NegotiateCompleted += OnSocketSessionNegotiateCompleted;
            negotiateSession.Negotiate();

            return null;
        }
        void SessionClosed(ISocketSession session, CloseReason reason)
        {
            var socketSession = session as IAsyncSocketSessionBase;
            if (socketSession == null)
                return;

            var proxy = socketSession.SocketAsyncProxy;
            proxy.Reset();
            var args = proxy.SocketEventArgs;

            var serverState = AppServer.State;
            var pool = this.m_ReadWritePool;

            if (pool == null || serverState == ServerState.Stopping || serverState == ServerState.NotStarted)
            {
                if (!Environment.HasShutdownStarted && !AppDomain.CurrentDomain.IsFinalizingForUnload())
                    args.Dispose();
                return;
            }

            if (proxy.OrigOffset != args.Offset)
            {
                args.SetBuffer(proxy.OrigOffset, ServerConfig.DefaultReceiveBufferSize);
            }

            if (!proxy.IsRecyclable)
            {
                //cannot be recycled, so release the resource and don't return it to the pool
                args.Dispose();
                return;
            }

            pool.Push(proxy);
        }

        private void OnSocketSessionNegotiateCompleted(object sender, EventArgs e)
        {
            var socketSession = sender as ISocketSession;
            var negotiateSession = socketSession as INegotiateSocketSession;

            if (!negotiateSession.Result)
            {
                socketSession.Close(CloseReason.SocketError);
                return;
            }

            if (RegisterSession(negotiateSession.AppSession))
            {
                Async.AsyncRun(() => socketSession.Start());
            }
        }
        private bool RegisterSession(IAppSession appSession)
        {
            return true;
            //if (AppServer.RegisterSession(appSession))
            //    return true;

            //appSession.SocketSession.Close(CloseReason.InternalError);
            //return false;
        }


        public override void Stop()
        {
            if (IsStopped)
                return;

            lock (SyncRoot)
            {
                if (IsStopped)
                    return;

                base.Stop();

                foreach (var item in m_ReadWritePool)
                    item.SocketEventArgs.Dispose();

                m_ReadWritePool = null;
                m_BufferManager = null;
                IsRunning = false;
            }
        }

    }
}

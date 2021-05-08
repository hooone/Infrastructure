using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.SocketServer
{
    class AsyncTcpServer : TcpSocketServerBase
    {
        public AsyncTcpServer(IAppServer appServer, ListenerInfo[] listeners)
            : base(appServer, listeners)
        {

        }

        private ReceiveBuffer m_BufferManager;

        private ConcurrentStack<SocketAsyncEventArgsProxy> m_ReadWritePool;

        public override bool Start()
        {
            try
            {
                // 实例化buffer manager
                int bufferSize = ServerConfig.DefaultReceiveBufferSize;
                if (bufferSize <= 0)
                    bufferSize = 1024 * 4;
                m_BufferManager = new ReceiveBuffer(bufferSize * ServerConfig.DefaultMaxConnectionNumber, bufferSize);
                try
                {
                    m_BufferManager.InitBuffer();
                }
                catch (Exception e)
                {
                    AppServer.Logger.Error("Failed to allocate buffer for async socket communication, may because there is no enough memory, please decrease maxConnectionNumber in configuration!", e);
                    return false;
                }

                // 为每一个可能的socket预分配buffer池，并绑定到SocketAsyncEventArgs
                SocketAsyncEventArgs socketEventArg;
                var socketArgsProxyList = new List<SocketAsyncEventArgsProxy>(ServerConfig.DefaultMaxConnectionNumber);
                for (int i = 0; i < ServerConfig.DefaultMaxConnectionNumber; i++)
                {
                    socketEventArg = new SocketAsyncEventArgs();
                    m_BufferManager.SetBuffer(socketEventArg);
                    socketArgsProxyList.Add(new SocketAsyncEventArgsProxy(socketEventArg));
                }

                // 实例化socket堆栈
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
            // 取出proxy
            SocketAsyncEventArgsProxy socketEventArgsProxy;
            if (!m_ReadWritePool.TryPop(out socketEventArgsProxy))
            {
                Async.AsyncRun(client.SafeClose);
                return null;
            }

            // 实例化SocketSession
            ISocketSession socketSession = new AsyncSocketSession(client, socketEventArgsProxy);

            // 实例化AppSession
            var session = CreateSession(client, socketSession);
            if (session == null)
            {
                socketEventArgsProxy.Reset();
                this.m_ReadWritePool.Push(socketEventArgsProxy);
                Async.AsyncRun(client.SafeClose);
                return null;
            }
            socketSession.Closed += SessionClosed;

            // 此处移除了握手操作

            if (RegisterSession(session))
            {
                Async.AsyncRun(() => socketSession.Start());
            }
            return session;
        }
        void SessionClosed(ISocketSession session, CloseReason reason)
        {
            var socketSession = session as IAsyncSocketSession;
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

        private bool RegisterSession(IAppSession appSession)
        {
            if (AppServer.RegisterSession(appSession))
                return true;

            appSession.SocketSession.Close(CloseReason.InternalError);
            return false;
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

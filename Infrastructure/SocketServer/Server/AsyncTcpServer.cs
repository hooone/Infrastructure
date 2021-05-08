using Infrastructure.Log;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.SocketServer
{
    class AsyncTcpServer : ISocketServer, IDisposable
    {
        private readonly byte[] m_KeepAliveOptionValues;
        private readonly byte[] m_KeepAliveOptionOutValues;
        private readonly int m_SendTimeOut;
        private readonly int m_ReceiveBufferSize;
        private readonly int m_SendBufferSize;

        protected ListenerInfo[] ListenerInfos { get; private set; }

        protected AppServer AppServer { get; private set; }

        protected List<ISocketListener> Listeners { get; private set; }

        public AsyncTcpServer(AppServer app, ListenerInfo[] listeners)
        {
            AppServer = app;
            IsRunning = false;
            ListenerInfos = listeners;
            Listeners = new List<ISocketListener>(listeners.Length);

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

        public bool IsRunning { get; protected set; }

        protected bool IsStopped { get; set; }

        public ILog Logger { get; private set; } = new NopLogger();


        private ReceiveBuffer m_ReceiveBufferManager;

        private ConcurrentStack<SocketProxy> m_SocketPool;
        internal ISmartPool<SendingQueue> SendingQueuePool { get; private set; }
        public bool Start()
        {
            try
            {
                // 预热数据接收buffer
                int bufferSize = ServerConfig.DefaultReceiveBufferSize;
                if (bufferSize <= 0)
                    bufferSize = 1024 * 4;
                m_ReceiveBufferManager = new ReceiveBuffer(bufferSize * ServerConfig.DefaultMaxConnectionNumber, bufferSize);
                try
                {
                    m_ReceiveBufferManager.InitBuffer();
                }
                catch (Exception e)
                {
                    Logger.Error("Failed to allocate buffer for async socket communication, may because there is no enough memory, please decrease maxConnectionNumber in configuration!", e);
                    return false;
                }

                // 预热socket连接池
                SocketAsyncEventArgs socketEventArg;
                var socketArgsProxyList = new List<SocketProxy>(ServerConfig.DefaultMaxConnectionNumber);
                for (int i = 0; i < ServerConfig.DefaultMaxConnectionNumber; i++)
                {
                    socketEventArg = new SocketAsyncEventArgs();
                    m_ReceiveBufferManager.SetBuffer(socketEventArg);
                    socketArgsProxyList.Add(new SocketProxy(socketEventArg));
                }
                m_SocketPool = new ConcurrentStack<SocketProxy>(socketArgsProxyList);


                // 预热数据发送buffer
                var sendingQueuePool = new SmartPool<SendingQueue>();
                sendingQueuePool.Initialize(Math.Max(ServerConfig.DefaultMaxConnectionNumber / 6, 256),
                        Math.Max(ServerConfig.DefaultMaxConnectionNumber * 2, 256),
                        new SendingQueueSourceCreator(ServerConfig.DefaultSendingQueueSize));
                SendingQueuePool = sendingQueuePool;

                // 打开监听socket
                if (!InitListeners())
                    return false;

                IsRunning = true;
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        private bool InitListeners()
        {
            IsStopped = false;
            for (var i = 0; i < ListenerInfos.Length; i++)
            {
                var listener = new AsyncTcpListener(ListenerInfos[i]);
                listener.Error += new ErrorHandler(OnListenerError);
                listener.Stopped += new EventHandler(OnListenerStopped);
                listener.NewClientAccepted += new NewClientAcceptHandler(OnNewClientAccepted);

                if (listener.Start())
                {
                    Listeners.Add(listener);
                    Logger.Debug(string.Format("Listener ({0}) was started", listener.Info.EndPoint));
                }
                else //如果有一个启动失败，则全部listener都关闭
                {
                    Logger.Debug(string.Format("Listener ({0}) failed to start", listener.Info.EndPoint));
                    for (var j = 0; j < Listeners.Count; j++)
                    {
                        Listeners[j].Stop();
                    }

                    Listeners.Clear();
                    return false;
                }
            }

            IsRunning = true;
            return true;
        }

        protected void OnNewClientAccepted(ISocketListener listener, Socket client, object state)
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
            // 从Socket连接池中取出proxy
            SocketProxy socketEventArgsProxy;
            if (!m_SocketPool.TryPop(out socketEventArgsProxy))
            {
                Async.AsyncRun(client.SafeClose);
                return null;
            }

            // 设置socket参数
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

            // 实例化SocketSession
            ISocketSession socketSession = new AsyncSocketSession(client, socketEventArgsProxy, SendingQueuePool);

            // 实例化AppSession
            var session = new AppSession();
            session.WithLogger(this.Logger);
            session.Initialize(socketSession);
            if (session == null)
            {
                socketEventArgsProxy.Reset();
                this.m_SocketPool.Push(socketEventArgsProxy);
                Async.AsyncRun(client.SafeClose);
                return null;
            }
            socketSession.Closed += SessionClosed;

            if (RegisterSession(session))
            {
                Async.AsyncRun(() => socketSession.Start());
            }
            return session;
        }
        private bool RegisterSession(IAppSession appSession)
        {
            if (AppServer.RegisterSession(appSession))
                return true;

            return false;
        }


        void OnListenerError(ISocketListener listener, Exception e)
        {
            Logger.Error(string.Format("Listener ({0}) error: {1}", listener.Info.EndPoint, e.Message), e);
        }
        void OnListenerStopped(object sender, EventArgs e)
        {
            var listener = sender as ISocketListener;
            Logger.Debug(string.Format("Listener ({0}) was stoppped", listener.Info.EndPoint));
        }

        void SessionClosed(ISocketSession session, CloseReason reason)
        {
            var socketSession = session as AsyncSocketSession;
            if (socketSession == null)
                return;

            var proxy = socketSession.SocketAsyncProxy;
            proxy.Reset();
            var args = proxy.SocketEventArgs;

            var serverState = AppServer.State;
            var pool = this.m_SocketPool;

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

        public void Stop()
        {
            throw new NotImplementedException();
        }

        public void WithLogger(ILog logger)
        {
            Logger = logger;
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (IsRunning)
                    Stop();
            }
        }
    }
}

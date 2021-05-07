using Infrastructure.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.SocketServer
{
    public abstract class AppServerBase : IAppServer, IDisposable
    {
        /// <summary>
        /// Null appSession instance
        /// </summary>
        protected readonly AppSession NullAppSession = default(AppSession);

        //Server instance name
        private string m_Name;

        /// <summary>
        /// Gets the name of the server instance.
        /// </summary>
        public string Name
        {
            get { return m_Name; }
        }

        /// <summary>
        /// the current state's code
        /// </summary>
        private int m_StateCode = ServerStateConst.NotInitialized;

        /// <summary>
        /// Gets the current state of the work item.
        /// </summary>
        /// <value>
        /// The state.
        /// </value>
        public ServerState State
        {
            get
            {
                return (ServerState)m_StateCode;
            }
        }

        /// <summary>
        /// Gets the logger assosiated with this object.
        /// </summary>
        public ILog Logger { get; private set; } = new NopLogger();

        // 线程池初始化标志位
        private static bool m_ThreadPoolConfigured = false;

        private long m_TotalHandledRequests = 0;

        /// <summary>
        /// Gets the total handled requests number.
        /// </summary>
        protected long TotalHandledRequests
        {
            get { return m_TotalHandledRequests; }
        }

        private ListenerInfo[] m_Listeners;

        /// <summary>
        /// Gets or sets the listeners inforamtion.
        /// </summary>
        /// <value>
        /// The listeners.
        /// </value>
        public ListenerInfo[] Listeners
        {
            get { return m_Listeners; }
        }

        /// <summary>
        /// Gets the started time of this server instance.
        /// </summary>
        /// <value>
        /// The started time.
        /// </value>
        public DateTime StartedTime { get; private set; }

        public AppServerBase()
        {

        }
        public virtual bool Setup(int port, SocketMode mode)
        {
            // 确认AppServer状态
            TrySetInitializedState();

            // 线程池设置
            SetupThreadPool();

            // 监听端口设置
            if (!SetupListeners(port))
                return false;

            // SocketServer设置
            if (!SetupSocketServer(mode))
                return false;

            m_StateCode = ServerStateConst.NotStarted;
            return true;
        }

        private void TrySetInitializedState()
        {
            if (Interlocked.CompareExchange(ref m_StateCode, ServerStateConst.Initializing, ServerStateConst.NotInitialized)
                    != ServerStateConst.NotInitialized)
            {
                throw new Exception("The server has been initialized already, you cannot initialize it again!");
            }
        }

        private void SetupThreadPool()
        {
            if (!m_ThreadPoolConfigured)
            {
                int oldMinWorkingThreads, oldMinCompletionPortThreads;
                int oldMaxWorkingThreads, oldMaxCompletionPortThreads;

                ThreadPool.GetMinThreads(out oldMinWorkingThreads, out oldMinCompletionPortThreads);
                ThreadPool.GetMaxThreads(out oldMaxWorkingThreads, out oldMaxCompletionPortThreads);

                ThreadPool.SetMinThreads(oldMinWorkingThreads, oldMinCompletionPortThreads);
                ThreadPool.SetMaxThreads(oldMaxWorkingThreads, oldMaxCompletionPortThreads);

                m_ThreadPoolConfigured = true;
            }
        }

        /// <summary>
        /// Setups the listeners base on server configuration
        /// </summary>
        /// <param name="config">The config.</param>
        /// <returns></returns>
        private bool SetupListeners(int port)
        {
            var listeners = new List<ListenerInfo>();
            try
            {
                if (port > 0)
                {
                    listeners.Add(new ListenerInfo
                    {
                        EndPoint = new IPEndPoint(IPAddress.Any, port),
                    });
                }
                if (!listeners.Any())
                {
                    Logger.Error("No listener defined!");
                    return false;
                }
                m_Listeners = listeners.ToArray();
                return true;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return false;
            }
        }

        private ISocketServer m_SocketServer;
        /// <summary>
        /// Gets the socket server.
        /// </summary>
        /// <value>
        /// The socket server.
        /// </value>
        public ISocketServer SocketServer
        {
            get { return m_SocketServer; }
        }


        /// <summary>
        /// Setups the socket server.instance
        /// </summary>
        /// <returns></returns>
        private bool SetupSocketServer(SocketMode mode)
        {
            try
            {
                switch (mode)
                {
                    case (SocketMode.Tcp):
                        m_SocketServer = new AsyncTcpServer(this, Listeners);
                        break;
                    //case (SocketMode.Udp):
                    //    return new UdpSocketServer<TRequestInfo>(appServer, listeners);
                    default:
                        throw new NotSupportedException("Unsupported SocketMode:" + mode);
                }
                return m_SocketServer != null;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return false;
            }
        }


        /// <summary>
        /// Starts this server instance.
        /// </summary>
        /// <returns>
        /// return true if start successfull, else false
        /// </returns>
        public virtual bool Start()
        {
            // 启动状态确认，避免重复启动
            var origStateCode = Interlocked.CompareExchange(ref m_StateCode, ServerStateConst.Starting, ServerStateConst.NotStarted);
            if (origStateCode != ServerStateConst.NotStarted)
            {
                if (origStateCode < ServerStateConst.NotStarted)
                    throw new Exception("You cannot start a server instance which has not been setup yet.");
                Logger.Error(string.Format("This server instance is in the state {0}, you cannot start it now.", (ServerState)origStateCode));
                return false;
            }

            // 调用SocketServer.Start()
            if (!m_SocketServer.Start())
            {
                m_StateCode = ServerStateConst.NotStarted;
                return false;
            }
            StartedTime = DateTime.Now;
            m_StateCode = ServerStateConst.Running;
            return true;
        }

        /// <summary>
        /// Creates the app session.
        /// </summary>
        /// <param name="socketSession">The socket session.</param>
        /// <returns></returns>
        public IAppSession CreateAppSession(ISocketSession socketSession)
        {
            var appSession = new AppSession();
            appSession.Initialize(this, socketSession);
            return appSession;
        }

        /// <summary>
        /// Gets the app session by ID.
        /// </summary>
        /// <param name="sessionID">The session ID.</param>
        /// <returns></returns>
        public abstract AppSession GetSessionByID(string sessionID);

        /// <summary>
        /// Registers the new created app session into the appserver's session container.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <returns></returns>
        bool IAppServer.RegisterSession(IAppSession session)
        {
            var appSession = session as AppSession;

            if (!RegisterSession(appSession.SessionID, appSession))
                return false;

            appSession.SocketSession.Closed += OnSocketSessionClosed;

            Logger.Info("A new session connected!");

            OnNewSessionConnected(appSession);
            return true;
        }

        /// <summary>
        /// Registers the session into session container.
        /// </summary>
        /// <param name="sessionID">The session ID.</param>
        /// <param name="appSession">The app session.</param>
        /// <returns></returns>
        protected virtual bool RegisterSession(string sessionID, AppSession appSession)
        {
            return true;
        }

        /// <summary>
        /// Called when [socket session closed].
        /// </summary>
        /// <param name="session">The socket session.</param>
        /// <param name="reason">The reason.</param>
        private void OnSocketSessionClosed(ISocketSession session, CloseReason reason)
        {
            Logger.Info(string.Format("This session was closed for {0}!", reason));

            var appSession = session.AppSession as AppSession;
            appSession.Connected = false;
            OnSessionClosed(appSession, reason);
        }
        /// <summary>
        /// Called when [session closed].
        /// </summary>
        /// <param name="session">The appSession.</param>
        /// <param name="reason">The reason.</param>
        protected virtual void OnSessionClosed(AppSession session, CloseReason reason)
        {
            var handler = m_SessionClosed;
            if (handler != null)
            {
                handler.BeginInvoke(session, reason, OnSessionClosedCallback, handler);
            }
        }

        private void OnSessionClosedCallback(IAsyncResult result)
        {
            try
            {
                var handler = (SessionHandler< CloseReason>)result.AsyncState;
                handler.EndInvoke(result);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        private SessionHandler m_NewSessionConnected;
        /// <summary>
        /// The action which will be executed after a new session connect
        /// </summary>
        public event SessionHandler NewSessionConnected
        {
            add { m_NewSessionConnected += value; }
            remove { m_NewSessionConnected -= value; }
        }

        private SessionHandler<CloseReason> m_SessionClosed;
        /// <summary>
        /// Gets/sets the session closed event handler.
        /// </summary>
        public event SessionHandler<CloseReason> SessionClosed
        {
            add { m_SessionClosed += value; }
            remove { m_SessionClosed -= value; }
        }

        /// <summary>
        /// Called when [new session connected].
        /// </summary>
        /// <param name="session">The session.</param>
        protected virtual void OnNewSessionConnected(AppSession session)
        {
            var handler = m_NewSessionConnected;
            if (handler == null)
                return;

            handler.BeginInvoke(session, OnNewSessionConnectedCallback, handler);
        }
        private void OnNewSessionConnectedCallback(IAsyncResult result)
        {
            try
            {
                var handler = (SessionHandler)result.AsyncState;
                handler.EndInvoke(result);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        /// <summary>
        /// Stops this server instance.
        /// </summary>
        public virtual void Stop()
        {
            if (Interlocked.CompareExchange(ref m_StateCode, ServerStateConst.Stopping, ServerStateConst.Running)
                    != ServerStateConst.Running)
            {
                return;
            }

            m_SocketServer.Stop();

            m_StateCode = ServerStateConst.NotStarted;

            Logger.Info(string.Format("The server instance {0} has been stopped!", Name));
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        public void Dispose()
        {
            if (m_StateCode == ServerStateConst.Running)
                Stop();
        }

    }
}

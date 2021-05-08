using Infrastructure.Log;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.SocketServer
{
    public delegate void RequestHandler(AppSession session, byte[] requestInfo);
    public class AppServer : IAppServer, IDisposable
    {

        private int m_StateCode = ServerStateConst.NotInitialized;
        public ServerState State
        {
            get
            {
                return (ServerState)m_StateCode;
            }
        }


        private RequestHandler m_RequestHandler;

        public virtual event RequestHandler NewRequestReceived
        {
            add { m_RequestHandler += value; }
            remove { m_RequestHandler -= value; }
        }


        public DateTime StartedTime { get; private set; }

        private ListenerInfo[] m_Listeners;
        public ListenerInfo[] Listeners
        {
            get { return m_Listeners; }
        }

        public ILog Logger { get; private set; } = new NopLogger();

        private ISocketServer m_SocketServer;

        public bool Setup(int port)
        {
            // 确认AppServer状态
            TrySetInitializedState();

            // 线程池设置
            SetupThreadPool();

            // 监听端口设置
            if (!SetupListeners(port))
                return false;

            // SocketServer设置
            if (!SetupSocketServer(SocketMode.Tcp))
                return false;


            m_StateCode = ServerStateConst.NotStarted;
            return true;
        }

        public bool Start()
        {
            if (!socketListen())
            {
                return false;
            }

            // 定时Session快照
            StartSessionSnapshotTimer();

            // 定时清理Session
            StartClearSessionTimer();

            return true;
        }
        private ConcurrentDictionary<string, AppSession> m_SessionDict = new ConcurrentDictionary<string, AppSession>(StringComparer.OrdinalIgnoreCase);

        internal bool RegisterSession(IAppSession session)
        {
            var appSession = session as AppSession;

            if (!m_SessionDict.TryAdd(appSession.SessionID, appSession))
                return false;

            appSession.SocketSession.Closed += OnSocketSessionClosed;

            Logger.Info("A new session connected!");

            OnNewSessionConnected(appSession);
            return true;
        }

        internal void ExecuteCommand(AppSession session, byte[] requestInfo)
        {
            if (m_RequestHandler != null)
            {
                try
                {
                    m_RequestHandler(session, requestInfo);
                }
                catch (Exception e)
                {
                    session.HandleException(e);
                }
            }
            session.LastActiveTime = DateTime.Now;
            Logger.Info(string.Format("Receive - {0}", ToHexStrFromByte(requestInfo)));
        }
        public static string ToHexStrFromByte(byte[] byteDatas)
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < byteDatas.Length; i++)
            {
                builder.Append(string.Format("{0:X2} ", byteDatas[i]));
            }
            return builder.ToString().Trim();
        }

        private void OnSocketSessionClosed(ISocketSession session, CloseReason reason)
        {
            Logger.Info(string.Format("This session was closed for {0}!", reason));
            if (m_SessionDict.TryGetValue(session.SessionID, out AppSession appSession))
            {
                appSession.Connected = false;
                OnSessionClosed(appSession, reason);
            }
        }

        public IAppSession GetSessionByID(string sessionID)
        {
            throw new NotImplementedException();
        }

        public void Stop()
        {
            if (Interlocked.CompareExchange(ref m_StateCode, ServerStateConst.Stopping, ServerStateConst.Running)
                    != ServerStateConst.Running)
            {
                return;
            }

            m_SocketServer.Stop();

            m_StateCode = ServerStateConst.NotStarted;

            Logger.Info(string.Format("The server instance has been stopped!"));
            if (m_SessionSnapshotTimer != null)
            {
                m_SessionSnapshotTimer.Change(Timeout.Infinite, Timeout.Infinite);
                m_SessionSnapshotTimer.Dispose();
                m_SessionSnapshotTimer = null;
            }

            if (m_ClearIdleSessionTimer != null)
            {
                m_ClearIdleSessionTimer.Change(Timeout.Infinite, Timeout.Infinite);
                m_ClearIdleSessionTimer.Dispose();
                m_ClearIdleSessionTimer = null;
            }

            m_SessionsSnapshot = null;

            var sessions = m_SessionDict.ToArray();

            if (sessions.Length > 0)
            {
                var tasks = new Task[sessions.Length];

                for (var i = 0; i < tasks.Length; i++)
                {
                    tasks[i] = Task.Factory.StartNew((s) =>
                    {
                        var session = s as AppSession;

                        if (session != null)
                        {
                            session.Close(CloseReason.ServerShutdown);
                        }

                    }, sessions[i].Value);
                }

                Task.WaitAll(tasks);
            }
        }
        #region setup

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
        // 线程池初始化标志位
        private static bool m_ThreadPoolConfigured = false;
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

        private void TrySetInitializedState()
        {
            if (Interlocked.CompareExchange(ref m_StateCode, ServerStateConst.Initializing, ServerStateConst.NotInitialized)
                    != ServerStateConst.NotInitialized)
            {
                throw new Exception("The server has been initialized already, you cannot initialize it again!");
            }
        }

        private bool SetupSocketServer(SocketMode mode)
        {
            try
            {
                switch (mode)
                {
                    case (SocketMode.Tcp):
                        m_SocketServer = new AsyncTcpServer(this, Listeners);
                        m_SocketServer.WithLogger(this.Logger);
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

        #endregion
        #region start
        private bool socketListen()
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

        #endregion
        #region event
        public delegate void SessionHandler(AppSession session);
        public delegate void SessionHandler<TParam>(AppSession session, TParam value);

        private SessionHandler m_NewSessionConnected;
        public event SessionHandler NewSessionConnected
        {
            add { m_NewSessionConnected += value; }
            remove { m_NewSessionConnected -= value; }
        }
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

        private SessionHandler<CloseReason> m_SessionClosed;
        public event SessionHandler<CloseReason> SessionClosed
        {
            add { m_SessionClosed += value; }
            remove { m_SessionClosed -= value; }
        }
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
                var handler = (SessionHandler<CloseReason>)result.AsyncState;
                handler.EndInvoke(result);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }
        #endregion
        public void WithLogger(ILog logger)
        {
            this.Logger = logger.SubLogger("SocketServer");
        }
        public void Dispose()
        {
            if (m_StateCode == ServerStateConst.Running)
                Stop();
        }


        #region Take session snapshot

        private System.Threading.Timer m_SessionSnapshotTimer = null;

        private KeyValuePair<string, AppSession>[] m_SessionsSnapshot = new KeyValuePair<string, AppSession>[0];

        private void StartSessionSnapshotTimer()
        {
            int interval = Math.Max(ServerConfig.DefaultClearIdleSessionInterval, 1) * 1000;//in milliseconds
            m_SessionSnapshotTimer = new System.Threading.Timer(TakeSessionSnapshot, new object(), interval, interval);
        }

        private void TakeSessionSnapshot(object state)
        {
            if (Monitor.TryEnter(state))
            {
                Interlocked.Exchange(ref m_SessionsSnapshot, m_SessionDict.ToArray());
                Monitor.Exit(state);
            }
        }

        #endregion

        #region Clear idle sessions

        private System.Threading.Timer m_ClearIdleSessionTimer = null;

        private void StartClearSessionTimer()
        {
            int interval = ServerConfig.DefaultClearIdleSessionInterval * 1000;//in milliseconds
            m_ClearIdleSessionTimer = new System.Threading.Timer(ClearIdleSession, new object(), interval, interval);
        }

        /// <summary>
        /// Clears the idle session.
        /// </summary>
        /// <param name="state">The state.</param>
        private void ClearIdleSession(object state)
        {
            if (Monitor.TryEnter(state))
            {
                try
                {
                    var sessionSource = SessionSource;

                    if (sessionSource == null)
                        return;

                    DateTime now = DateTime.Now;
                    DateTime timeOut = now.AddSeconds(0 - ServerConfig.DefaultIdleSessionTimeOut);

                    var timeOutSessions = sessionSource.Where(s => s.Value.LastActiveTime <= timeOut).Select(s => s.Value);

                    System.Threading.Tasks.Parallel.ForEach(timeOutSessions, s =>
                    {
                        Logger.Info(string.Format("The session will be closed for {0} timeout, the session start time: {1}, last active time: {2}!", now.Subtract(s.LastActiveTime).TotalSeconds, s.StartTime, s.LastActiveTime));
                        s.Close(CloseReason.TimeOut);
                    });
                }
                catch (Exception e)
                {
                    Logger.Error("Clear idle session error!", e);
                }
                finally
                {
                    Monitor.Exit(state);
                }
            }
        }

        private KeyValuePair<string, AppSession>[] SessionSource
        {
            get
            {
                return m_SessionsSnapshot;
            }
        }

        #endregion
        #region Search session utils

        /// <summary>
        /// Gets the matched sessions from sessions snapshot.
        /// </summary>
        /// <param name="critera">The prediction critera.</param>
        /// <returns></returns>
        public IEnumerable<AppSession> GetSessions(Func<AppSession, bool> critera)
        {
            var sessionSource = SessionSource;

            if (sessionSource == null)
                return null;

            return sessionSource.Select(p => p.Value).Where(critera);
        }

        /// <summary>
        /// Gets all sessions in sessions snapshot.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<AppSession> GetAllSessions()
        {
            var sessionSource = SessionSource;

            if (sessionSource == null)
                return null;

            return sessionSource.Select(p => p.Value);
        }
        #endregion
    }
}

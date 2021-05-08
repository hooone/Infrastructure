using Infrastructure.Log;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.SocketServer.Server
{
    public class AppServer : IAppServer
    {

        private int m_StateCode = ServerStateConst.NotInitialized;
        public ServerState State
        {
            get
            {
                return (ServerState)m_StateCode;
            }
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

            //// 定时Session快照
            //StartSessionSnapshotTimer();

            //// 定时清理Session
            //StartClearSessionTimer();

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

        public bool Stop()
        {
            throw new NotImplementedException();
        }
        #region setup
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
    }
}

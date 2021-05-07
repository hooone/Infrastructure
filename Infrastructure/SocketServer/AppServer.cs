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
    public class AppServer : AppServerBase
    {
        /// <summary>
        /// Starts this AppServer instance.
        /// </summary>
        /// <returns></returns>
        public override bool Start()
        {
            if (!base.Start())
                return false;

            // 定时Session快照
            StartSessionSnapshotTimer();

            // 定时清理Session
            StartClearSessionTimer();

            return true;
        }

        private ConcurrentDictionary<string, AppSession> m_SessionDict = new ConcurrentDictionary<string, AppSession>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Registers the session into the session container.
        /// </summary>
        /// <param name="sessionID">The session ID.</param>
        /// <param name="appSession">The app session.</param>
        /// <returns></returns>
        protected override bool RegisterSession(string sessionID, AppSession appSession)
        {
            if (m_SessionDict.TryAdd(sessionID, appSession))
                return true;

            Logger.Error("The session is refused because the it's ID already exists!");

            return false;
        }
        /// <summary>
        /// Gets the app session by ID.
        /// </summary>
        /// <param name="sessionID">The session ID.</param>
        /// <returns></returns>
        public override AppSession GetSessionByID(string sessionID)
        {
            if (string.IsNullOrEmpty(sessionID))
                return NullAppSession;

            AppSession targetSession;
            m_SessionDict.TryGetValue(sessionID, out targetSession);
            return targetSession;
        }



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
                        Logger.Info( string.Format("The session will be closed for {0} timeout, the session start time: {1}, last active time: {2}!", now.Subtract(s.LastActiveTime).TotalSeconds, s.StartTime, s.LastActiveTime));
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

        #region Take session snapshot

        private System.Threading.Timer m_SessionSnapshotTimer = null;

        private KeyValuePair<string, AppSession>[] m_SessionsSnapshot = new KeyValuePair<string, AppSession>[0];

        private void StartSessionSnapshotTimer()
        {
            int interval = Math.Max(ServerConfig.DefaultSessionSnapshotInterval, 1) * 1000;//in milliseconds
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

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.SocketServer
{
    public class SocketServer
    {
        /// <summary>
        /// Gets the default text encoding.
        /// </summary>
        /// <value>
        /// The text encoding.
        /// </value>
        public Encoding TextEncoding { get; private set; }

        /// <summary>
        /// 当前的server状态
        /// </summary>
        private int m_StateCode = ServerStateConst.NotInitialized;

        /// <summary>
        /// 线程池状态
        /// </summary>
        private static bool m_ThreadPoolConfigured = false;

        private ISocketServer m_SocketServer;
        private ListenerInfo[] m_Listeners;


        public bool Setup(int port)
        {
            TrySetInitializedState();

            SetupBasic();

            //if (!SetupLogFactory(logFactory))
            //    return false;

            //Logger = CreateLogger(this.Name);

            //if (!SetupMedium(receiveFilterFactory, connectionFilters, commandLoaders))
            //    return false;

            //if (!SetupAdvanced(config))
            //    return false;

            //if (!Setup(rootConfig, config))
            //    return false;

            //if (!SetupFinal())
            //    return false;

            if (!SetupListeners(port))
                return false;

            if (!SetupSocketServer())
                return false;

            m_StateCode = ServerStateConst.NotStarted;
            return true;
        }

        /// <summary>
        /// Setups the socket server.instance
        /// </summary>
        /// <returns></returns>
        private bool SetupSocketServer()
        {
            try
            {
                m_SocketServer = new AsyncSocketServer( m_Listeners);
                return m_SocketServer != null;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        /// <summary>
        /// Server状态切换到Initializing
        /// </summary>
        private void TrySetInitializedState()
        {
            if (Interlocked.CompareExchange(ref m_StateCode, ServerStateConst.Initializing, ServerStateConst.NotInitialized)
                    != ServerStateConst.NotInitialized)
            {
                throw new Exception("The server has been initialized already, you cannot initialize it again!");
            }
        }
        private void SetupBasic()
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

            TextEncoding = new ASCIIEncoding();
        }

        /// <summary>
        /// Setups the listeners base on server configuration
        /// </summary>
        /// <param name="config">The config.</param>
        /// <returns></returns>
        private bool SetupListeners(int port)
        {
            if (port <= 0)
                return false;

            var listeners = new List<ListenerInfo>();

            try
            {
                listeners.Add(new ListenerInfo
                {
                    EndPoint = new IPEndPoint(IPAddress.Any, port),
                });
                m_Listeners = listeners.ToArray();

                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

    }
}

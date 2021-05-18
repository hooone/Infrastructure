using Infrastructure.Log;
using Infrastructure.SocketClient;
using Infrastructure.SocketClient.Filter;
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
    public delegate void RequestHandler(ClientSession session, byte[] requestInfo);
    public class EasyClient : IClient, ILoggerProvider
    {
        private IClientSession m_Session;
        private TaskCompletionSource<bool> m_ConnectTaskSource;
        private TaskCompletionSource<bool> m_CloseTaskSource;

        public event EventHandler<byte[]> NewPackageReceived;

        private EndPoint m_RemoteEndPoint;
        private EndPoint m_EndPointToBind;
        private EndPoint m_LocalEndPoint;


        private RequestHandler m_RequestHandler;

        public virtual event RequestHandler NewRequestReceived
        {
            add { m_RequestHandler += value; }
            remove { m_RequestHandler -= value; }
        }


        public EndPoint LocalEndPoint
        {
            get
            {
                if (m_LocalEndPoint != null)
                    return m_LocalEndPoint;

                return m_EndPointToBind;
            }
            set
            {
                m_EndPointToBind = value;
            }
        }
        public Socket Socket
        {
            get
            {
                var session = m_Session;

                if (session == null)
                    return null;

                return session.Socket;
            }
        }


        private bool m_Connected = false;
        public bool IsConnected { get { return m_Connected; } }

        public int ReceiveBufferSize { get; set; }

        public ILog Logger { get; private set; } = new NopLogger();

        public async Task<bool> ConnectAsync(string ip, int port)
        {
            EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            return await ConnectAsync(remoteEndPoint);
        }
        public async Task<bool> ConnectAsync(EndPoint remoteEndPoint)
        {
            var connectTaskSrc = InitConnect(remoteEndPoint);
            return await connectTaskSrc.Task.ConfigureAwait(false);
        }
        private TaskCompletionSource<bool> InitConnect(EndPoint remoteEndPoint)
        {
            m_RemoteEndPoint = remoteEndPoint;

            var session = GetUnderlyingSession();
            session.NoDelay = true;

            //if (Proxy != null)
            //    session.Proxy = Proxy;

            session.Connected += new EventHandler(OnSessionConnected);
            session.Error += new EventHandler<ErrorEventArgs>(OnSessionError);
            session.Closed += new EventHandler(OnSessionClosed);
            session.DataReceived += new EventHandler<byte[]>(OnSessionDataReceived);

            if (ReceiveBufferSize > 0)
                session.ReceiveBufferSize = ReceiveBufferSize;

            m_Session = session;

            var taskSrc = m_ConnectTaskSource = new TaskCompletionSource<bool>();

            session.Connect(remoteEndPoint);

            return taskSrc;
        }
        private TcpClientSession GetUnderlyingSession()
        {
            return new AsyncTcpSession();
        }
        void OnSessionDataReceived(object sender, byte[] e)
        {
            if (m_RequestHandler != null)
            {
                try
                {
                    m_RequestHandler((ClientSession)sender, e);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
            }
            Logger.Info(string.Format("Receive - {0}", ToHexStrFromByte(e)));
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


        public bool Send(byte[] data)
        {
            return Send(new ArraySegment<byte>(data, 0, data.Length));
        }

        public bool Send(ArraySegment<byte> segment)
        {
            var session = m_Session;

            if (!m_Connected || session == null)
            {
                if (m_RemoteEndPoint == null)
                {
                    return false;
                }
                var r = ConnectAsync(m_RemoteEndPoint);
                if (!r.Result)
                {
                    return false;
                }
            }

            return session.Send(segment);
        }
        public async Task<bool> Close()
        {
            var session = m_Session;

            if (session != null && m_Connected)
            {
                var closeTaskSrc = new TaskCompletionSource<bool>();
                m_CloseTaskSource = closeTaskSrc;
                session.Close();
                return await closeTaskSrc.Task.ConfigureAwait(false);
            }

            return await Task.FromResult(false);
        }
        #region 事件上抛
        public event EventHandler Connected;
        void OnSessionConnected(object sender, EventArgs e)
        {
            m_Connected = true;

            TcpClientSession session = sender as TcpClientSession;
            if (session != null)
            {
                m_LocalEndPoint = session.LocalEndPoint;
            }

            FinishConnectTask(true);

            var handler = Connected;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        bool FinishConnectTask(bool result)
        {
            var connectTaskSource = m_ConnectTaskSource;

            if (connectTaskSource == null)
                return false;

            if (Interlocked.CompareExchange(ref m_ConnectTaskSource, null, connectTaskSource) == connectTaskSource)
            {
                connectTaskSource.SetResult(result);
                return true;
            }

            return false;
        }

        public event EventHandler<ErrorEventArgs> Error;
        void OnSessionError(object sender, ErrorEventArgs e)
        {
            if (!m_Connected)
            {
                FinishConnectTask(false);
            }

            OnError(e);
        }

        private void OnError(Exception e)
        {
            OnError(new ErrorEventArgs(e));
        }

        private void OnError(ErrorEventArgs args)
        {
            var handler = Error;

            if (handler != null)
                handler(this, args);
        }

        public event EventHandler Closed;
        void OnSessionClosed(object sender, EventArgs e)
        {
            m_Connected = false;

            m_LocalEndPoint = null;

            var handler = Closed;

            if (handler != null)
                handler(this, EventArgs.Empty);


            var closeTaskSrc = m_CloseTaskSource;

            if (closeTaskSrc != null)
            {
                if (Interlocked.CompareExchange(ref m_CloseTaskSource, null, closeTaskSrc) == closeTaskSrc)
                {
                    closeTaskSrc.SetResult(true);
                }
            }
        }

        public void WithLogger(ILog logger)
        {
            this.Logger = logger;
        }


        #endregion
    }
}

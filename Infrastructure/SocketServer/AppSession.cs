using Infrastructure.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.SocketServer
{
    public class AppSession : IAppSession
    {
        /// <summary>
        /// Gets the app server instance assosiated with the session.
        /// </summary>
        public IAppServer AppServer { get; set; }

        /// <summary>
        /// Gets or sets the last active time of the session.
        /// </summary>
        /// <value>
        /// The last active time.
        /// </value>
        public DateTime LastActiveTime { get; set; }

        /// <summary>
        /// Gets the start time of the session.
        /// </summary>
        public DateTime StartTime { get; private set; }

        /// <summary>
        /// Gets the local listening endpoint.
        /// </summary>
        public IPEndPoint LocalEndPoint
        {
            get { return SocketSession.LocalEndPoint; }
        }

        /// <summary>
        /// Gets the remote endpoint of client.
        /// </summary>
        public IPEndPoint RemoteEndPoint
        {
            get { return SocketSession.RemoteEndPoint; }
        }

        /// <summary>
        /// Gets the logger.
        /// </summary>
        public ILog Logger
        {
            get { return AppServer.Logger; }
        }

        /// <summary>
        /// Gets the session ID.
        /// </summary>
        public string SessionID { get; private set; }

        private bool m_Connected = false;
        /// <summary>
        /// Gets a value indicating whether this <see cref="IAppSession"/> is connected.
        /// </summary>
        /// <value>
        ///   <c>true</c> if connected; otherwise, <c>false</c>.
        /// </value>
        public bool Connected
        {
            get { return m_Connected; }
            internal set { m_Connected = value; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AppSession&lt;TAppSession, TRequestInfo&gt;"/> class.
        /// </summary>
        public AppSession()
        {
            this.StartTime = DateTime.Now;
            this.LastActiveTime = this.StartTime;
        }


        /// <summary>
        /// Closes this session.
        /// </summary>
        public virtual void Close()
        {
            Close(CloseReason.ServerClosing);
        }

        /// <summary>
        /// Closes the session by the specified reason.
        /// </summary>
        /// <param name="reason">The close reason.</param>
        public virtual void Close(CloseReason reason)
        {
            this.SocketSession.Close(reason);
        }

        /// <summary>
        /// Starts the session.
        /// </summary>
        public void StartSession()
        {
        }

        /// <summary>
        /// Gets the socket session of the AppSession.
        /// </summary>

        public ISocketSession SocketSession { get; private set; }
        /// <summary>
        /// Initializes the specified app session by AppServer and SocketSession.
        /// </summary>
        /// <param name="appServer">The app server.</param>
        /// <param name="socketSession">The socket session.</param>
        public virtual void Initialize(IAppServer appServer, ISocketSession socketSession)
        {
            var castedAppServer = appServer;
            AppServer = castedAppServer;
            SocketSession = socketSession;
            SessionID = socketSession.SessionID;
            m_Connected = true;
            m_ReceiveFilter = new BeginEndMarkReceiveFilter(new byte[] { }, new byte[] { (byte)'\r', (byte)'\n' });
            socketSession.Initialize(this);

        }


        /// <summary>
        /// Processes the request data.
        /// </summary>
        /// <param name="readBuffer">The read buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="length">The length.</param>
        /// <param name="toBeCopied">if set to <c>true</c> [to be copied].</param>
        /// <returns>
        /// return offset delta of next receiving buffer
        /// </returns>
        public int ProcessRequest(byte[] readBuffer, int offset, int length, bool toBeCopied)
        {
            int rest, offsetDelta;

            while (true)
            {
                var requestInfo = FilterRequest(readBuffer, offset, length, toBeCopied, out rest, out offsetDelta);

                if (requestInfo != null)
                {
                    try
                    {
                        AppServer.ExecuteCommand(this, requestInfo);
                    }
                    catch (Exception e)
                    {
                        HandleException(e);
                    }
                }

                if (rest <= 0)
                {
                    return offsetDelta;
                }

                //Still have data has not been processed
                offset = offset + length - rest;
                length = rest;
            }
        }

        IReceiveFilter m_ReceiveFilter;

        /// <summary>
        /// Filters the request.
        /// </summary>
        /// <param name="readBuffer">The read buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="length">The length.</param>
        /// <param name="toBeCopied">if set to <c>true</c> [to be copied].</param>
        /// <param name="rest">The rest, the size of the data which has not been processed</param>
        /// <param name="offsetDelta">return offset delta of next receiving buffer.</param>
        /// <returns></returns>
        byte[] FilterRequest(byte[] readBuffer, int offset, int length, bool toBeCopied, out int rest, out int offsetDelta)
        {
            var currentRequestLength = m_ReceiveFilter.LeftBufferSize;

            var requestInfo = m_ReceiveFilter.Filter(readBuffer, offset, length, toBeCopied, out rest);

            if (m_ReceiveFilter.State == FilterState.Error)
            {
                rest = 0;
                offsetDelta = 0;
                Close(CloseReason.ProtocolError);
                return null;
            }

            var offsetAdapter = m_ReceiveFilter as IOffsetAdapter;

            offsetDelta = offsetAdapter != null ? offsetAdapter.OffsetDelta : 0;

            if (requestInfo == null)
            {
                //current buffered length
                currentRequestLength = m_ReceiveFilter.LeftBufferSize;
            }
            else
            {
                //current request length
                currentRequestLength = currentRequestLength + length - rest;
            }

            var maxRequestLength = GetMaxRequestLength();

            if (currentRequestLength >= maxRequestLength)
            {
                Logger.Error(string.Format("Max request length: {0}, current processed length: {1}", maxRequestLength, currentRequestLength));

                Close(CloseReason.ProtocolError);
                return null;
            }

            //If next Receive filter wasn't set, still use current Receive filter in next round received data processing
            if (m_ReceiveFilter.NextReceiveFilter != null)
                m_ReceiveFilter = m_ReceiveFilter.NextReceiveFilter;

            return requestInfo;
        }

        /// <summary>
        /// Gets the maximum allowed length of the request.
        /// </summary>
        /// <returns></returns>
        protected virtual int GetMaxRequestLength()
        {
            return ServerConfig.DefaultMaxRequestLength;
        }



        internal void InternalHandleExcetion(Exception e)
        {
            HandleException(e);
        }
        /// <summary>
        /// Handles the exceptional error, it only handles application error.
        /// </summary>
        /// <param name="e">The exception.</param>
        protected virtual void HandleException(Exception e)
        {
            Logger.Error(e);
            this.Close(CloseReason.ApplicationError);
        }
    }
}

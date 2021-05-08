using Infrastructure.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.SocketServer
{
    public class AppSession : IAppSession, ILoggerProvider
    {
        public ILog Logger { get; private set; } = new NopLogger();
        public string SessionID { get; private set; }

        public DateTime LastActiveTime { get; set; }

        public DateTime StartTime { get; private set; }

        private bool m_Connected = false;
        public bool Connected
        {
            get { return m_Connected; }
            internal set { m_Connected = value; }
        }

        internal AppSession()
        {
            this.StartTime = DateTime.Now;
            this.LastActiveTime = this.StartTime;
        }
        internal ISocketSession SocketSession { get; set; }
        IReceiveFilter m_ReceiveFilter;

        internal void Initialize(ISocketSession socketSession)
        {
            SocketSession = socketSession;
            SessionID = socketSession.SessionID;
            m_Connected = true;
            m_ReceiveFilter = new TerminatorReceiveFilter(new byte[] { (byte)'\r', (byte)'\n' });
            socketSession.Initialize(this);
        }
        public int ProcessRequest(byte[] readBuffer, int offset, int length, bool toBeCopied)
        {
            int rest, offsetDelta;

            while (true)
            {
                var requestInfo = FilterRequest(readBuffer, offset, length, toBeCopied, out rest, out offsetDelta);
                if (requestInfo != null)
                {
                    // 上抛数据
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

            var maxRequestLength = ServerConfig.DefaultMaxRequestLength;

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
        public void WithLogger(ILog logger)
        {
            this.Logger = logger;
        }
    }
}

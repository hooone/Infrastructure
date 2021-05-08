using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.SocketServer.Server
{
    public class AppSession : IAppSession
    {
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
            m_ReceiveFilter = new BeginEndMarkReceiveFilter(new byte[] { }, new byte[] { (byte)'\r', (byte)'\n' });
            socketSession.Initialize(this);
        }
    }
}

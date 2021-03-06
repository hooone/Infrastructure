using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.SocketServer
{
    class ServerConfig
    {
        /// <summary>
        /// Default ReceiveBufferSize
        /// </summary>
        public const int DefaultReceiveBufferSize = 4096;
        /// <summary>
        /// Default MaxConnectionNumber
        /// </summary>
        public const int DefaultMaxConnectionNumber = 100;
        /// <summary>
        /// Default sending queue size
        /// </summary>
        public const int DefaultSendingQueueSize = 5;
        /// <summary>
        /// The default listen backlog
        /// </summary>
        public const int DefaultListenBacklog = 100;
        /// <summary>
        /// The default keep alive time
        /// </summary>
        public const int DefaultKeepAliveTime = 600; // 60 * 10 = 10 minutes
        /// <summary>
        /// The default keep alive interval
        /// </summary>
        public const int DefaultKeepAliveInterval = 60; // 60 seconds
        /// <summary>
        /// Default send timeout value, in milliseconds
        /// </summary>
        public const int DefaultSendTimeout = 5000;
        /// <summary>
        /// The default send buffer size
        /// </summary>
        public const int DefaultSendBufferSize = 2048;
        /// <summary>
        /// Default clear idle session interval(second)
        /// </summary>
        public const int DefaultClearIdleSessionInterval = 3;
        /// <summary>
        /// Default idle session timeout
        /// </summary>
        public const int DefaultIdleSessionTimeOut = 300;
        /// <summary>
        /// The default session snapshot interval
        /// </summary>
        public const int DefaultSessionSnapshotInterval = 5;
        /// <summary>
        /// Default MaxRequestLength
        /// </summary>
        public const int DefaultMaxRequestLength = 4096;
    }
}

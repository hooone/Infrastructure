using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.SocketServer
{
    /// <summary>
    /// The basic session interface
    /// </summary>
    public interface ISessionBase
    {
        /// <summary>
        /// Gets the session ID.
        /// </summary>
        string SessionID { get; }

        /// <summary>
        /// Gets the remote endpoint.
        /// </summary>
        IPEndPoint RemoteEndPoint { get; }
    }
}

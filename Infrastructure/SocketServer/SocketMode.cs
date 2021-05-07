using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.SocketServer
{
    /// <summary>
    /// Socket server running mode
    /// </summary>
    public enum SocketMode
    {
        /// <summary>
        /// Tcp mode
        /// </summary>
        Tcp,

        /// <summary>
        /// Udp mode
        /// </summary>
        Udp
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.SocketServer
{
    public class ListenerInfo
    {
        /// <summary>
        /// Gets or sets the listen endpoint.
        /// </summary>
        /// <value>
        /// The end point.
        /// </value>
        public IPEndPoint EndPoint { get; set; }
    }
}

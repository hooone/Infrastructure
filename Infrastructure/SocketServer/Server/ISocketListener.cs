using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.SocketServer
{
    delegate void ErrorHandler(ISocketListener listener, Exception e);

    delegate void NewClientAcceptHandler(ISocketListener listener, Socket client, object state);

    /// <summary>
    /// The interface for socket listener
    /// </summary>
    interface ISocketListener
    {
        /// <summary>
        /// Gets the info of listener
        /// </summary>
        ListenerInfo Info { get; }

        /// <summary>
        /// Starts to listen
        /// </summary>
        /// <param name="config">The server config.</param>
        /// <returns></returns>
        bool Start();

        /// <summary>
        /// Stops listening
        /// </summary>
        void Stop();

        /// <summary>
        /// Occurs when new client accepted.
        /// </summary>
        event NewClientAcceptHandler NewClientAccepted;

        /// <summary>
        /// Occurs when error got.
        /// </summary>
        event ErrorHandler Error;


        /// <summary>
        /// Occurs when [stopped].
        /// </summary>
        event EventHandler Stopped;
    }
}

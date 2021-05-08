using Infrastructure.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.SocketServer
{
    /// <summary>
    /// The interface for AppServer
    /// </summary>
    public interface IAppServer
    {
        /// <summary>
        /// Gets the current state of the work item.
        /// </summary>
        /// <value>
        /// The state.
        /// </value>
        ServerState State { get; }
        /// <summary>
        /// Gets the started time.
        /// </summary>
        /// <value>
        /// The started time.
        /// </value>
        DateTime StartedTime { get; }


        /// <summary>
        /// Gets or sets the listeners.
        /// </summary>
        /// <value>
        /// The listeners.
        /// </value>
        ListenerInfo[] Listeners { get; }

        /// <summary>
        /// Gets the Receive filter factory.
        /// </summary>
        //object ReceiveFilterFactory { get; }

        /// <summary>
        /// Creates the app session.
        /// </summary>
        /// <param name="socketSession">The socket session.</param>
        /// <returns></returns>
        IAppSession CreateAppSession(ISocketSession socketSession);


        /// <summary>
        /// Registers the new created app session into the appserver's session container.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <returns></returns>
        bool RegisterSession(IAppSession session);

        /// <summary>
        /// Gets the app session by ID.
        /// </summary>
        /// <param name="sessionID">The session ID.</param>
        /// <returns></returns>
        //IAppSession GetSessionByID(string sessionID);

        /// <summary>
        /// Gets the logger assosiated with this object.
        /// </summary>
        ILog Logger { get; }

        /// <summary>
        /// Gets the socket server.
        /// </summary>
        /// <value>
        /// The socket server.
        /// </value>
        ISocketServer SocketServer { get; }

        void ExecuteCommand(AppSession session, byte[] requestInfo);
    }
}

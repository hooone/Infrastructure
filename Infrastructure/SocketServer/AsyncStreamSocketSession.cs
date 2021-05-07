using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.SocketServer
{
    /// <summary>
    /// The interface for socket session which requires negotiation before communication
    /// </summary>
    interface INegotiateSocketSession
    {
        /// <summary>
        /// Start negotiates
        /// </summary>
        void Negotiate();

        /// <summary>
        /// Gets a value indicating whether this <see cref="INegotiateSocketSession" /> is result.
        /// </summary>
        /// <value>
        ///   <c>true</c> if result; otherwise, <c>false</c>.
        /// </value>
        bool Result { get; }


        /// <summary>
        /// Gets the app session.
        /// </summary>
        /// <value>
        /// The app session.
        /// </value>
        IAppSession AppSession { get; }

        /// <summary>
        /// Occurs when [negotiate completed].
        /// </summary>
        event EventHandler NegotiateCompleted;
    }
}

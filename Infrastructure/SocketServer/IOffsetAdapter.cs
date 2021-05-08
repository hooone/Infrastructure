using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.SocketServer
{
    /// <summary>
    /// The interface for a Receive filter to adapt receiving buffer offset
    /// </summary>
    public interface IOffsetAdapter
    {
        /// <summary>
        /// Gets the offset delta.
        /// </summary>
        int OffsetDelta { get; }
    }
}

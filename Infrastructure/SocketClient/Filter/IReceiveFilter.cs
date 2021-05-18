﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.SocketClient.Filter
{
    /// <summary>
    /// Receive filter interface
    /// </summary>
    /// <typeparam name="TRequestInfo">The type of the request info.</typeparam>
    public interface IReceiveFilter
    {
        /// <summary>
        /// Filters received data of the specific session into request info.
        /// </summary>
        /// <param name="readBuffer">The read buffer.</param>
        /// <param name="offset">The offset of the current received data in this read buffer.</param>
        /// <param name="length">The length of the current received data.</param>
        /// <param name="toBeCopied">if set to <c>true</c> [to be copied].</param>
        /// <param name="rest">The rest, the length of the data which hasn't been parsed.</param>
        /// <returns></returns>
        byte[] Filter(byte[] readBuffer, int offset, int length, bool toBeCopied, out int rest);

        /// <summary>
        /// Gets the size of the rest buffer.
        /// </summary>
        /// <value>
        /// The size of the rest buffer.
        /// </value>
        int LeftBufferSize { get; }

        /// <summary>
        /// Gets the next Receive filter.
        /// </summary>
        IReceiveFilter NextReceiveFilter { get; }

        /// <summary>
        /// Resets this instance to initial state.
        /// </summary>
        void Reset();


        /// <summary>
        /// Gets the filter state.
        /// </summary>
        /// <value>
        /// The filter state.
        /// </value>
        FilterState State { get; }
    }
}

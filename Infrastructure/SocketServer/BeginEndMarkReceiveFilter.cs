using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.SocketServer
{
    public class BeginEndMarkReceiveFilter : ReceiveFilterBase
    {
        private readonly SearchMarkState<byte> m_BeginSearchState;
        private readonly SearchMarkState<byte> m_EndSearchState;

        private bool m_FoundBegin = false;

        /// <summary>
        /// Null request info
        /// </summary>
        protected byte[] NullRequestInfo = default(byte[]);

        /// <summary>
        /// Initializes a new instance of the <see cref="BeginEndMarkReceiveFilter&lt;TRequestInfo&gt;"/> class.
        /// </summary>
        /// <param name="beginMark">The begin mark.</param>
        /// <param name="endMark">The end mark.</param>
        public BeginEndMarkReceiveFilter(byte[] beginMark, byte[] endMark)
        {
            m_BeginSearchState = new SearchMarkState<byte>(beginMark);
            m_EndSearchState = new SearchMarkState<byte>(endMark);
        }

        /// <summary>
        /// Filters the specified session.
        /// </summary>
        /// <param name="readBuffer">The read buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="length">The length.</param>
        /// <param name="toBeCopied">if set to <c>true</c> [to be copied].</param>
        /// <param name="rest">The rest.</param>
        /// <returns></returns>
        public override byte[] Filter(byte[] readBuffer, int offset, int length, bool toBeCopied, out int rest)
        {
            rest = 0;

            int searchEndMarkOffset = offset;
            int searchEndMarkLength = length;

            //prev macthed begin mark length
            int prevMatched = 0;
            int totalParsed = 0;

            while (true)
            {
                var prevEndMarkMatched = m_EndSearchState.Matched;
                var parsedLen = 0;
                var endPos = readBuffer.SearchMark(searchEndMarkOffset, searchEndMarkLength, m_EndSearchState, out parsedLen);

                //Haven't found end mark
                if (endPos < 0)
                {
                    rest = 0;
                    if (prevMatched > 0)//Also cache the prev matched begin mark
                        AddArraySegment(m_BeginSearchState.Mark, 0, prevMatched, false);
                    AddArraySegment(readBuffer, offset, length, toBeCopied);
                    return NullRequestInfo;
                }

                totalParsed += parsedLen;
                rest = length - totalParsed;

                byte[] commandData = new byte[BufferSegments.Count + prevMatched + totalParsed];

                if (BufferSegments.Count > 0)
                    BufferSegments.CopyTo(commandData, 0, 0, BufferSegments.Count);

                if (prevMatched > 0)
                    Array.Copy(m_BeginSearchState.Mark, 0, commandData, BufferSegments.Count, prevMatched);

                Array.Copy(readBuffer, offset, commandData, BufferSegments.Count + prevMatched, totalParsed);

                var requestInfo = ProcessMatchedRequest(commandData, 0, commandData.Length);

                if (!ReferenceEquals(requestInfo, NullRequestInfo))
                {
                    Reset();
                    return requestInfo;
                }

                if (rest > 0)
                {
                    searchEndMarkOffset = endPos + m_EndSearchState.Mark.Length;
                    searchEndMarkLength = rest;
                    continue;
                }

                //Not match
                if (prevMatched > 0)//Also cache the prev matched begin mark
                    AddArraySegment(m_BeginSearchState.Mark, 0, prevMatched, false);
                AddArraySegment(readBuffer, offset, length, toBeCopied);
                return NullRequestInfo;
            }
        }

        /// <summary>
        /// Processes the matched request.
        /// </summary>
        /// <param name="readBuffer">The read buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="length">The length.</param>
        /// <returns></returns>
        protected byte[] ProcessMatchedRequest(byte[] readBuffer, int offset, int length)
        {
            return readBuffer;
            if (length < 20)
            {
                Console.WriteLine("Ignore request");
                return NullRequestInfo;
            }

            byte[] rst = new byte[length];
            Array.Copy(readBuffer, offset, rst, 0, length);
            return rst;
        }

        /// <summary>
        /// Resets this instance.
        /// </summary>
        public override void Reset()
        {
            m_BeginSearchState.Matched = 0;
            m_EndSearchState.Matched = 0;
            m_FoundBegin = false;
            base.Reset();
        }
    }
}

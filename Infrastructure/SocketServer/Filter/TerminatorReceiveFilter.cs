using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.SocketServer.Server
{
    public class TerminatorReceiveFilter : ReceiveFilterBase, IOffsetAdapter
    {
        private readonly SearchMarkState<byte> m_SearchState;
        internal TerminatorReceiveFilter(byte[] terminator)
        {
            m_SearchState = new SearchMarkState<byte>(terminator);
        }
        protected static readonly byte[] NullRequestInfo = default(byte[]);

        private int m_ParsedLengthInBuffer = 0;
        private int m_OffsetDelta;

        int IOffsetAdapter.OffsetDelta
        {
            get { return m_OffsetDelta; }
        }
        public override byte[] Filter(byte[] readBuffer, int offset, int length, bool toBeCopied, out int rest)
        {
            rest = 0;

            int prevMatched = m_SearchState.Matched;

            int result = readBuffer.SearchMark(offset, length, m_SearchState);

            if (result < 0)
            {
                if (m_OffsetDelta != m_ParsedLengthInBuffer)
                {
                    Buffer.BlockCopy(readBuffer, offset - m_ParsedLengthInBuffer, readBuffer, offset - m_OffsetDelta, m_ParsedLengthInBuffer + length);

                    m_ParsedLengthInBuffer += length;
                    m_OffsetDelta = m_ParsedLengthInBuffer;
                }
                else
                {
                    m_ParsedLengthInBuffer += length;

                    if (m_ParsedLengthInBuffer >= ServerConfig.DefaultReceiveBufferSize)
                    {
                        this.AddArraySegment(readBuffer, offset + length - m_ParsedLengthInBuffer, m_ParsedLengthInBuffer, toBeCopied);
                        m_ParsedLengthInBuffer = 0;
                        m_OffsetDelta = 0;

                        return NullRequestInfo;
                    }

                    m_OffsetDelta += length;
                }

                return NullRequestInfo;
            }

            var findLen = result - offset;
            var currentMatched = m_SearchState.Mark.Length - prevMatched;

            //The prev matched part is not belong to the current matched terminator mark
            if (prevMatched > 0 && findLen != 0)
            {
                //rest prevMatched to 0
                prevMatched = 0;
                currentMatched = m_SearchState.Mark.Length;
            }

            rest = length - findLen - currentMatched;

            byte[] requestInfo;

            if (findLen > 0)
            {
                if (this.BufferSegments != null && this.BufferSegments.Count > 0)
                {
                    this.AddArraySegment(readBuffer, offset - m_ParsedLengthInBuffer, findLen + m_ParsedLengthInBuffer, toBeCopied);
                    requestInfo = ProcessMatchedRequest(BufferSegments, 0, BufferSegments.Count);
                }
                else
                {
                    requestInfo = ProcessMatchedRequest(readBuffer, offset - m_ParsedLengthInBuffer, findLen + m_ParsedLengthInBuffer);
                }
            }
            else if (prevMatched > 0)
            {
                if (m_ParsedLengthInBuffer > 0)
                {
                    if (m_ParsedLengthInBuffer < prevMatched)
                    {
                        BufferSegments.TrimEnd(prevMatched - m_ParsedLengthInBuffer);
                        requestInfo = ProcessMatchedRequest(BufferSegments, 0, BufferSegments.Count);
                    }
                    else
                    {
                        if (this.BufferSegments != null && this.BufferSegments.Count > 0)
                        {
                            this.AddArraySegment(readBuffer, offset - m_ParsedLengthInBuffer, m_ParsedLengthInBuffer - prevMatched, toBeCopied);
                            requestInfo = ProcessMatchedRequest(BufferSegments, 0, BufferSegments.Count);
                        }
                        else
                        {
                            requestInfo = ProcessMatchedRequest(readBuffer, offset - m_ParsedLengthInBuffer, m_ParsedLengthInBuffer - prevMatched);
                        }
                    }
                }
                else
                {
                    BufferSegments.TrimEnd(prevMatched);
                    requestInfo = ProcessMatchedRequest(BufferSegments, 0, BufferSegments.Count);
                }
            }
            else
            {
                if (this.BufferSegments != null && this.BufferSegments.Count > 0)
                {
                    if (m_ParsedLengthInBuffer > 0)
                    {
                        this.BufferSegments.AddSegment(readBuffer, offset, m_ParsedLengthInBuffer);
                    }

                    requestInfo = ProcessMatchedRequest(BufferSegments, 0, BufferSegments.Count);
                }
                else
                {
                    requestInfo = ProcessMatchedRequest(readBuffer, offset - m_ParsedLengthInBuffer, m_ParsedLengthInBuffer);
                }
            }

            InternalReset();

            if (rest == 0)
            {
                m_OffsetDelta = 0;
            }
            else
            {
                m_OffsetDelta += (length - rest);
            }

            return requestInfo;
        }

        private void InternalReset()
        {
            m_ParsedLengthInBuffer = 0;
            m_SearchState.Matched = 0;
            base.Reset();
        }
        private byte[] ProcessMatchedRequest(ArraySegmentList data, int offset, int length)
        {
            var targetData = data.ToArrayData(offset, length);
            return targetData;
        }
        protected byte[] ProcessMatchedRequest(byte[] data, int offset, int length)
        {
            if (data.Length == length && offset == 0)
            {
                return data;
            }
            byte[] rst = new byte[length];
            Array.Copy(data, offset, rst, 0, length);
            return rst;
        }
    }
}

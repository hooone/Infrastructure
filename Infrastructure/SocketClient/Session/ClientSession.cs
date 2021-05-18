using Infrastructure.Log;
using Infrastructure.SocketClient.Filter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.SocketClient
{
    public abstract class ClientSession : IClientSession, ILoggerProvider
    {
        public const int DefaultMaxRequestLength = 4096;
        public const int DefaultReceiveBufferSize = 4096;
        public virtual int ReceiveBufferSize { get; set; }


        protected Socket Client { get; set; }
        Socket IClientSession.Socket
        {
            get { return Client; }
        }

        public virtual EndPoint LocalEndPoint { get; set; }

        public bool IsConnected { get; private set; }
        public bool NoDelay { get; set; }
        public int SendingQueueSize { get; set; }

        protected ArraySegment<byte> Buffer { get; set; }

        public ILog Logger { get; private set; } = new NopLogger();

        protected virtual void SetBuffer(ArraySegment<byte> bufferSegment)
        {
            Buffer = bufferSegment;
        }


        private EventHandler m_Closed;

        public event EventHandler Closed
        {
            add { m_Closed += value; }
            remove { m_Closed -= value; }
        }

        public abstract void Close();

        protected virtual void OnClosed()
        {
            IsConnected = false;
            LocalEndPoint = null;

            var handler = m_Closed;

            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        private EventHandler<ErrorEventArgs> m_Error;

        public event EventHandler<ErrorEventArgs> Error
        {
            add { m_Error += value; }
            remove { m_Error -= value; }
        }

        protected virtual void OnError(Exception e)
        {
            var handler = m_Error;
            if (handler == null)
                return;

            handler(this, new ErrorEventArgs(e));
        }

        private EventHandler m_Connected;

        public event EventHandler Connected
        {
            add { m_Connected += value; }
            remove { m_Connected -= value; }
        }

        protected virtual void OnConnected()
        {
            var client = Client;

            if (client != null)
            {
                try
                {
                    if (client.NoDelay != NoDelay)
                        client.NoDelay = NoDelay;
                }
                catch
                {
                }
            }

            IsConnected = true;

            var handler = m_Connected;
            if (handler == null)
                return;

            handler(this, EventArgs.Empty);
        }

        private EventHandler<byte[]> m_DataReceived;

        public event EventHandler<byte[]> DataReceived
        {
            add { m_DataReceived += value; }
            remove { m_DataReceived -= value; }
        }
        protected virtual void HandDataReceived(byte[] data)
        {
            var handler = m_DataReceived;
            if (handler == null)
                return;

            handler(this, data);
        }


        IReceiveFilter m_ReceiveFilter = new TerminatorReceiveFilter(new byte[] { (byte)'\r', (byte)'\n' });
        byte[] FilterRequest(byte[] readBuffer, int offset, int length, bool toBeCopied, out int rest, out int offsetDelta)
        {
            var currentRequestLength = m_ReceiveFilter.LeftBufferSize;

            var requestInfo = m_ReceiveFilter.Filter(readBuffer, offset, length, toBeCopied, out rest);

            if (m_ReceiveFilter.State == FilterState.Error)
            {
                rest = 0;
                offsetDelta = 0;
                return null;
            }

            var offsetAdapter = m_ReceiveFilter as IOffsetAdapter;

            offsetDelta = offsetAdapter != null ? offsetAdapter.OffsetDelta : 0;

            if (requestInfo == null)
            {
                //current buffered length
                currentRequestLength = m_ReceiveFilter.LeftBufferSize;
            }
            else
            {
                //current request length
                currentRequestLength = currentRequestLength + length - rest;
            }

            var maxRequestLength = DefaultMaxRequestLength;

            if (currentRequestLength >= maxRequestLength)
            {
                Logger.Error(string.Format("Max request length: {0}, current processed length: {1}", maxRequestLength, currentRequestLength));

                return null;
            }

            //If next Receive filter wasn't set, still use current Receive filter in next round received data processing
            if (m_ReceiveFilter.NextReceiveFilter != null)
                m_ReceiveFilter = m_ReceiveFilter.NextReceiveFilter;

            return requestInfo;
        }
        protected virtual int OnDataReceived(byte[] data, int offset, int length)
        {
            int rest, offsetDelta;

            while (true)
            {
                var requestInfo = FilterRequest(data, offset, length, true, out rest, out offsetDelta);
                if (requestInfo != null)
                {
                    try
                    {
                        HandDataReceived(requestInfo);
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e);
                    }
                }

                if (rest <= 0)
                {
                    return offsetDelta;
                }

                //Still have data has not been processed
                offset = offset + length - rest;
                length = rest;
            }
        }

        public abstract void Connect(EndPoint remoteEndPoint);
        public abstract bool TrySend(ArraySegment<byte> segment);
        protected abstract bool DetectConnected();

        public bool Send(ArraySegment<byte> segment)
        {
            if (!DetectConnected())
            {
                return false;
            }
            if (TrySend(segment))
                return true;

            var spinWait = new SpinWait();

            while (true)
            {
                spinWait.SpinOnce();

                if (TrySend(segment))
                    return true;
            }
        }

        public void WithLogger(ILog logger)
        {
            this.Logger = logger;
        }
    }
}

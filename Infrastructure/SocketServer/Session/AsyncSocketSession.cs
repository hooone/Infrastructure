using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.SocketServer.Server
{
    internal class AsyncSocketSession : IAsyncSocketSession, ISocketSession
    {
        public string SessionID { get; private set; }

        public IPEndPoint LocalEndPoint { get; protected set; }

        public IPEndPoint RemoteEndPoint { get; protected set; }

        internal SocketProxy SocketAsyncProxy { get; set; }

        private Socket m_Client;
        internal Socket Client
        {
            get { return m_Client; }
        }

        private bool m_Connected = false;
        public bool Connected
        {
            get { return m_Connected; }
            internal set { m_Connected = value; }
        }

        private AppSession AppSession { get; set; }

        public Action<ISocketSession, CloseReason> Closed { get; set; }

        private bool m_IsReset;
        private ISmartPool<SendingQueue> m_SendingQueuePool;
        private SendingQueue m_SendingQueue;

        public AsyncSocketSession(Socket client, SocketProxy socketAsyncProxy, ISmartPool<SendingQueue> sendingPool)
        {
            if (client == null)
                throw new ArgumentNullException("client");

            SessionID = Guid.NewGuid().ToString();
            m_Client = client;
            LocalEndPoint = (IPEndPoint)client.LocalEndPoint;
            RemoteEndPoint = (IPEndPoint)client.RemoteEndPoint;

            m_SendingQueuePool = sendingPool;
            SocketAsyncProxy = socketAsyncProxy;
            m_IsReset = false;
        }

        public void Initialize(IAppSession appSession)
        {
            this.AppSession = appSession as AppSession;

            SendingQueue queue;
            if (m_SendingQueuePool.TryGet(out queue))
            {
                m_SendingQueue = queue;
                queue.StartEnqueue();
            }

            SocketAsyncProxy.Initialize(this);
        }
        public void Start()
        {
            StartReceive(SocketAsyncProxy.SocketEventArgs);
        }
        private void StartReceive(SocketAsyncEventArgs e)
        {
            StartReceive(e, 0);
        }

        public void ProcessReceive(SocketAsyncEventArgs e)
        {
            // 更新Session活动时间
            this.AppSession.LastActiveTime = DateTime.Now;

            // 确认该事件是消息接收事件，否正挥手断开
            if (!ProcessCompleted(e))
            {
                OnReceiveTerminated(e.SocketError == SocketError.Success ? CloseReason.ClientClosing : CloseReason.SocketError);
                return;
            }

            // 切换到读取状态
            OnReceiveEnded();

            int offsetDelta = 0;

            try
            {
                offsetDelta = this.AppSession.ProcessRequest(e.Buffer, e.Offset, e.BytesTransferred, true);
            }
            catch (Exception exc)
            {
                this.Close(CloseReason.ProtocolError);
                return;
            }

            //订阅接收下一个信息
            StartReceive(e, offsetDelta);
        }

        /// <summary>
        /// 监听消息
        /// </summary>
        private void StartReceive(SocketAsyncEventArgs e, int offsetDelta)
        {
            bool willRaiseEvent = false;

            try
            {
                if (offsetDelta < 0 || offsetDelta >= ServerConfig.DefaultReceiveBufferSize)
                    throw new ArgumentException(string.Format("Illigal offsetDelta: {0}", offsetDelta), "offsetDelta");

                var predictOffset = SocketAsyncProxy.OrigOffset + offsetDelta;

                if (e.Offset != predictOffset)
                {
                    e.SetBuffer(predictOffset, ServerConfig.DefaultReceiveBufferSize - offsetDelta);
                }

                // 检查连接状态
                if (!OnReceiveStarted())
                    return;

                willRaiseEvent = Client.ReceiveAsync(e);
            }
            catch (Exception exc)
            {
                OnReceiveTerminated(CloseReason.SocketError);
                return;
            }

            if (!willRaiseEvent)
            {
                ProcessReceive(e);
            }
        }

        private void OnClosed(CloseReason reason)
        {
            // 停止发送
            //var sae = m_SocketEventArgSend;
            //if (sae != null)
            //{
            //    if (Interlocked.CompareExchange(ref m_SocketEventArgSend, null, sae) == sae)
            //    {
            //        sae.Dispose();
            //    }
            //}
            // 状态位切换和释放资源
            if (!TryAddStateFlag(SocketState.Closed))
                return;
            while (true)
            {
                var sendingQueue = m_SendingQueue;

                if (sendingQueue == null)
                    break;

                //There is no sending was started after the m_Closed ws set to 'true'
                if (Interlocked.CompareExchange(ref m_SendingQueue, null, sendingQueue) == sendingQueue)
                {
                    sendingQueue.Clear();
                    m_SendingQueuePool.Push(sendingQueue);
                    break;
                }
            }

            var closedHandler = this.Closed;
            if (closedHandler != null)
            {
                closedHandler(this, reason);
            }
        }

        public virtual void Close(CloseReason reason)
        {
            //Already in closing procedure
            if (!TryAddStateFlag(SocketState.InClosing))
                return;

            Socket client;

            //No need to clean the socket instance
            if (TryValidateClosedBySocket(out client))
                return;

            //Some data is in sending
            if (CheckState(SocketState.InSending))
            {
                //Set closing reason only, don't close the socket directly
                AddStateFlag(GetCloseReasonValue(reason));
                return;
            }

            // In the udp mode, we needn't close the socket instance
            if (client != null)
                InternalClose(client, reason, true);
            else //In Udp mode, and the socket is not in the sending state, then fire the closed event directly
                OnClosed(reason);
        }
        #region 连接状态检查
        //0x00 0x00 0x00 0x00
        //1st byte: Closed(Y/N) - 0x01
        //2nd byte: N/A
        //3th byte: CloseReason
        //Last byte: 0000 0000 - normal state
        //0000 0001: in sending
        //0000 0010: in receiving
        //0001 0000: in closing
        private int m_State = 0;


        protected bool IsInClosingOrClosed
        {
            get { return m_State >= SocketState.InClosing; }
        }

        protected bool IsClosed
        {
            get { return m_State >= SocketState.Closed; }
        }
        protected bool OnReceiveStarted()
        {
            if (AddStateFlag(SocketState.InReceiving, true))
                return true;

            // the connection is in closing
            ValidateClosed(CloseReason.Unknown, false);
            return false;
        }
        private bool AddStateFlag(int stateValue, bool notClosing)
        {
            while (true)
            {
                var oldState = m_State;

                if (notClosing)
                {
                    // don't update the state if the connection has entered the closing procedure
                    if (oldState >= SocketState.InClosing)
                    {
                        return false;
                    }
                }

                var newState = m_State | stateValue;

                if (Interlocked.CompareExchange(ref m_State, newState, oldState) == oldState)
                    return true;
            }
        }
        protected void OnReceiveTerminated(CloseReason closeReason)
        {
            OnReceiveEnded();
            ValidateClosed(closeReason, true);
        }
        protected void OnReceiveEnded()
        {
            RemoveStateFlag(SocketState.InReceiving);
        }
        private void RemoveStateFlag(int stateValue)
        {
            while (true)
            {
                var oldState = m_State;
                var newState = m_State & (~stateValue);

                if (Interlocked.CompareExchange(ref m_State, newState, oldState) == oldState)
                    return;
            }
        }
        bool ProcessCompleted(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                if (e.BytesTransferred > 0)
                {
                    return true;
                }
            }
            return false;
        }
        private void ValidateClosed(CloseReason closeReason, bool forceClose)
        {
            ValidateClosed(closeReason, forceClose, false);
        }

        private void ValidateClosed(CloseReason closeReason, bool forceClose, bool forSend)
        {
            lock (this)
            {
                if (IsClosed)
                    return;

                if (CheckState(SocketState.InClosing))
                {
                    // we only keep socket instance after InClosing state when the it is sending
                    // so we check if the socket instance is alive now
                    if (forSend)
                    {
                        Socket client;

                        if (!TryValidateClosedBySocket(out client))
                        {
                            var sendingQueue = m_SendingQueue;
                            // No data to be sent
                            if (forceClose || (sendingQueue != null && sendingQueue.Count == 0))
                            {
                                if (client != null)// the socket instance is not closed yet, do it now
                                    InternalClose(client, GetCloseReasonFromState(), false);
                                else// The UDP mode, the socket instance always is null, fire the closed event directly
                                    FireCloseEvent();

                                return;
                            }

                            return;
                        }
                    }

                    if (ValidateNotInSendingReceiving())
                    {
                        FireCloseEvent();
                    }
                }
                else if (forceClose)
                {
                    Close(closeReason);
                }
            }
        }
        private void InternalClose(Socket client, CloseReason reason, bool setCloseReason)
        {
            if (Interlocked.CompareExchange(ref m_Client, null, client) == client)
            {
                if (setCloseReason)
                    AddStateFlag(GetCloseReasonValue(reason));

                client.SafeClose();

                if (ValidateNotInSendingReceiving())
                {
                    OnClosed(reason);
                }
            }
        }
        private bool ValidateNotInSendingReceiving()
        {
            var oldState = m_State;

            if ((oldState & SocketState.InSendingReceivingMask) == oldState)
            {
                return true;
            }

            return false;
        }


        private const int m_CloseReasonMagic = 256;
        private int GetCloseReasonValue(CloseReason reason)
        {
            return ((int)reason + 1) * m_CloseReasonMagic;
        }
        protected virtual bool TryValidateClosedBySocket(out Socket socket)
        {
            socket = m_Client;
            //Already closed/closing
            return socket == null;
        }

        private bool CheckState(int stateValue)
        {
            return (m_State & stateValue) == stateValue;
        }

        private CloseReason GetCloseReasonFromState()
        {
            return (CloseReason)(m_State / m_CloseReasonMagic - 1);
        }

        private void FireCloseEvent()
        {
            OnClosed(GetCloseReasonFromState());
        }
        private void AddStateFlag(int stateValue)
        {
            AddStateFlag(stateValue, false);
        }

        private bool TryAddStateFlag(int stateValue)
        {
            while (true)
            {
                var oldState = m_State;
                var newState = m_State | stateValue;

                //Already marked
                if (oldState == newState)
                {
                    return false;
                }

                var compareState = Interlocked.CompareExchange(ref m_State, newState, oldState);

                if (compareState == oldState)
                    return true;
            }
        }

        #endregion
    }
}

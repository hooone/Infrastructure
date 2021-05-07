using Infrastructure.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.SocketServer
{
    abstract class SocketServerBase : ISocketServer, IDisposable
    {
        protected object SyncRoot = new object();

        public IAppServer AppServer { get; private set; }

        public bool IsRunning { get; protected set; }

        protected bool IsStopped { get; set; }

        protected ListenerInfo[] ListenerInfos { get; private set; }

        protected List<ISocketListener> Listeners { get; private set; }

        protected abstract ISocketListener CreateListener(ListenerInfo listenerInfo);

        public SocketServerBase(IAppServer appServer, ListenerInfo[] listeners)
        {
            AppServer = appServer;
            IsRunning = false;
            ListenerInfos = listeners;
            Listeners = new List<ISocketListener>(listeners.Length);
        }

        /// <summary>
        /// Gets the sending queue manager.
        /// </summary>
        /// <value>
        /// The sending queue manager.
        /// </value>
        internal ISmartPool<SendingQueue> SendingQueuePool { get; private set; }
        public virtual bool Start()
        {
            IsStopped = false;

            ILog log = AppServer.Logger;

            // 实例化线程池
            var sendingQueuePool = new SmartPool<SendingQueue>();
            sendingQueuePool.Initialize(Math.Max(ServerConfig.DefaultMaxConnectionNumber / 6, 256),
                    Math.Max(ServerConfig.DefaultMaxConnectionNumber * 2, 256),
                    new SendingQueueSourceCreator(ServerConfig.DefaultSendingQueueSize));
            SendingQueuePool = sendingQueuePool;

            for (var i = 0; i < ListenerInfos.Length; i++)
            {
                var listener = CreateListener(ListenerInfos[i]);
                listener.Error += new ErrorHandler(OnListenerError);
                listener.Stopped += new EventHandler(OnListenerStopped);
                listener.NewClientAccepted += new NewClientAcceptHandler(OnNewClientAccepted);

                if (listener.Start())
                {
                    Listeners.Add(listener);
                    log.Debug(string.Format("Listener ({0}) was started", listener.EndPoint));
                }
                else //如果有一个启动失败，则全部listener都关闭
                {
                    log.Debug(string.Format("Listener ({0}) failed to start", listener.EndPoint));
                    for (var j = 0; j < Listeners.Count; j++)
                    {
                        Listeners[j].Stop();
                    }

                    Listeners.Clear();
                    return false;
                }
            }

            IsRunning = true;
            return true;
        }
        protected abstract void OnNewClientAccepted(ISocketListener listener, Socket client, object state);

        void OnListenerError(ISocketListener listener, Exception e)
        {
            var logger = this.AppServer.Logger;
            logger.Error(string.Format("Listener ({0}) error: {1}", listener.EndPoint, e.Message), e);
        }

        void OnListenerStopped(object sender, EventArgs e)
        {
            var listener = sender as ISocketListener;
            ILog log = AppServer.Logger;
            log.Debug(string.Format("Listener ({0}) was stoppped", listener.EndPoint));
        }
        public virtual void Stop()
        {
            IsStopped = true;

            for (var i = 0; i < Listeners.Count; i++)
            {
                var listener = Listeners[i];

                listener.Stop();
            }

            Listeners.Clear();

            SendingQueuePool = null;

            IsRunning = false;
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (IsRunning)
                    Stop();
            }
        }

    }
}

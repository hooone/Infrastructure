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

        public SocketServerBase(ListenerInfo[] listeners)
        {
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
                }
                else //If one listener failed to start, stop started listeners
                {
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
        }

        void OnListenerStopped(object sender, EventArgs e)
        {
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

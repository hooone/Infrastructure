using Infrastructure.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.SocketServer.Server
{
    public interface IAppServer : ILoggerProvider
    {
        /// <summary>
        /// Server状态
        /// </summary>
        ServerState State { get; }

        /// <summary>
        /// 启动事件
        /// </summary>
        DateTime StartedTime { get; }

        /// <summary>
        /// 监听端口的基本属性
        /// </summary>
        ListenerInfo[] Listeners { get; }

        /// <summary>
        /// 初始化设置端口
        /// </summary>
        bool Setup(int port);

        /// <summary>
        /// 启动监听
        /// </summary>
        bool Start();

        /// <summary>
        /// 停止监听
        /// </summary>
        bool Stop();

        /// <summary>
        /// 获取Session用于发送信息
        /// </summary>
        IAppSession GetSessionByID(string sessionID);

    }
}

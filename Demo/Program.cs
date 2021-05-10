using Demo.DAL;
using Demo.Model;
using Infrastructure.CommandBus;
using Infrastructure.SocketServer;
using System;
namespace Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            // CommandBus
            var dis = new Dispatcher();
            StringRequestInfo cmd = new StringRequestInfo();
            cmd.Key = "ADD";
            dis.ExecuteCommand(null, cmd);
            // socket server
            //var appServer = new AppServer();
            //appServer.Setup(9527);
            //appServer.NewRequestReceived += AppServer_NewRequestReceived;
            //appServer.Start();
            //while (Console.ReadKey().KeyChar != 'q')
            //{
            //    Console.WriteLine();
            //    continue;
            //}
            //appServer.Stop();
        }

        private static void AppServer_NewRequestReceived(AppSession session, byte[] requestInfo)
        {
        }
    }
    public class ADD : CommandBase<StringRequestInfo>
    {
        public override void ExecuteCommand(object session, StringRequestInfo requestInfo)
        {
            throw new NotImplementedException();
        }
    }
    public class StringRequestInfo : IRequestInfo
    {
        public string Key { get; set; }
    }
}

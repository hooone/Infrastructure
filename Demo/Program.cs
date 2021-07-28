using Demo.DAL;
using Demo.Model;
using Infrastructure.CommandBus;
using Infrastructure.SocketClient;
using Infrastructure.SocketServer;
using System;
using System.Threading;

namespace Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            //// CommandBus
            //var dis = new Dispatcher();
            //StringRequestInfo cmd = new StringRequestInfo();
            //cmd.Key = "ADD";
            //dis.ExecuteCommand(null, cmd);

            //// socket server
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

            // socket client
            EasyClient client = new EasyClient();
            var r = client.ConnectAsync("127.0.0.1", 9988);
            client.NewRequestReceived += Client_NewRequestReceived;
            var rst = r.Result;
            //Thread th = new Thread(() =>
            //  {
            //      int n = 0;
            //      while (true)
            //      {
            //          if (client.Send(BitConverter.GetBytes(n)))
            //              n++;
            //          Thread.Sleep(1000);
            //      }
            //  });
            //th.Start();
            Console.ReadLine();
        }

        private static void Client_NewRequestReceived(ClientSession session, byte[] requestInfo)
        {
        }

        private static void AppServer_NewRequestReceived(AppSession session, byte[] requestInfo)
        {
            Thread th = new Thread(() =>
              {
                  int n = 0;
                  while (true)
                  {
                      if (session.Send(BitConverter.GetBytes(n)))
                          n++;
                      Thread.Sleep(1000);
                  }
              });
            th.Start();
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

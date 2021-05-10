using Demo.DAL;
using Demo.Model;
using Infrastructure.Code;
using Infrastructure.CommandBus;
using Infrastructure.DB;
using Infrastructure.SocketServer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
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

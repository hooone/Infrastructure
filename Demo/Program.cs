using Demo.DAL;
using Demo.Model;
using Infrastructure.Code;
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
            var appServer = new AppServer();
            appServer.Setup(9527, SocketMode.Tcp);
            appServer.Start();
            while (Console.ReadKey().KeyChar != 'q')
            {
                Console.WriteLine();
                continue;
            }
        }
    }
}

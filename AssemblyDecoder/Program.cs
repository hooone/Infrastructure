using Infrastructure.Code;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AssemblyDecoder
{
    class Program
    {
        static void Main(string[] args)
        {
            AssemblyInfo assembly = new AssemblyInfo(args[0]);
            var rst = assembly.Load();
            Console.Write(rst);
        }
    }
}

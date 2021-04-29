using Infrastructure.Code;
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
            FileInfo p = new FileInfo(@"C:\Users\techsun\Desktop\Infrastructure\AssemblyDecoder\Program.cs");
            FileInfo g = new FileInfo(@"C:\Users\techsun\Desktop\Infrastructure\AssemblyDecoder\bin\debug\assemblyDecoder.exe");
            if (p.LastWriteTime > g.LastWriteTime)
            {

            }
            Process myProcess = new Process();
            myProcess.StartInfo.FileName = "AssemblyDecoder.exe";
            myProcess.StartInfo.UseShellExecute = false;
            myProcess.StartInfo.RedirectStandardInput = true;
            myProcess.StartInfo.RedirectStandardOutput = true;
            myProcess.StartInfo.RedirectStandardError = true;
            myProcess.StartInfo.CreateNoWindow = true;
            myProcess.StartInfo.Arguments = @"C:\Users\techsun\Desktop\质检车间\澳爱\数据采集\HennessyCodeLink\HennessyCodeLink.csproj";
            myProcess.Start();
            var str = myProcess.StandardOutput.ReadToEnd();
            myProcess.WaitForExit();
            var asm = Newtonsoft.Json.JsonConvert.DeserializeObject<AssemblyInfo>(str);
        }

    }

}

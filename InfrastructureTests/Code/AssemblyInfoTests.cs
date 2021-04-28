using Microsoft.VisualStudio.TestTools.UnitTesting;
using Infrastructure.Code;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Code.Tests
{
    [TestClass()]
    public class AssemblyInfoTests
    {
        [TestMethod()]
        public void LoadTest()
        {
            AssemblyInfo assembly = new AssemblyInfo(@"C:\Users\techsun\Desktop\Infrastructure\Demo\Demo.csproj");
            assembly.Load();
        }
    }
}
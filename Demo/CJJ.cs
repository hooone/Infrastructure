using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Infrastructure.DB;
namespace Demo
{
    [DbTable("aa")]
    public partial class CJJ
    {
        public string TSS { get; set; }
        public void Func1(int c)
        {
            var a = 1;
            a = 2;
            a = 2;
            a = 2;
            a = 2;
            a = 2;
            a = 2;
            a = 2;
            a = 2;
            a = 2;
        }
        public void Func2(int a)
        {

        }
    }

}

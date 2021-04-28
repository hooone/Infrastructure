using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Infrastructure.DB;
namespace Demo
{
    [DbTable("ACTION_INFO")]
    public partial class CJJ
    {
        [DbColumn(DataType.DATE)]
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

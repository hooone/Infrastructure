using FlowEngine.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowEngine.Model
{
    public class TestTotalPayload : ISqlExecutePayload, IDelayPayload
    {
        public string DbName { get; set; }
        public string Sql { get; set; }
        public int SqlExecuteResult { get; set; }
        public Dictionary<string, object> ObjectList { get; set; }
        public int MilliSecond { get; set; }
    }
}

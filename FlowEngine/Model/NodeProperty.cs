using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowEngine.Model
{
    public class NodeProperty
    {
        public string Id { get; set; }
        public string Text { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public Dictionary<string, int> LinkIn { get; set; }
        public Dictionary<string, int> LinkOut { get; set; }
    }
}

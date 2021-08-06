using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowEngine.Model
{
    public class LinkViewModel
    {
        public string Id { get; set; }
        public string FromPoint { get; set; }
        public string ToPoint { get; set; }
        public string FromNode { get; set; }
        public string ToNode { get; set; }
        public Precondition DestCondition { get; set; }
    }
}

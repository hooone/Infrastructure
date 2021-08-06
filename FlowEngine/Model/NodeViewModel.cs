using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowEngine.Model
{
    public class NodeViewModel
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public string Text { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public List<ConditionModel> Conditions { get; set; }
        public List<PropertyModel> Properties { get; set; }
    }
}

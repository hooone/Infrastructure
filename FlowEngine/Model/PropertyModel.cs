using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowEngine.Model
{
    public class PropertyModel
    {
        public string Id { get; set; }
        public string NodeId { get; set; }
        public string Name { get; set; }
        public string DefaultName { get; set; }
        public string Value { get; set; }
        public bool IsCustom { get; set; }
        public OperationType Operation { get; set; }
        public DataType DataType { get; set; }
        public string Description { get; set; }
    }
}

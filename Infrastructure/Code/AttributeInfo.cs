using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Code
{
    public class AttributeInfo
    {
        public string Description { get; set; }
        public string TypeName { get; set; }
        public string TypeFullName { get; set; }
        public List<string> ArgumentList { get; set; } = new List<string>();
    }
}

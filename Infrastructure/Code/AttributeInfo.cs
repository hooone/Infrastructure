using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Code
{
    public class AttributeInfo
    {
        public string TypeFullName { get; set; }
        public List<string> ArgumentList { get; set; } = new List<string>();
    }
}

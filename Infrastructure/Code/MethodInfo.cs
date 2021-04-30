using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Code
{
    public class MethodInfo
    {
        public string Name { get; set; }
        public string ReturnType { get; set; }
        public int MinLine { get; set; }
        public int MaxLine { get; set; }
        public List<AttributeInfo> AttributeList { get; set; } = new List<AttributeInfo>();
    }
}

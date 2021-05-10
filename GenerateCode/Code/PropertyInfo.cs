using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenerateCode.Code
{
    public class PropertyInfo
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public List<AttributeInfo> AttributeList { get; set; } = new List<AttributeInfo>();
    }
}

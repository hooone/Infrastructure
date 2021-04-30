using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Code
{
    public class ClassInfo
    {
        public string Name { get; set; }
        public string NameSpace { get; set; }
        public string FullName { get; set; }
        public List<string> FilePath { get; set; } = new List<string>();
        public List<string> Interfaces { get; set; } = new List<string>();
        public List<AttributeInfo> AttributeList { get; set; } = new List<AttributeInfo>();
        public List<PropertyInfo> PropertyList { get; set; } = new List<PropertyInfo>();
        public List<MethodInfo> MethodList { get; set; } = new List<MethodInfo>();
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.CommandBus
{
    public class StringRequestInfo : IRequestInfo
    {
        public string Key { get; internal set; }
        public string Body { get; internal set; }
        public StringRequestInfo(string key, string body)
        {
            Key = key;
            Body = body;
        }
    }
}

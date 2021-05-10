using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.CommandBus
{
    public class SocketCommandServer
    {
        const string m_Spliter = " ";
        public StringRequestInfo ParseRequestInfo(string source)
        {
            int pos = source.IndexOf(m_Spliter);

            string name = string.Empty;
            string body = string.Empty;

            if (pos > 0)
            {
                name = source.Substring(0, pos);
                body = source.Substring(pos + m_Spliter.Length);
            }
            else
            {
                name = source;
            }

            return new StringRequestInfo(name, body);
        }
    }
}

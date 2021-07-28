using Infrastructure.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowEngine.DAL
{
    public class COracleParameter : IConnectionString
    {
        public string ConnectionString =>
            $"Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST={ServerIp})(PORT={Port}))" +
            $"(CONNECT_DATA=(SERVICE_NAME={DbName})));" +
            $"Persist Security Info=True;User ID={UserName};Password={Password};";

        public string ServerIp { set; get; } = "127.0.0.1";

        public string Port { set; get; } = "1521";

        public string DbName { set; get; } = "XE";

        public string UserName { set; get; } = "RFID";

        public string Password { set; get; } = "RFID";
    }
}

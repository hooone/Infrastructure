using Infrastructure.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowEngine.DTO
{
    [DbTable("LINK")]
    public class Link : IDbModel
    {
        /// 该类型的代码由插件自动生成，请勿修改。

        [DbColumn(DataType.VARCHAR2)]
        public string ID { get; set; }

        [DbColumn(DataType.VARCHAR2)]
        public string LINKFROM { get; set; }

        [DbColumn(DataType.VARCHAR2)]
        public string LINKTO { get; set; }

    }
}

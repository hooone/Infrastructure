using Infrastructure.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowEngine.DTO
{
    [DbTable("PROPERTY")]
    public class PropertyDTO : IDbModel
    {
        /// 该类型的代码由插件自动生成，请勿修改。

        [DbColumn(DataType.VARCHAR2)]
        public string ID { get; set; }

        [DbColumn(DataType.VARCHAR2)]
        public string NODEID { get; set; }

        [DbColumn(DataType.VARCHAR2)]
        public string NAME { get; set; }

        [DbColumn(DataType.VARCHAR2)]
        public string VALUE { get; set; }

        [DbColumn(DataType.NUMBER)]
        public int CONDITION { get; set; }

        [DbColumn(DataType.VARCHAR2)]
        public string DESCRIPTION { get; set; }

    }
}

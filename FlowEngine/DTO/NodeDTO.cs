using Infrastructure.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowEngine.DTO
{
    [DbTable("NODE")]
    public class NodeDTO : IDbModel
    {
        /// 该类型的代码由插件自动生成，请勿修改。

        [DbColumn(DataType.VARCHAR2)]
        public string ID { get; set; }

        [DbColumn(DataType.VARCHAR2)]
        public string TYPE { get; set; }

        [DbColumn(DataType.VARCHAR2)]
        public string TEXT { get; set; }

        [DbColumn(DataType.NUMBER)]
        public int X { get; set; }

        [DbColumn(DataType.NUMBER)]
        public int Y { get; set; }

        [DbColumn(DataType.NUMBER)]
        public int CUSTOMABLE { get; set; }

    }
}

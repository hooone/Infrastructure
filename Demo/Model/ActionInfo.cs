using Infrastructure.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Demo.Model
{
    [DbTable("ACTION_INFO")]
    public class ActionInfo : IDbModel
    {
        /// 该类型的代码由插件自动生成，请勿修改。

        /// <summary>
        /// 自增主键
        /// </summary>
        [DbColumn(DataType.NUMBER)]
        public int NUM { get; set; }

        [DbColumn(DataType.VARCHAR2)]
        public string ACTION_CODE { get; set; }

        [DbColumn(DataType.TIMESTAMP_6)]
        public DateTime CREATE_TIME { get; set; }

        [DbColumn(DataType.VARCHAR2)]
        public string DEST_DEVICE { get; set; }

        [DbColumn(DataType.VARCHAR2)]
        public string SOURCE_DEVICE { get; set; }

        [DbColumn(DataType.VARCHAR2)]
        public string ACTION_REQUEST { get; set; }

        [DbColumn(DataType.VARCHAR2)]
        public string ACTION_RESPONSE { get; set; }

        [DbColumn(DataType.NUMBER)]
        public int FLAG { get; set; }

    }
}

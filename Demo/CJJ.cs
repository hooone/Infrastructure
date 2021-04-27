using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Infrastructure.DB;
namespace Demo2
{
    [DbTable("ACTION_INFO")]
    public class CJJ    
    {
        /// <summary>
        /// 自增主键
        /// </summary>
        [DbColumn(DataType.NUMBER)]
        public int NUM { get; set; }
        [DbColumn(DataType.VARCHAR2)]
        public string ACTION_CODE { get; set; }
        [DbColumn(DataType.TIMESTAMP(6))]
        public string CREATE_TIME { get; set; }
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

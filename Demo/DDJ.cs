using Infrastructure.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Demo
{
    [DbTable("ACTION_INFO")]
    public class DDJ : IDbModel
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

    }//


    [DbTable("ACTION_INFO")]
    public class DDJ2 : IDbAccess
    {
        public SqlHelper helper { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        [DbInsert]
        public int insert(DDJ obj)
        {
            /// 该方法的代码由插件自动生成，请勿修改。
            string sql = @"INSERT INTO ACTION_INFO (NUM,ACTION_CODE,CREATE_TIME,DEST_DEVICE,SOURCE_DEVICE,ACTION_REQUEST,ACTION_RESPONSE,FLAG ) values (@NUM,@ACTION_CODE,@CREATE_TIME,@DEST_DEVICE,@SOURCE_DEVICE,@ACTION_REQUEST,@ACTION_RESPONSE,@FLAG)";
            return helper.ExecuteNonQuery(sql, obj.NUM, obj.ACTION_CODE, obj.CREATE_TIME, obj.DEST_DEVICE, obj.SOURCE_DEVICE, obj.ACTION_REQUEST, obj.ACTION_RESPONSE, obj.FLAG);
        }

    }


}

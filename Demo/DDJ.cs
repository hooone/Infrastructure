using Infrastructure.DB;
using System;
using System.Collections.Generic;
using System.Data;
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

        [DbInsert]
        public int insert(DDJ obj)
        {
            /// 该方法的代码由插件自动生成，请勿修改。
            string sql = @"INSERT INTO ACTION_INFO (NUM,ACTION_CODE,CREATE_TIME,DEST_DEVICE,SOURCE_DEVICE,ACTION_REQUEST,ACTION_RESPONSE,FLAG ) values (@NUM,@ACTION_CODE,@CREATE_TIME,@DEST_DEVICE,@SOURCE_DEVICE,@ACTION_REQUEST,@ACTION_RESPONSE,@FLAG)";
            return helper.ExecuteNonQuery(sql, obj.NUM, obj.ACTION_CODE, obj.CREATE_TIME, obj.DEST_DEVICE, obj.SOURCE_DEVICE, obj.ACTION_REQUEST, obj.ACTION_RESPONSE, obj.FLAG);
        }

        [DbUpdate]
        [SqlValue(nameof(DDJ.ACTION_CODE))]
        public int update(DDJ obj)
        {
            /// 该方法的代码由插件自动生成，请勿修改。
            string sql = @"UPDATE ACTION_INFO SET ACTION_CODE=@ACTION_CODE";
            return helper.ExecuteNonQuery(sql, obj.ACTION_CODE);
        }

        [DbDelete]
        public int delete(DDJ obj)
        {
            /// 该方法的代码由插件自动生成，请勿修改。
            string sql = @"DELETE ACTION_INFO";
            return helper.ExecuteNonQuery(sql);
        }


        [DbRead]
        [SqlKey(nameof(DDJ.NUM))]
        public List<DDJ> read(DDJ obj)
        {
            /// 该方法的代码由插件自动生成，请勿修改。
            string sql = @"SELECT * FROM ACTION_INFO WHERE NUM=@NUM";
            DataTable dt = helper.Query(sql, obj.NUM);
            List<DDJ> rst = new List<DDJ>();
            foreach (DataRow row in dt.Rows)
            {
                DDJ t = new DDJ();
                t.NUM = row[nameof(DDJ.NUM)].TryToInt();
                t.ACTION_CODE = row[nameof(DDJ.ACTION_CODE)].TryToString();
                t.CREATE_TIME = row[nameof(DDJ.CREATE_TIME)].TryToString();
                t.DEST_DEVICE = row[nameof(DDJ.DEST_DEVICE)].TryToString();
                t.SOURCE_DEVICE = row[nameof(DDJ.SOURCE_DEVICE)].TryToString();
                t.ACTION_REQUEST = row[nameof(DDJ.ACTION_REQUEST)].TryToString();
                t.ACTION_RESPONSE = row[nameof(DDJ.ACTION_RESPONSE)].TryToString();
                t.FLAG = row[nameof(DDJ.FLAG)].TryToInt();
                rst.Add(t);
            }
            return rst;
        }

    }


}

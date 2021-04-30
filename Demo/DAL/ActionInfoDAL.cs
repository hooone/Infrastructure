using Demo.Model;
using Infrastructure.DB;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Demo.DAL
{
    [DbTable("ACTION_INFO")]
    public class ActionInfoDAL : IDbAccess
    {
        public SqlHelper Helper { get; set; }
        public ActionInfoDAL(SqlHelper helper)
        {
            Helper = helper;
        }
        [DbInsert]
        public int insert(ActionInfo obj)
        {
            /// 该方法的代码由插件自动生成，请勿修改。
            string sql = @"INSERT INTO ACTION_INFO (NUM,ACTION_CODE,CREATE_TIME,DEST_DEVICE,SOURCE_DEVICE,ACTION_REQUEST,ACTION_RESPONSE,FLAG ) values (@NUM,@ACTION_CODE,@CREATE_TIME,@DEST_DEVICE,@SOURCE_DEVICE,@ACTION_REQUEST,@ACTION_RESPONSE,@FLAG)";
            return Helper.ExecuteNonQuery(sql, obj.NUM, obj.ACTION_CODE, obj.CREATE_TIME, obj.DEST_DEVICE, obj.SOURCE_DEVICE, obj.ACTION_REQUEST, obj.ACTION_RESPONSE, obj.FLAG);
        }
        [DbUpdate]
        [SqlKey(nameof(ActionInfo.ACTION_CODE))]
        [SqlValue(nameof(ActionInfo.DEST_DEVICE))]
        public int update(ActionInfo obj)
        {
            /// 该方法的代码由插件自动生成，请勿修改。
            string sql = @"UPDATE ACTION_INFO SET DEST_DEVICE=@DEST_DEVICE WHERE ACTION_CODE=@ACTION_CODE";
            return Helper.ExecuteNonQuery(sql, obj.DEST_DEVICE, obj.ACTION_CODE);
        }
        [DbRead]
        public List<ActionInfo> read()
        {
            /// 该方法的代码由插件自动生成，请勿修改。
            string sql = @"SELECT * FROM ACTION_INFO";
            DataTable dt = Helper.Query(sql);
            List<ActionInfo> rst = new List<ActionInfo>();
            foreach (DataRow row in dt.Rows)
            {
                ActionInfo t = new ActionInfo();
                t.NUM = row[nameof(ActionInfo.NUM)].TryToInt();
                t.ACTION_CODE = row[nameof(ActionInfo.ACTION_CODE)].TryToString();
                t.CREATE_TIME = row[nameof(ActionInfo.CREATE_TIME)].TryToDateTime();
                t.DEST_DEVICE = row[nameof(ActionInfo.DEST_DEVICE)].TryToString();
                t.SOURCE_DEVICE = row[nameof(ActionInfo.SOURCE_DEVICE)].TryToString();
                t.ACTION_REQUEST = row[nameof(ActionInfo.ACTION_REQUEST)].TryToString();
                t.ACTION_RESPONSE = row[nameof(ActionInfo.ACTION_RESPONSE)].TryToString();
                t.FLAG = row[nameof(ActionInfo.FLAG)].TryToInt();
                rst.Add(t);
            }
            return rst;
        }
        [DbDelete]
        [SqlKey(nameof(ActionInfo.DEST_DEVICE))]
        public int delete(ActionInfo obj)
        {
            /// 该方法的代码由插件自动生成，请勿修改。
            string sql = @"DELETE ACTION_INFO WHERE DEST_DEVICE=@DEST_DEVICE";
            return Helper.ExecuteNonQuery(sql, obj.DEST_DEVICE);
        }
    }
}

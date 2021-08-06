using Infrastructure.DB;
using FlowEngine.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;

namespace FlowEngine.DAL
{
    [DbTable("POINT")]
    public class PointDAL : IDbAccess
    {
        public SqlHelper Helper { get; set; }
        public PointDAL(SqlHelper helper)
        {
            Helper = helper;
        }

        [DbInsert]
        public int insert(PointDTO obj)
        {
            /// 该方法的代码由插件自动生成，请勿修改。
            string sql = @"INSERT INTO POINT (ID,NODEID,SEQ ) values (@ID,@NODEID,@SEQ)";
            return Helper.ExecuteNonQuery(sql, obj.ID, obj.NODEID, obj.SEQ);
        }

        [DbRead]
        [SqlKey(nameof(PointDTO.NODEID))]
        public List<PointDTO> ReadByNode(PointDTO obj)
        {
            /// 该方法的代码由插件自动生成，请勿修改。
            string sql = @"SELECT * FROM POINT WHERE NODEID=@NODEID";
            DataTable dt = Helper.Query(sql, obj.NODEID);
            List<PointDTO> rst = new List<PointDTO>();
            foreach (DataRow row in dt.Rows)
            {
                PointDTO t = new PointDTO();
                t.ID = row[nameof(PointDTO.ID)].TryToString();
                t.NODEID = row[nameof(PointDTO.NODEID)].TryToString();
                t.SEQ = row[nameof(PointDTO.SEQ)].TryToInt();
                rst.Add(t);
            }
            return rst;
        }

        [DbDelete]
        [SqlKey(nameof(PointDTO.NODEID))]
        public int DeleteByNode(PointDTO obj)
        {
            /// 该方法的代码由插件自动生成，请勿修改。
            string sql = @"DELETE POINT WHERE NODEID=@NODEID";
            return Helper.ExecuteNonQuery(sql, obj.NODEID);
        }
    }
}

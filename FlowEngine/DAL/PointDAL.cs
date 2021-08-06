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
            string sql = @"INSERT INTO POINT (ID,NODEID,SEQ,ISPRECONDITION ) values (@ID,@NODEID,@SEQ,@ISPRECONDITION)";
            return Helper.ExecuteNonQuery(sql, obj.ID, obj.NODEID, obj.SEQ, obj.ISPRECONDITION);
        }

        [DbRead]
        [SqlKey(nameof(PointDTO.ID))]
        public List<PointDTO> ReadByID(PointDTO obj)
        {
            /// 该方法的代码由插件自动生成，请勿修改。
            string sql = @"SELECT * FROM POINT WHERE ID=@ID";
            DataTable dt = Helper.Query(sql, obj.ID);
            List<PointDTO> rst = new List<PointDTO>();
            foreach (DataRow row in dt.Rows)
            {
                PointDTO t = new PointDTO();
                t.ID = row[nameof(PointDTO.ID)].TryToString();
                t.NODEID = row[nameof(PointDTO.NODEID)].TryToString();
                t.SEQ = row[nameof(PointDTO.SEQ)].TryToInt();
                t.ISPRECONDITION = row[nameof(PointDTO.ISPRECONDITION)].TryToInt();
                rst.Add(t);
            }
            return rst;
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
                t.ISPRECONDITION = row[nameof(PointDTO.ISPRECONDITION)].TryToInt();
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

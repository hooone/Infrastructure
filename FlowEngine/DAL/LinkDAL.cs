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
    [DbTable("LINK")]
    public class LinkDAL : IDbAccess
    {
        public SqlHelper Helper { get; set; }
        public LinkDAL(SqlHelper helper)
        {
            Helper = helper;
        }

        [DbInsert]
        public int insert(LinkDTO obj)
        {
            /// 该方法的代码由插件自动生成，请勿修改。
            string sql = @"INSERT INTO LINK (ID,FROMPOINT,TOPOINT,FROMNODE,TONODE ) values (@ID,@FROMPOINT,@TOPOINT,@FROMNODE,@TONODE)";
            return Helper.ExecuteNonQuery(sql, obj.ID, obj.FROMPOINT, obj.TOPOINT, obj.FROMNODE, obj.TONODE);
        }

        [DbRead]
        public List<LinkDTO> read(LinkDTO obj)
        {
            /// 该方法的代码由插件自动生成，请勿修改。
            string sql = @"SELECT * FROM LINK";
            DataTable dt = Helper.Query(sql);
            List<LinkDTO> rst = new List<LinkDTO>();
            foreach (DataRow row in dt.Rows)
            {
                LinkDTO t = new LinkDTO();
                t.ID = row[nameof(LinkDTO.ID)].TryToString();
                t.FROMPOINT = row[nameof(LinkDTO.FROMPOINT)].TryToString();
                t.TOPOINT = row[nameof(LinkDTO.TOPOINT)].TryToString();
                t.FROMNODE = row[nameof(LinkDTO.FROMNODE)].TryToString();
                t.TONODE = row[nameof(LinkDTO.TONODE)].TryToString();
                rst.Add(t);
            }
            return rst;
        }

        [DbRead]
        [SqlKey(nameof(LinkDTO.FROMNODE))]
        public List<LinkDTO> ReadByFromNode(LinkDTO obj)
        {
            /// 该方法的代码由插件自动生成，请勿修改。
            string sql = @"SELECT * FROM LINK WHERE FROMNODE=@FROMNODE";
            DataTable dt = Helper.Query(sql, obj.FROMNODE);
            List<LinkDTO> rst = new List<LinkDTO>();
            foreach (DataRow row in dt.Rows)
            {
                LinkDTO t = new LinkDTO();
                t.ID = row[nameof(LinkDTO.ID)].TryToString();
                t.FROMPOINT = row[nameof(LinkDTO.FROMPOINT)].TryToString();
                t.TOPOINT = row[nameof(LinkDTO.TOPOINT)].TryToString();
                t.FROMNODE = row[nameof(LinkDTO.FROMNODE)].TryToString();
                t.TONODE = row[nameof(LinkDTO.TONODE)].TryToString();
                rst.Add(t);
            }
            return rst;
        }

        [DbRead]
        [SqlKey(nameof(LinkDTO.FROMPOINT))]
        public List<LinkDTO> ReadByFrom(LinkDTO obj)
        {
            /// 该方法的代码由插件自动生成，请勿修改。
            string sql = @"SELECT * FROM LINK WHERE FROMPOINT=@FROMPOINT";
            DataTable dt = Helper.Query(sql, obj.FROMPOINT);
            List<LinkDTO> rst = new List<LinkDTO>();
            foreach (DataRow row in dt.Rows)
            {
                LinkDTO t = new LinkDTO();
                t.ID = row[nameof(LinkDTO.ID)].TryToString();
                t.FROMPOINT = row[nameof(LinkDTO.FROMPOINT)].TryToString();
                t.TOPOINT = row[nameof(LinkDTO.TOPOINT)].TryToString();
                t.FROMNODE = row[nameof(LinkDTO.FROMNODE)].TryToString();
                t.TONODE = row[nameof(LinkDTO.TONODE)].TryToString();
                rst.Add(t);
            }
            return rst;
        }

        [DbRead]
        [SqlKey(nameof(LinkDTO.TOPOINT))]
        public List<LinkDTO> ReadByTo(LinkDTO obj)
        {
            /// 该方法的代码由插件自动生成，请勿修改。
            string sql = @"SELECT * FROM LINK WHERE TOPOINT=@TOPOINT";
            DataTable dt = Helper.Query(sql, obj.TOPOINT);
            List<LinkDTO> rst = new List<LinkDTO>();
            foreach (DataRow row in dt.Rows)
            {
                LinkDTO t = new LinkDTO();
                t.ID = row[nameof(LinkDTO.ID)].TryToString();
                t.FROMPOINT = row[nameof(LinkDTO.FROMPOINT)].TryToString();
                t.TOPOINT = row[nameof(LinkDTO.TOPOINT)].TryToString();
                t.FROMNODE = row[nameof(LinkDTO.FROMNODE)].TryToString();
                t.TONODE = row[nameof(LinkDTO.TONODE)].TryToString();
                rst.Add(t);
            }
            return rst;
        }

        [DbDelete]
        [SqlKey(nameof(LinkDTO.ID))]
        public int Delete(LinkDTO obj)
        {
            /// 该方法的代码由插件自动生成，请勿修改。
            string sql = @"DELETE LINK WHERE ID=@ID";
            return Helper.ExecuteNonQuery(sql, obj.ID);
        }
    }
}

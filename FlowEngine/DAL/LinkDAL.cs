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
            string sql = @"INSERT INTO LINK (ID,LINKFROM,LINKTO ) values (@ID,@LINKFROM,@LINKTO)";
            return Helper.ExecuteNonQuery(sql, obj.ID, obj.LINKFROM, obj.LINKTO);
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
                t.LINKFROM = row[nameof(LinkDTO.LINKFROM)].TryToString();
                t.LINKTO = row[nameof(LinkDTO.LINKTO)].TryToString();
                rst.Add(t);
            }
            return rst;
        }

        [DbRead]
        [SqlKey(nameof(LinkDTO.LINKFROM))]
        public List<LinkDTO> ReadByFrom(LinkDTO obj)
        {
            /// 该方法的代码由插件自动生成，请勿修改。
            string sql = @"SELECT * FROM LINK WHERE LINKFROM=@LINKFROM";
            DataTable dt = Helper.Query(sql, obj.LINKFROM);
            List<LinkDTO> rst = new List<LinkDTO>();
            foreach (DataRow row in dt.Rows)
            {
                LinkDTO t = new LinkDTO();
                t.ID = row[nameof(LinkDTO.ID)].TryToString();
                t.LINKFROM = row[nameof(LinkDTO.LINKFROM)].TryToString();
                t.LINKTO = row[nameof(LinkDTO.LINKTO)].TryToString();
                rst.Add(t);
            }
            return rst;
        }

        [DbRead]
        [SqlKey(nameof(LinkDTO.LINKTO))]
        public List<LinkDTO> ReadByTo(LinkDTO obj)
        {
            /// 该方法的代码由插件自动生成，请勿修改。
            string sql = @"SELECT * FROM LINK WHERE LINKTO=@LINKTO";
            DataTable dt = Helper.Query(sql, obj.LINKTO);
            List<LinkDTO> rst = new List<LinkDTO>();
            foreach (DataRow row in dt.Rows)
            {
                LinkDTO t = new LinkDTO();
                t.ID = row[nameof(LinkDTO.ID)].TryToString();
                t.LINKFROM = row[nameof(LinkDTO.LINKFROM)].TryToString();
                t.LINKTO = row[nameof(LinkDTO.LINKTO)].TryToString();
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

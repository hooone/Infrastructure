using Infrastructure.DB;
using FlowEngine.Model;
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
        public int insert(Link obj)
        {
            /// 该方法的代码由插件自动生成，请勿修改。
            string sql = @"INSERT INTO LINK (ID,LINKFROM,LINKTO ) values (@ID,@LINKFROM,@LINKTO)";
            return Helper.ExecuteNonQuery(sql, obj.ID, obj.LINKFROM, obj.LINKTO);
        }

        [DbRead]
        [SqlKey(nameof(Link.LINKFROM))]
        public List<Link> read(Link obj)
        {
            /// 该方法的代码由插件自动生成，请勿修改。
            string sql = @"SELECT * FROM LINK WHERE LINKFROM=@LINKFROM";
            DataTable dt = Helper.Query(sql, obj.LINKFROM);
            List<Link> rst = new List<Link>();
            foreach (DataRow row in dt.Rows)
            {
                Link t = new Link();
                t.ID = row[nameof(Link.ID)].TryToString();
                t.LINKFROM = row[nameof(Link.LINKFROM)].TryToString();
                t.LINKTO = row[nameof(Link.LINKTO)].TryToString();
                rst.Add(t);
            }
            return rst;
        }
    }
}

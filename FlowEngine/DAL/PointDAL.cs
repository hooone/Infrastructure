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
        public int insert(Point obj)
        {
            /// 该方法的代码由插件自动生成，请勿修改。
            string sql = @"INSERT INTO POINT (ID,NODEID ) values (@ID,@NODEID)";
            return Helper.ExecuteNonQuery(sql, obj.ID, obj.NODEID);
        }

        [DbRead]
        public List<Point> read()
        {
            /// 该方法的代码由插件自动生成，请勿修改。
            string sql = @"SELECT * FROM POINT";
            DataTable dt = Helper.Query(sql);
            List<Point> rst = new List<Point>();
            foreach (DataRow row in dt.Rows)
            {
                Point t = new Point();
                t.ID = row[nameof(Point.ID)].TryToString();
                t.NODEID = row[nameof(Point.NODEID)].TryToString();
                rst.Add(t);
            }
            return rst;
        }
    }
}

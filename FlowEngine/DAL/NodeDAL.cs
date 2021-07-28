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
    [DbTable("NODE")]
    public class NodeDAL : IDbAccess
    {
        public SqlHelper Helper { get; set; }
        public NodeDAL(SqlHelper helper)
        {
            Helper = helper;
        }

        [DbInsert]
        public int insert(Node obj)
        {
            /// 该方法的代码由插件自动生成，请勿修改。
            string sql = @"INSERT INTO NODE (ID,TYPE,TEXT,X,Y ) values (@ID,@TYPE,@TEXT,@X,@Y)";
            return Helper.ExecuteNonQuery(sql, obj.ID, obj.TYPE, obj.TEXT, obj.X, obj.Y);
        }

        [DbRead]
        public List<Node> read()
        {
            /// 该方法的代码由插件自动生成，请勿修改。
            string sql = @"SELECT * FROM NODE";
            DataTable dt = Helper.Query(sql);
            List<Node> rst = new List<Node>();
            foreach (DataRow row in dt.Rows)
            {
                Node t = new Node();
                t.ID = row[nameof(Node.ID)].TryToString();
                t.TYPE = row[nameof(Node.TYPE)].TryToString();
                t.TEXT = row[nameof(Node.TEXT)].TryToString();
                t.X = row[nameof(Node.X)].TryToInt();
                t.Y = row[nameof(Node.Y)].TryToInt();
                rst.Add(t);
            }
            return rst;
        }
    }
}

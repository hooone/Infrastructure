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
    [DbTable("NODE")]
    public class NodeDAL : IDbAccess
    {
        public SqlHelper Helper { get; set; }
        public NodeDAL(SqlHelper helper)
        {
            Helper = helper;
        }

        [DbInsert]
        public int insert(NodeDTO obj)
        {
            /// 该方法的代码由插件自动生成，请勿修改。
            string sql = @"INSERT INTO NODE (ID,TYPE,TEXT,X,Y,CUSTOMABLE ) values (@ID,@TYPE,@TEXT,@X,@Y,@CUSTOMABLE)";
            return Helper.ExecuteNonQuery(sql, obj.ID, obj.TYPE, obj.TEXT, obj.X, obj.Y, obj.CUSTOMABLE);
        }

        [DbRead]
        public List<NodeDTO> ReadAll(NodeDTO obj)
        {
            /// 该方法的代码由插件自动生成，请勿修改。
            string sql = @"SELECT * FROM NODE";
            DataTable dt = Helper.Query(sql);
            List<NodeDTO> rst = new List<NodeDTO>();
            foreach (DataRow row in dt.Rows)
            {
                NodeDTO t = new NodeDTO();
                t.ID = row[nameof(NodeDTO.ID)].TryToString();
                t.TYPE = row[nameof(NodeDTO.TYPE)].TryToString();
                t.TEXT = row[nameof(NodeDTO.TEXT)].TryToString();
                t.X = row[nameof(NodeDTO.X)].TryToInt();
                t.Y = row[nameof(NodeDTO.Y)].TryToInt();
                t.CUSTOMABLE = row[nameof(NodeDTO.CUSTOMABLE)].TryToInt();
                rst.Add(t);
            }
            return rst;
        }
        [DbRead]
        [SqlKey(nameof(NodeDTO.ID))]
        public List<NodeDTO> ReadById(NodeDTO obj)
        {
            /// 该方法的代码由插件自动生成，请勿修改。
            string sql = @"SELECT * FROM NODE WHERE ID=@ID";
            DataTable dt = Helper.Query(sql, obj.ID);
            List<NodeDTO> rst = new List<NodeDTO>();
            foreach (DataRow row in dt.Rows)
            {
                NodeDTO t = new NodeDTO();
                t.ID = row[nameof(NodeDTO.ID)].TryToString();
                t.TYPE = row[nameof(NodeDTO.TYPE)].TryToString();
                t.TEXT = row[nameof(NodeDTO.TEXT)].TryToString();
                t.X = row[nameof(NodeDTO.X)].TryToInt();
                t.Y = row[nameof(NodeDTO.Y)].TryToInt();
                t.CUSTOMABLE = row[nameof(NodeDTO.CUSTOMABLE)].TryToInt();
                rst.Add(t);
            }
            return rst;
        }

        [DbUpdate]
        [SqlKey(nameof(NodeDTO.ID))]
        [SqlValue(nameof(NodeDTO.X), nameof(NodeDTO.Y))]
        public int UpdateLocation(NodeDTO obj)
        {
            /// 该方法的代码由插件自动生成，请勿修改。
            string sql = @"UPDATE NODE SET X=@X,Y=@Y WHERE ID=@ID";
            return Helper.ExecuteNonQuery(sql, obj.X, obj.Y, obj.ID);
        }

        [DbDelete]
        [SqlKey(nameof(NodeDTO.ID))]
        public int Delete(NodeDTO obj)
        {
            /// 该方法的代码由插件自动生成，请勿修改。
            string sql = @"DELETE NODE WHERE ID=@ID";
            return Helper.ExecuteNonQuery(sql, obj.ID);
        }

        [DbUpdate]
        [SqlKey(nameof(NodeDTO.ID))]
        [SqlValue(nameof(NodeDTO.TEXT))]
        public int UpdateText(NodeDTO obj)
        {
            /// 该方法的代码由插件自动生成，请勿修改。
            string sql = @"UPDATE NODE SET TEXT=@TEXT WHERE ID=@ID";
            return Helper.ExecuteNonQuery(sql, obj.TEXT, obj.ID);
        }
    }
}

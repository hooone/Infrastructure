using FlowEngine.DTO;
using Infrastructure.DB;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowEngine.DAL
{
    [DbTable("PROPERTY")]
    public class PropertyDAL : IDbAccess
    {
        public SqlHelper Helper { get; set; }
        public PropertyDAL(SqlHelper helper)
        {
            Helper = helper;
        }
        [DbInsert]
        public int insert(PropertyDTO obj)
        {
            /// 该方法的代码由插件自动生成，请勿修改。
            string sql = @"INSERT INTO PROPERTY (ID,NODEID,NAME,VALUE,CONDITION ) values (@ID,@NODEID,@NAME,@VALUE,@CONDITION)";
            return Helper.ExecuteNonQuery(sql, obj.ID, obj.NODEID, obj.NAME, obj.VALUE, obj.CONDITION);
        }

        [DbRead]
        [SqlKey(nameof(PropertyDTO.NODEID))]
        public List<PropertyDTO> ReadByNode(PropertyDTO obj)
        {
            /// 该方法的代码由插件自动生成，请勿修改。
            string sql = @"SELECT * FROM PROPERTY WHERE NODEID=@NODEID";
            DataTable dt = Helper.Query(sql, obj.NODEID);
            List<PropertyDTO> rst = new List<PropertyDTO>();
            foreach (DataRow row in dt.Rows)
            {
                PropertyDTO t = new PropertyDTO();
                t.ID = row[nameof(PropertyDTO.ID)].TryToString();
                t.NODEID = row[nameof(PropertyDTO.NODEID)].TryToString();
                t.NAME = row[nameof(PropertyDTO.NAME)].TryToString();
                t.VALUE = row[nameof(PropertyDTO.VALUE)].TryToString();
                t.CONDITION = row[nameof(PropertyDTO.CONDITION)].TryToInt();
                rst.Add(t);
            }
            return rst;
        }

        [DbDelete]
        [SqlKey(nameof(PropertyDTO.NODEID))]
        public int DeleteByNode(PropertyDTO obj)
        {
            /// 该方法的代码由插件自动生成，请勿修改。
            string sql = @"DELETE PROPERTY WHERE NODEID=@NODEID";
            return Helper.ExecuteNonQuery(sql, obj.NODEID);
        }
    }
}

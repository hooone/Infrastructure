using FlowEngine.DTO;
using Infrastructure.DB;
using System.Collections.Generic;
using System.Data;

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
            string sql = @"INSERT INTO PROPERTY (ID,NODEID,NAME,VALUE,OPERATION,DESCRIPTION,ISCUSTOM,DATATYPE,DEFAULTNAME ) values (@ID,@NODEID,@NAME,@VALUE,@OPERATION,@DESCRIPTION,@ISCUSTOM,@DATATYPE,@DEFAULTNAME)";
            return Helper.ExecuteNonQuery(sql, obj.ID, obj.NODEID, obj.NAME, obj.VALUE, obj.OPERATION, obj.DESCRIPTION, obj.ISCUSTOM, obj.DATATYPE, obj.DEFAULTNAME);
        }

        [DbRead]
        public List<PropertyDTO> ReadAll(PropertyDTO obj)
        {
            /// 该方法的代码由插件自动生成，请勿修改。
            string sql = @"SELECT * FROM PROPERTY";
            DataTable dt = Helper.Query(sql);
            List<PropertyDTO> rst = new List<PropertyDTO>();
            foreach (DataRow row in dt.Rows)
            {
                PropertyDTO t = new PropertyDTO();
                t.ID = row[nameof(PropertyDTO.ID)].TryToString();
                t.NODEID = row[nameof(PropertyDTO.NODEID)].TryToString();
                t.NAME = row[nameof(PropertyDTO.NAME)].TryToString();
                t.VALUE = row[nameof(PropertyDTO.VALUE)].TryToString();
                t.OPERATION = row[nameof(PropertyDTO.OPERATION)].TryToString();
                t.DESCRIPTION = row[nameof(PropertyDTO.DESCRIPTION)].TryToString();
                t.ISCUSTOM = row[nameof(PropertyDTO.ISCUSTOM)].TryToInt();
                t.DATATYPE = row[nameof(PropertyDTO.DATATYPE)].TryToString();
                t.DEFAULTNAME = row[nameof(PropertyDTO.DEFAULTNAME)].TryToString();
                rst.Add(t);
            }
            return rst;
        }

        [DbRead]
        [SqlKey(nameof(PropertyDTO.ID))]
        public List<PropertyDTO> ReadById(PropertyDTO obj)
        {
            /// 该方法的代码由插件自动生成，请勿修改。
            string sql = @"SELECT * FROM PROPERTY WHERE ID=@ID";
            DataTable dt = Helper.Query(sql, obj.ID);
            List<PropertyDTO> rst = new List<PropertyDTO>();
            foreach (DataRow row in dt.Rows)
            {
                PropertyDTO t = new PropertyDTO();
                t.ID = row[nameof(PropertyDTO.ID)].TryToString();
                t.NODEID = row[nameof(PropertyDTO.NODEID)].TryToString();
                t.NAME = row[nameof(PropertyDTO.NAME)].TryToString();
                t.VALUE = row[nameof(PropertyDTO.VALUE)].TryToString();
                t.OPERATION = row[nameof(PropertyDTO.OPERATION)].TryToString();
                t.DESCRIPTION = row[nameof(PropertyDTO.DESCRIPTION)].TryToString();
                t.ISCUSTOM = row[nameof(PropertyDTO.ISCUSTOM)].TryToInt();
                t.DATATYPE = row[nameof(PropertyDTO.DATATYPE)].TryToString();
                t.DEFAULTNAME = row[nameof(PropertyDTO.DEFAULTNAME)].TryToString();
                rst.Add(t);
            }
            return rst;
        }

        [DbUpdate]
        [SqlKey(nameof(PropertyDTO.ID))]
        [SqlValue(nameof(PropertyDTO.NAME), nameof(PropertyDTO.OPERATION), nameof(PropertyDTO.DATATYPE), nameof(PropertyDTO.VALUE), nameof(PropertyDTO.DESCRIPTION))]
        public int Update(PropertyDTO obj)
        {
            /// 该方法的代码由插件自动生成，请勿修改。
            string sql = @"UPDATE PROPERTY SET NAME=@NAME,OPERATION=@OPERATION,DATATYPE=@DATATYPE,VALUE=@VALUE,DESCRIPTION=@DESCRIPTION WHERE ID=@ID";
            return Helper.ExecuteNonQuery(sql, obj.NAME, obj.OPERATION, obj.DATATYPE, obj.VALUE, obj.DESCRIPTION, obj.ID);
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
                t.OPERATION = row[nameof(PropertyDTO.OPERATION)].TryToString();
                t.DESCRIPTION = row[nameof(PropertyDTO.DESCRIPTION)].TryToString();
                t.ISCUSTOM = row[nameof(PropertyDTO.ISCUSTOM)].TryToInt();
                t.DATATYPE = row[nameof(PropertyDTO.DATATYPE)].TryToString();
                t.DEFAULTNAME = row[nameof(PropertyDTO.DEFAULTNAME)].TryToString();
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

        [DbDelete]
        [SqlKey(nameof(PropertyDTO.ID))]
        public int DeleteById(PropertyDTO obj)
        {
            /// 该方法的代码由插件自动生成，请勿修改。
            string sql = @"DELETE PROPERTY WHERE ID=@ID";
            return Helper.ExecuteNonQuery(sql, obj.ID);
        }
    }
}

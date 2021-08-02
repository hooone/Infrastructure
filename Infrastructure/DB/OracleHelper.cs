using Infrastructure.Log;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Infrastructure.DB
{
    public class OracleHelper : SqlHelper
    {
        public string aa = "1";
        private ILog Log = new NopLogger();
        private OracleConnection _conn;
        public OracleConnection Conn
        {
            get
            {
                if (_conn == null)
                {
                    return _conn;
                }
                else if (_conn.State == ConnectionState.Closed)
                    _conn.Open();
                return _conn;
            }
            set => _conn = value;
        }

        public bool Close()
        {
            if (_conn != null)
            {
                _conn.Close();
            }
            return true;
        }

        public bool Connect(string connect)
        {
            bool flag;
            try
            {
                _conn = new OracleConnection(connect);
                _conn.Open();
                flag = true;
            }
            catch (Exception ex)
            {
                Log.Debug($"连接数据库失败!{ex.Message}");
                flag = false;
            }
            return flag;
        }

        public int ExecuteNonQuery(string sql, params object[] parameters)
        {
            CheckConnection();
            OracleCommand command = Conn.CreateCommand();
            command.CommandText = sql.Replace("@", ":");
            AttachParameters(command, command.CommandText, parameters);
            int num = command.ExecuteNonQuery();
            command.Dispose();
            return num;
        }

        public DataTable Query(string sql, params object[] parameters)
        {
            sql = sql.Replace("@", ":");
            CheckConnection();
            OracleCommand command = Conn.CreateCommand();
            command.CommandText = sql;
            AttachParameters(command, command.CommandText, parameters);
            OracleDataAdapter oracleDataAdapter = new OracleDataAdapter(command);
            DataSet dataSet = new DataSet();
            oracleDataAdapter.Fill(dataSet);
            oracleDataAdapter.Dispose();
            command.Dispose();
            return dataSet.Tables[0];
        }

        private OracleParameterCollection AttachParameters(OracleCommand cmd, string commandText, object[] paramList)
        {
            if (paramList == null || paramList.Length == 0)
                return null;
            OracleParameterCollection parameters = cmd.Parameters;
            MatchCollection matchCollection = new Regex("(:)\\S*(.*?)\\b", RegexOptions.IgnoreCase)
                .Matches(commandText.Substring(commandText.IndexOf(":", StringComparison.Ordinal))
                    .Replace(",", " ,"));
            if (paramList.Length!= matchCollection.Count)
                throw new SystemException("oracle parameter length error");
            string[] strArray = new string[matchCollection.Count];
            int index1 = 0;
            foreach (Match match in matchCollection)
            {
                strArray[index1] = match.Value;
                ++index1;
            }
            int index2 = 0;
            foreach (object obj in paramList)
            {
                OracleParameter oracleParameter = new OracleParameter();
                if (paramList[index2] == null)
                {
                    oracleParameter.ParameterName = strArray[index2];
                    parameters.Add(oracleParameter);
                    continue;
                }
                Type type = obj.GetType();
                switch (type.ToString())
                {
                    case "DBNull":
                    case "Char":
                    case "SByte":
                    case "UInt16":
                    case "UInt32":
                    case "UInt64":
                        throw new SystemException("Invalid data type");
                    case "System.String":
                        oracleParameter.DbType = DbType.String;
                        oracleParameter.ParameterName = strArray[index2];
                        oracleParameter.Value = (string)paramList[index2];
                        parameters.Add(oracleParameter);
                        break;
                    case "System.Byte[]":
                        oracleParameter.DbType = DbType.Binary;
                        oracleParameter.ParameterName = strArray[index2];
                        oracleParameter.Value = (byte[])paramList[index2];
                        parameters.Add(oracleParameter);
                        break;
                    case "System.Int32":
                        oracleParameter.DbType = DbType.Int32;
                        oracleParameter.ParameterName = strArray[index2];
                        oracleParameter.Value = (int)paramList[index2];
                        parameters.Add(oracleParameter);
                        break;
                    case "System.Int64":
                        oracleParameter.DbType = DbType.Int32;
                        oracleParameter.ParameterName = strArray[index2];
                        oracleParameter.Value = Convert.ToInt32(paramList[index2]);
                        parameters.Add(oracleParameter);
                        break;
                    case "System.Boolean":
                        oracleParameter.DbType = DbType.Boolean;
                        oracleParameter.ParameterName = strArray[index2];
                        oracleParameter.Value = (bool)paramList[index2];
                        parameters.Add(oracleParameter);
                        break;
                    case "System.DateTime":
                        oracleParameter.DbType = DbType.DateTime;
                        oracleParameter.ParameterName = strArray[index2];
                        oracleParameter.Value = Convert.ToDateTime(paramList[index2]);
                        parameters.Add(oracleParameter);
                        break;
                    case "System.Double":
                        oracleParameter.DbType = DbType.Double;
                        oracleParameter.ParameterName = strArray[index2];
                        oracleParameter.Value = Convert.ToDouble(paramList[index2]);
                        parameters.Add(oracleParameter);
                        break;
                    case "System.Decimal":
                        oracleParameter.DbType = DbType.Decimal;
                        oracleParameter.ParameterName = strArray[index2];
                        oracleParameter.Value = Convert.ToDecimal(paramList[index2]);
                        break;
                    case "System.Guid":
                        oracleParameter.DbType = DbType.Guid;
                        oracleParameter.ParameterName = strArray[index2];
                        oracleParameter.Value = (Guid)paramList[index2];
                        break;
                    case "System.Object":
                        oracleParameter.DbType = DbType.Object;
                        oracleParameter.ParameterName = strArray[index2];
                        oracleParameter.Value = paramList[index2];
                        parameters.Add(oracleParameter);
                        break;
                    default:
                        throw new SystemException("Value is of unknown data type");
                }
                ++index2;
            }
            return parameters;
        }

        private void CheckConnection()
        {
            if (Conn.State != ConnectionState.Closed)
                return;
            Conn.Open();
        }
    }
}

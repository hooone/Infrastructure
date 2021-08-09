using Autofac;
using FlowEngine.Model;
using Infrastructure.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace FlowEngine.Command
{
    public interface ISqlExecutePayload
    {
        string DbName { get; set; }
        string Sql { get; set; }
        int SqlExecuteResult { get; set; }
        Dictionary<string, object> ObjectList { get; set; }
    }
    public class SqlExecuteCommand<T> : NoBranchBaseCommand<T> where T : ISqlExecutePayload, new()
    {
        public override string Name { get; set; } = "执行sql";
        public override bool CustomAble { get { return true; } }

        private SqlHelper helper = null;

        public override bool Execute(T payload)
        {
            // 加载helper
            if (helper == null)
            {
                if (string.IsNullOrWhiteSpace(payload.DbName))
                    helper = Launcher.Container.ResolveNamed<SqlHelper>(payload.DbName);
                else
                    helper = Launcher.Container.Resolve<SqlHelper>();
            }
            if (helper == null)
                throw new Exception("SQL Helper not exist: " + payload.DbName);
            // 执行sql
            List<object> pms = new List<object>();
            Dictionary<string, object> objs = payload.ObjectList;
            if (payload.Sql.IndexOf("@", StringComparison.Ordinal) >= 0)
            {
                MatchCollection matchCollection = new Regex(@"(@)\S*(.*?)\b", RegexOptions.IgnoreCase)
             .Matches(payload.Sql.Substring(payload.Sql.IndexOf("@", StringComparison.Ordinal))
                 .Replace(",", " ,"));
                foreach (Match match in matchCollection)
                {
                    string key = match.Value.Replace("@", "");
                    if (objs.ContainsKey(key))
                    {
                        pms.Add(objs[key]);
                    }
                    else
                    {
                        pms.Add(null);
                    }
                }
            }
            payload.SqlExecuteResult = helper.ExecuteNonQuery(payload.Sql, pms.ToArray());
            if (base.Post != null)
                base.Post.SetSignal();
            return true;
        }

        public override T UnBoxing(Dictionary<string, object> context)
        {
            T payload = new T();
            payload.ObjectList = new Dictionary<string, object>();
            // 解析payload
            foreach (var prop in Properties)
            {
                if (prop.DefaultName.Equals(nameof(ISqlExecutePayload.DbName), StringComparison.CurrentCultureIgnoreCase))
                {
                    if (context.ContainsKey(prop.Name))
                    {
                        payload.DbName = (string)context[prop.Name];
                    }
                }
                else if (prop.DefaultName.Equals(nameof(ISqlExecutePayload.Sql), StringComparison.CurrentCultureIgnoreCase))
                {
                    if (context.ContainsKey(prop.Name))
                    {
                        payload.Sql = (string)context[prop.Name];
                    }
                }
                else if (prop.DefaultName.Equals(nameof(ISqlExecutePayload.SqlExecuteResult), StringComparison.CurrentCultureIgnoreCase))
                {
                    continue;
                }
                else
                {
                    if (context.ContainsKey(prop.Name))
                    {
                        payload.ObjectList.Add(prop.Name, context[prop.Name]);
                    }
                }
            }
            return payload;
        }
        public override void Boxing(Dictionary<string, object> context, T payload)
        {
            foreach (var prop in Properties)
            {
                if (prop.DefaultName.Equals(nameof(ISqlExecutePayload.SqlExecuteResult), StringComparison.CurrentCultureIgnoreCase))
                {
                    if (context.ContainsKey(prop.Name))
                    {
                        context[prop.Name] = payload.SqlExecuteResult;
                    }
                    else
                    {
                        context.Add(prop.Name, payload.SqlExecuteResult);
                    }
                }
            }
        }

        public override List<PropertyModel> GetProperties()
        {
            List<PropertyModel> result = new List<PropertyModel>();
            PropertyModel db = new PropertyModel();
            db.Name = nameof(ISqlExecutePayload.DbName);
            db.DefaultName = nameof(ISqlExecutePayload.DbName);
            db.Operation = OperationType.InputValue;
            db.DataType = Model.DataType.STRING;
            db.Description = "数据库名";
            db.IsCustom = false;
            db.Value = "ORACLE";
            result.Add(db);

            PropertyModel sql = new PropertyModel();
            sql.Name = nameof(ISqlExecutePayload.Sql);
            sql.DefaultName = nameof(ISqlExecutePayload.Sql);
            sql.Operation = OperationType.InputValue;
            sql.DataType = Model.DataType.STRING;
            sql.Description = "要执行的Sql语句";
            sql.IsCustom = false;
            sql.Value = "";
            result.Add(sql);

            PropertyModel sqlrst = new PropertyModel();
            sqlrst.Name = nameof(ISqlExecutePayload.SqlExecuteResult);
            sqlrst.DefaultName = nameof(ISqlExecutePayload.SqlExecuteResult);
            sqlrst.Operation = OperationType.ResultValue;
            sqlrst.DataType = Model.DataType.STRING;
            sqlrst.Description = "sql执行结果";
            sqlrst.IsCustom = false;
            sqlrst.Value = "";
            result.Add(sqlrst);
            return result;
        }
    }
}

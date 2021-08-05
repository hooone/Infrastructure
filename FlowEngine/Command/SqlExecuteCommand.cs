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
    public class SqlExecuteCommand<T> : ICommand<T> where T : ISqlExecutePayload, new()
    {
        private SqlHelper helper = null;

        public bool Execute(T payload)
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
                return false;
            // 执行sql
            Dictionary<string, object> objs = payload.ObjectList;
            List<object> pms = new List<object>();
            MatchCollection matchCollection = new Regex(@"(@)\S*(.*?)\b", RegexOptions.IgnoreCase)
         .Matches(payload.Sql.Substring(payload.Sql.IndexOf("@", StringComparison.Ordinal))
             .Replace(",", " ,"));
            foreach (Match match in matchCollection)
            {
                if (objs.ContainsKey(match.Value))
                {
                    pms.Add(objs[match.Value]);
                }
                else
                {
                    pms.Add(null);
                }
            }
            payload.SqlExecuteResult = helper.ExecuteNonQuery(payload.Sql);
            return true;
        }

        public T UnBoxing(List<PropertyModel> props, Dictionary<string, object> payloads)
        {
            T t = new T();
            t.ObjectList = new Dictionary<string, object>();
            // 解析payload
            foreach (var prop in props)
            {
                if (prop.DefaultName.Equals(nameof(ISqlExecutePayload.DbName), StringComparison.CurrentCultureIgnoreCase))
                {
                    if (payloads.ContainsKey(prop.Name))
                    {
                        t.DbName = (string)payloads[prop.Name];
                    }
                }
                else if (prop.DefaultName.Equals(nameof(ISqlExecutePayload.Sql), StringComparison.CurrentCultureIgnoreCase))
                {
                    if (payloads.ContainsKey(prop.Name))
                    {
                        t.Sql = (string)payloads[prop.Name];
                    }
                }
                else
                {
                    if (payloads.ContainsKey(prop.Name))
                    {
                        t.ObjectList.Add(prop.Name, payloads[prop.Name]);
                    }
                }
            }
            return t;
        }
        public void Boxing(List<PropertyModel> props, T payload, Dictionary<string, object> payloads)
        {
        }
        public string Id { get; set; }
        public string Name { get; set; }
        public Precondition Pre { get; set; }
        public Postcondition Post { get; set; }
        public List<Postcondition> GetPostcondition()
        {
            return new List<Postcondition>() { Post };
        }

        public List<Precondition> GetPrecondition()
        {
            return new List<Precondition>() { Pre };
        }

        //internal static ICommand NewCommand()
        //{
        //    SqlExecuteCommand rst = new SqlExecuteCommand();
        //    rst.Id = Guid.NewGuid().ToString("N");
        //    rst.Name = "执行sql";
        //    rst.Pre = new Precondition();
        //    rst.Pre.Id = Guid.NewGuid().ToString("N");
        //    rst.Pre.Seq = 1;
        //    rst.Pre.CommandId = rst.Id;
        //    rst.Post = new Postcondition();
        //    rst.Post.Id = Guid.NewGuid().ToString("N");
        //    rst.Post.Seq = 2;
        //    rst.Post.CommandId = rst.Id;
        //    return rst;
        //}

        public List<PropertyModel> GetProperties()
        {
            List<PropertyModel> result = new List<PropertyModel>();
            PropertyModel db = new PropertyModel();
            db.Name = nameof(ISqlExecutePayload.DbName);
            db.DefaultName = nameof(ISqlExecutePayload.DbName);
            db.Condition = 0;
            db.DataType = Model.DataType.STRING;
            db.Description = "数据库名";
            db.IsCustom = false;
            db.Value = "ORACLE";
            result.Add(db);

            PropertyModel sql = new PropertyModel();
            sql.Name = nameof(ISqlExecutePayload.Sql);
            sql.DefaultName = nameof(ISqlExecutePayload.Sql);
            sql.Condition = 0;
            sql.DataType = Model.DataType.STRING;
            sql.Description = "要执行的Sql语句";
            sql.IsCustom = false;
            sql.Value = "";
            result.Add(sql);
            return result;
        }
    }
}

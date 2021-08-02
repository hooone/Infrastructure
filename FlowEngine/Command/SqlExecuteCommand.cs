using Autofac;
using Infrastructure.DB;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace FlowEngine.Command
{
    public interface ISqlExecutePayload
    {
        int SqlExecuteResult { get; set; }
        Dictionary<string, object> ObjectList();
    }
    public class SqlExecuteCommand : ICommand
    {
        public string Id { get; set; }
        public string Name { get; set; }

        private SqlHelper helper = null;

        public string DbName { get; set; }
        public string Sql { get; set; }

        public bool Init()
        {
            if (helper == null)
            {
                if (string.IsNullOrWhiteSpace(DbName))
                    helper = Launcher.Container.ResolveNamed<SqlHelper>(DbName);
                else
                    helper = Launcher.Container.Resolve<SqlHelper>();
            }
            return true;
        }

        public bool Execute(ISqlExecutePayload payload)
        {
            Dictionary<string, object> objs = payload.ObjectList();
            List<object> pms = new List<object>();
            MatchCollection matchCollection = new Regex(@"(@)\S*(.*?)\b", RegexOptions.IgnoreCase)
         .Matches(this.Sql.Substring(this.Sql.IndexOf("@", StringComparison.Ordinal))
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
            payload.SqlExecuteResult = helper.ExecuteNonQuery(this.Sql);
            return true;
        }

        public List<Postcondition> GetPostcondition()
        {
            return null;
        }

        public List<Precondition> GetPrecondition()
        {
            return null;
        }
    }
}

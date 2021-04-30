using Demo.DAL;
using Demo.Model;
using Infrastructure.Code;
using Infrastructure.DB;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
namespace Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            SqlHelper hlp = new OracleHelper();
            hlp.Connect(new COracleParameter().ConnectionString);
            ActionInfoDAL dal = new ActionInfoDAL(hlp);
            ActionInfo action = new ActionInfo();
            action.CREATE_TIME = DateTime.Now;
            action.ACTION_CODE = "1";
            action.DEST_DEVICE = "dest";
            dal.insert(action);
            action.DEST_DEVICE = "22";
            dal.update(action);
            var lst = dal.read();
            dal.delete(action);
        }
    }
}

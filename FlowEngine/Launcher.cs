using Autofac;
using FlowEngine.DAL;
using Infrastructure.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowEngine
{
    public class Launcher
    {
        public static IContainer Container = null;
        public static void InitAutoFac()
        {
            var builder = new ContainerBuilder();
            // DB
            builder.RegisterType<OracleHelper>().Named<SqlHelper>("ORACLE").As<SqlHelper>().SingleInstance();
            // DAL
            builder.RegisterType<DAL.LinkDAL>().SingleInstance();
            builder.RegisterType<DAL.NodeDAL>().SingleInstance();
            builder.RegisterType<DAL.PointDAL>().SingleInstance();
            builder.RegisterType<DAL.PropertyDAL>().SingleInstance();
            // Service
            builder.RegisterType<FlowConfigService>().SingleInstance();
            builder.RegisterType<UnitTestRuntimeService>().InstancePerDependency();
            Container = builder.Build();

        }

        public static void InitDatabase()
        {
            // 打开数据库
            var a = Container.ResolveNamed<SqlHelper>("ORACLE");
            a.Connect(new COracleParameter().ConnectionString);
        }
    }
}

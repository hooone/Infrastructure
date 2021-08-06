using FlowEngine.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowEngine.Command
{
    public abstract class BaseCommand<T>
    {
        public string Id { get; set; }
        public abstract string Name { get; set; }
        public Precondition PreCondition { get; set; }
        public CommandState CommandState { get; set; }
        public List<PropertyModel> Properties { get; set; }

        T payload = default(T);
        public void Run(Dictionary<string, object> context)
        {
            if (CommandState == CommandState.Wait)
            {
                if (PreCondition.IsReady())
                    CommandState = CommandState.Ready;
            }
            if (CommandState == CommandState.Ready)
            {
                payload = UnBoxing(context);
                CommandState = CommandState.Running;
            }
            if (CommandState == CommandState.Running)
            {
                var rst = Execute(payload);
                if (rst)
                {
                    Boxing(context, payload);
                    CommandState = CommandState.Complete;
                }
            }
        }
        /// <summary>
        /// 执行具体的业务代码，执行完毕返回true，需要再次调用返回false，出错抛异常
        /// </summary>
        public abstract bool Execute(T payload);
        public abstract void Boxing(Dictionary<string, object> context, T payload);
        public abstract T UnBoxing(Dictionary<string, object> context);
        public abstract List<ConditionModel> GetConditions();
        public abstract List<PropertyModel> GetProperties();
        public abstract void RegisterLink(List<LinkViewModel> links);
    }
}

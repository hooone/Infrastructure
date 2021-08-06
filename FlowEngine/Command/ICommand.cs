using FlowEngine.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowEngine.Command
{
    public interface ICommand<T>
    {
        string Id { get; set; }
        string Name { get; set; }
        CommandState CommandState { get; set; }
        List<PropertyModel> Properties { get; set; }
        /// <summary>
        /// 执行Command
        /// </summary>
        bool Execute(T payload);

        /// <summary>
        /// 从全流程的变量集中拆卸得到当前Command所需的变量
        /// </summary>
        T UnBoxing(Dictionary<string, object> context);

        /// <summary>
        /// 将当前Command的变量装箱到全流程的变量集中
        /// </summary>
        void Boxing(Dictionary<string, object> context, T payload);

        List<ConditionModel> GetConditions();
        List<PropertyModel> GetProperties();
    }
    public enum CommandState
    {
        Wait,
        Ready,
        Running,
        Complete,
        Error,
    }
}

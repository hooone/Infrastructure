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
        List<PropertyModel> Properties { get; set; }
        /// <summary>
        /// 从全流程的变量集中拆卸得到当前Command所需的变量
        /// </summary>
        T UnBoxing(Dictionary<string, object> context);
        /// <summary>
        /// 将当前Command的变量装箱到全流程的变量集中
        /// </summary>
        void Boxing(Dictionary<string, object> context, T payload);
        bool Execute(T payload);
        List<Precondition> GetPrecondition();
        List<Postcondition> GetPostcondition();
        List<PropertyModel> GetProperties();
    }
    public class Precondition
    {
        public string Id { get; set; }
        public int Seq { get; set; }
        public string CommandId { get; set; }
    }
    public class Postcondition
    {
        public string Id { get; set; }
        public int Seq { get; set; }
        public string CommandId { get; set; }
    }
}

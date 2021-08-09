using Autofac;
using FlowEngine.Model;
using Infrastructure.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace FlowEngine.Command
{
    public interface IDelayPayload
    {
        int MilliSecond { get; set; }
    }
    public class DelayCommand<T> : NoBranchBaseCommand<T> where T : IDelayPayload, new()
    {
        public override string Name { get; set; } = "延迟";

        public override bool CustomAble { get { return false; } }

        private long start = 0;
        public override bool Execute(T payload)
        {
            if (start == 0)
            {
                start = DateTime.Now.Ticks;
                return false;
            }
            if (TimeSpan.FromTicks(DateTime.Now.Ticks - start).TotalMilliseconds >= payload.MilliSecond)
            {
                Post.SetSignal();
                return true;
            }
            return false;
        }

        public override T UnBoxing(Dictionary<string, object> context)
        {
            T payload = new T();
            // 解析payload
            foreach (var prop in Properties)
            {
                if (prop.DefaultName.Equals(nameof(IDelayPayload.MilliSecond), StringComparison.CurrentCultureIgnoreCase))
                {
                    if (context.ContainsKey(prop.Name))
                    {
                        payload.MilliSecond = (int)context[prop.Name];
                    }
                }
            }
            return payload;
        }
        public override void Boxing(Dictionary<string, object> context, T payload)
        {
        }

        public override List<PropertyModel> GetProperties()
        {
            List<PropertyModel> result = new List<PropertyModel>();
            PropertyModel ms = new PropertyModel();
            ms.Name = nameof(IDelayPayload.MilliSecond);
            ms.DefaultName = nameof(IDelayPayload.MilliSecond);
            ms.Operation = OperationType.InputValue;
            ms.DataType = Model.DataType.NUMBER;
            ms.Description = "延时时长（毫秒）";
            ms.IsCustom = false;
            ms.Value = "100";
            result.Add(ms);
            return result;
        }
    }
}

using FlowEngine.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowEngine
{
    public class FlowConfigService
    {
        private static FlowConfigService _ins;
        public static FlowConfigService Instance
        {
            get
            {
                if (_ins != null)
                    return _ins;
                _ins = new FlowConfigService();
                return _ins;
            }
        }
        public FlowConfig GetFlowConfig()
        {
            FlowConfig rst = new FlowConfig();
            rst.Nodes = new List<NodeProperty>();
            return rst;
        }
    }
}

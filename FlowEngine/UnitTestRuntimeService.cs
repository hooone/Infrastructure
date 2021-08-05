using FlowEngine.Command;
using FlowEngine.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowEngine
{
    public class UnitTestRuntimeService
    {
        private readonly FlowConfigService flowConfig;
        private readonly PropertyDAL propertyDAL = null;
        public UnitTestRuntimeService(FlowConfigService _config, PropertyDAL propertyDAL)
        {
            this.flowConfig = _config;
            this.propertyDAL = propertyDAL;
        }
        ICommand command = null;
        Dictionary<string, object> payload = new Dictionary<string, object>();
        public List<ICommand> Init()
        {
            var flows = flowConfig.GetFlowConfig();
            // 初始化message
            foreach (var node in flows.Nodes)
            {
                propertyDAL.ReadByNode(new DTO.PropertyDTO() { NODEID = node.Id });
            }
            // 初始化赋值
            // 初始化command
            List<ICommand> result = new List<ICommand>();
            foreach (var node in flows.Nodes)
            {
                ICommand command = flowConfig.GetCommand(node.Type);
                command.Id = node.Id;
                command.Name = node.Text;
                result.Add(command);
            }
            // 初始化流程信号量
            return result;
        }
        public void Run()
        {
        }
    }
}

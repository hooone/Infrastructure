using FlowEngine.Command;
using FlowEngine.DAL;
using FlowEngine.Model;
using Infrastructure.DB;
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
        ICommand<TestTotalPayload> command = null;
        public bool Init(string nodeId)
        {
            // 初始化全局context
            InitContext();
            // 初始化command
            NodeViewModel node = flowConfig.GetNodeInfo(nodeId);
            command = flowConfig.GetCommand(node.Type);
            command.Id = node.Id;
            command.Name = node.Text;
            command.Properties = node.Properties;
            return true;
        }
        public Dictionary<string, object> context = new Dictionary<string, object>();
        public bool InitContext()
        {
            var flows = flowConfig.GetFlowConfig();
            // 初始化payload
            foreach (var node in flows.Nodes)
            {
                var props = propertyDAL.ReadByNode(new DTO.PropertyDTO() { NODEID = node.Id });
                foreach (var prop in props)
                {
                    object value = null;
                    Model.DataType typ = (Model.DataType)Enum.Parse(typeof(Model.DataType), prop.DATATYPE);
                    // 直接填值
                    if (prop.CONDITION == 0)
                    {
                        switch (typ)
                        {
                            case Model.DataType.STRING:
                                value = prop.VALUE.TryToString();
                                break;
                            case Model.DataType.NUMBER:
                                value = prop.VALUE.TryToInt();
                                break;
                            case Model.DataType.FLOAT:
                                value = prop.VALUE.TryToFloat();
                                break;
                            case Model.DataType.DATE:
                                value = prop.VALUE.TryToDateTime();
                                break;
                            default:
                                break;
                        }
                    }
                    // 采用类型默认值
                    else
                    {
                        switch (typ)
                        {
                            case Model.DataType.STRING:
                                value = "";
                                break;
                            case Model.DataType.NUMBER:
                                value = 0;
                                break;
                            case Model.DataType.FLOAT:
                                value = 0f;
                                break;
                            case Model.DataType.DATE:
                                value = DateTime.Now;
                                break;
                            default:
                                break;
                        }
                    }
                    context.Add(prop.NAME, value);
                }
            }
            return true;
        }
        public bool Run()
        {
            if (command == null)
                return false;
            // 将context加载到command
            var payload = command.UnBoxing(context);
            var rst = command.Execute(payload);
            command.Boxing(context, payload);
            return rst;
        }
    }
}

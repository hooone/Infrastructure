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
        ICommand command = null;
        public bool Init(string nodeId)
        {
            // 初始化payload
            InitPayload();
            // 初始化command
            NodeViewModel node = flowConfig.GetNodeInfo(nodeId);
            command = flowConfig.GetCommand(node.Type);
            command.Id = node.Id;
            command.Name = node.Text;
            // 将payload加载到command
            command.Init()
            return true;
        }
        Dictionary<string, object> payload = new Dictionary<string, object>();
        public bool InitPayload()
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
                    payload.Add(prop.NAME, value);
                }
            }
            return true;
        }
        public void Run()
        {
            if (command == null)
                return;
        }
    }
}

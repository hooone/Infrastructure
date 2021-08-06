using AutoMapper;
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
        private readonly IMapper mapper;
        private readonly FlowConfigService flowConfig;
        private readonly PropertyDAL propertyDAL = null;
        public UnitTestRuntimeService(IMapper mapper, FlowConfigService _config, PropertyDAL propertyDAL)
        {
            this.mapper = mapper;
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
                foreach (var propdto in props)
                {
                    PropertyModel prop = mapper.Map<PropertyModel>(propdto);
                    object value = null;
                    // 直接填值
                    if (prop.Operation == OperationType.InputValue)
                    {
                        switch (prop.DataType)
                        {
                            case Model.DataType.STRING:
                                value = prop.Value.TryToString();
                                break;
                            case Model.DataType.NUMBER:
                                value = prop.Value.TryToInt();
                                break;
                            case Model.DataType.FLOAT:
                                value = prop.Value.TryToFloat();
                                break;
                            case Model.DataType.DATE:
                                value = prop.Value.TryToDateTime();
                                break;
                            default:
                                break;
                        }
                    }
                    // 采用类型默认值
                    else
                    {
                        switch (prop.DataType)
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
                    context.Add(prop.Name, value);
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

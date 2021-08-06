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
    public class RuntimeService
    {
        private readonly IMapper mapper;
        private readonly FlowConfigService flowConfig;
        private readonly NodeDAL nodeDAL = null;
        private readonly PropertyDAL propertyDAL = null;
        private readonly PointDAL pointDAL = null;
        private readonly LinkDAL linkDAL = null;
        public RuntimeService(IMapper mapper, FlowConfigService _config, NodeDAL nodeDAL, PropertyDAL propertyDAL, PointDAL pointDAL, LinkDAL linkDAL)
        {
            this.mapper = mapper;
            this.flowConfig = _config;
            this.nodeDAL = nodeDAL;
            this.propertyDAL = propertyDAL;
            this.pointDAL = pointDAL;
            this.linkDAL = linkDAL;
        }
        bool Working = false;
        List<BaseCommand<TestTotalPayload>> Commands = new List<BaseCommand<TestTotalPayload>>();
        public void Work()
        {
            //while (Working)
            {
                // 等待触发器
                // 重置数据上下文
                Reset();
                // 执行控制字段
                bool runningFlag = true;
                int lastWait = 0;
                int lastReady = 0;
                int lastRunning = 0;
                int lastComplete = 0;
                while (runningFlag)
                {
                    // 调用执行业务代码
                    foreach (var cmd in Commands)
                    {
                        cmd.Run(context);
                    }
                    // 判断执行完成
                    bool hasRunning = false;
                    int WaitCount = 0;
                    int ReadyCount = 0;
                    int RunningCount = 0;
                    int CompleteCount = 0;
                    foreach (var cmd in Commands)
                    {
                        switch (cmd.CommandState)
                        {
                            case CommandState.Wait:
                                WaitCount++;
                                break;
                            case CommandState.Running:
                                RunningCount++;
                                hasRunning = true;
                                break;
                            case CommandState.Complete:
                                CompleteCount++;
                                break;
                            case CommandState.Error:
                                runningFlag = false;
                                break;
                        }
                    }
                    if (!hasRunning && lastWait == WaitCount && lastReady == ReadyCount && lastRunning == RunningCount && lastComplete == CompleteCount)
                    {
                        break;
                    }
                    // 判断超时
                }
            }
        }
        public bool Reset()
        {
            var nodes = nodeDAL.ReadAll(null);
            // 初始化全局context
            InitContext(nodes);
            // 初始化command
            Commands = new List<BaseCommand<TestTotalPayload>>();
            foreach (var item in nodes)
            {
                NodeViewModel node = flowConfig.GetNodeInfo(item.ID, false);
                var command = flowConfig.GetCommand(node.Type);
                command.Id = node.Id;
                command.Name = node.Text;
                command.Properties = node.Properties;
                Commands.Add(command);
            }
            // 初始化转换条件
            foreach (var cmd in Commands)
            {
                var links = mapper.Map<List<LinkViewModel>>(linkDAL.ReadByFromNode(new DTO.LinkDTO() { FROMNODE = cmd.Id }));
                foreach (var item in links)
                {
                    var destCmd = Commands.FirstOrDefault(f => f.Id == item.ToNode);
                    if (destCmd != null)
                    {
                        item.DestCondition = destCmd.PreCondition;
                    }
                }
                cmd.RegisterLink(links);
            }
            return true;
        }

        private Dictionary<string, object> context = new Dictionary<string, object>();
        private bool InitContext(List<DTO.NodeDTO> nodes)
        {
            // 初始化payload
            foreach (var node in nodes)
            {
                var props = propertyDAL.ReadByNode(new DTO.PropertyDTO() { NODEID = node.ID });
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
    }
}

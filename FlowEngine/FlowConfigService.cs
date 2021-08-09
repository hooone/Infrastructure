using AutoMapper;
using FlowEngine.Command;
using FlowEngine.DAL;
using FlowEngine.Model;
using System;
using System.Collections.Generic;

namespace FlowEngine
{
    public class FlowConfigService
    {
        private readonly NodeDAL nodeDAL = null;
        private readonly LinkDAL linkDAL = null;
        private readonly PointDAL pointDAL = null;
        private readonly PropertyDAL propertyDAL = null;
        private readonly IMapper mapper = null;
        public FlowConfigService(IMapper mapper, NodeDAL nodeDAL, LinkDAL linkDAL, PointDAL pointDAL, PropertyDAL propertyDAL)
        {
            this.mapper = mapper;
            this.nodeDAL = nodeDAL;
            this.linkDAL = linkDAL;
            this.pointDAL = pointDAL;
            this.propertyDAL = propertyDAL;
        }

        #region 增
        /// <summary>
        /// 创建流程节点
        /// </summary>
        public NodeViewModel CreateNode(string type, int x, int y)
        {
            // 获取command
            BaseCommand<TestTotalPayload> cmd = GetCommand(type);
            cmd.Id = Guid.NewGuid().ToString("N");
            // 插入node表
            NodeViewModel node = new NodeViewModel();
            node.Id = cmd.Id;
            node.Type = type;
            node.Text = cmd.Name;
            node.X = x;
            node.Y = y;
            DTO.NodeDTO nd = mapper.Map<DTO.NodeDTO>(node);
            if (nodeDAL.insert(nd) != 1)
            {
                return null;
            }

            // 插入Point表
            node.Conditions = new List<ConditionModel>();
            var conds = cmd.GetConditions();
            foreach (var item in conds)
            {
                item.Id = Guid.NewGuid().ToString("N");
                item.NodeId = cmd.Id;
                DTO.PointDTO pt = mapper.Map<DTO.PointDTO>(item);
                if (pointDAL.insert(pt) != 1)
                {
                    return null;
                }
                node.Conditions.Add(item);
            }

            // 插入Property表
            var props = cmd.GetProperties();
            var curProps = propertyDAL.ReadAll(null);
            foreach (var item in props)
            {
                item.Id = Guid.NewGuid().ToString("N");
                item.NodeId = cmd.Id;
                // 避免Name重复 
                string name = item.Name;
                int idx = 1;
                while (curProps.Exists(f => f.NAME == name))
                {
                    name = item.Name + idx.ToString().PadLeft(2, '0');
                    idx++;
                }
                item.Name = name;
                DTO.PropertyDTO po = mapper.Map<DTO.PropertyDTO>(item);
                propertyDAL.insert(po);
            }
            return node;
        }

        /// <summary>
        /// 创建连接线
        /// </summary>
        public LinkViewModel CreateLine(string pointFrom, string pointTo)
        {
            var ps1 = pointDAL.ReadByID(new DTO.PointDTO { ID = pointFrom });
            if (ps1.Count != 1)
                return null;
            string node1 = ps1[0].NODEID;
            var ps2 = pointDAL.ReadByID(new DTO.PointDTO { ID = pointTo });
            if (ps2.Count != 1)
                return null;
            string node2 = ps2[0].NODEID;
            LinkViewModel lv = new LinkViewModel();
            lv.Id = Guid.NewGuid().ToString("N");
            lv.FromPoint = pointFrom;
            lv.ToPoint = pointTo;
            lv.FromNode = node1;
            lv.ToNode = node2;
            // 插入LINK表
            DTO.LinkDTO link = mapper.Map<DTO.LinkDTO>(lv);
            if (linkDAL.insert(link) != 1)
            {
                return null;
            }
            return lv;
        }

        /// <summary>
        /// 创建新的自定义属性
        /// </summary>
        /// <param name="nodeid"></param>
        /// <returns></returns>
        public PropertyModel CreateNodeProperty(string nodeid)
        {
            // 生成唯一属性名
            var all = propertyDAL.ReadAll(null);
            string name = "property";
            int idx = 1;
            while (all.Exists(f => f.NAME.Equals(name + idx, StringComparison.CurrentCultureIgnoreCase)))
            {
                idx++;
            }
            PropertyModel rst = new PropertyModel();
            rst.Id = Guid.NewGuid().ToString("N");
            rst.NodeId = nodeid;
            rst.Operation = OperationType.InputValue;
            rst.DataType = DataType.STRING;
            rst.IsCustom = true;
            rst.Description = "";
            rst.Name = name + idx;
            rst.Value = "";
            // 写入数据库
            DTO.PropertyDTO property = mapper.Map<DTO.PropertyDTO>(rst);
            if (propertyDAL.insert(property) == 0)
                return null;

            // 装箱
            return rst;
        }
        #endregion

        #region 删
        /// <summary>
        /// 删除流程节点
        /// </summary>
        public void DeleteNode(string id)
        {
            var lines = GetLinesByNode(id);
            foreach (var item in lines)
            {
                linkDAL.Delete(new DTO.LinkDTO() { ID = item.Id });
            }
            nodeDAL.Delete(new DTO.NodeDTO() { ID = id });
            pointDAL.DeleteByNode(new DTO.PointDTO() { NODEID = id });
            propertyDAL.DeleteByNode(new DTO.PropertyDTO() { NODEID = id });
        }

        /// <summary>
        /// 删除连接线
        /// </summary>
        public void DeleteLine(string id)
        {
            linkDAL.Delete(new DTO.LinkDTO() { ID = id });
        }
        #endregion

        #region 改
        /// <summary>
        ///  更改节点属性
        /// </summary>
        public int UpdateProperty(string nodeId, string propertyId, string name, string type, int operation, string value, string description)
        {
            if (nodeId == propertyId)
            {
                // 更改文本描述
                return nodeDAL.UpdateText(new DTO.NodeDTO() { ID = nodeId, TEXT = value });
            }
            // 数字验证
            if (type == DataType.NUMBER.ToString())
            {
                if (!int.TryParse(value, out _))
                {
                    return -2;
                }
            }
            else if (type == DataType.FLOAT.ToString())
            {
                if (!float.TryParse(value, out _))
                {
                    return -2;
                }
            }
            else if (type == DataType.DATE.ToString())
            {
                if (!DateTime.TryParse(value, out DateTime dt))
                {
                    return -2;
                }
                value = dt.ToString("yyyy-MM-dd HH:mm:ss");
            }
            // 重名验证
            var allProps = propertyDAL.ReadAll(null);
            if (allProps.Exists(f => f.NAME.Equals(name, StringComparison.CurrentCultureIgnoreCase) && f.ID != propertyId))
            {
                return -1;
            }
            // 指向校验
            if (operation == 1)
            {
                if (!allProps.Exists(f => f.NAME.Equals(value, StringComparison.CurrentCultureIgnoreCase)))
                {
                    return -2;
                }
            }
            // 更改属性
            return propertyDAL.Update(new DTO.PropertyDTO()
            {
                ID = propertyId,
                NAME = name,
                OPERATION = ((OperationType)operation).ToString(),
                VALUE = value,
                DATATYPE = type,
                DESCRIPTION = description,
            });
        }

        /// <summary>
        /// 更改节点的位置
        /// </summary>
        public int UpdateNodeLocation(string id, int x, int y)
        {
            DTO.NodeDTO node = new DTO.NodeDTO();
            node.ID = id;
            node.X = x;
            node.Y = y;
            return nodeDAL.UpdateLocation(node);
        }
        #endregion

        #region 查
        /// <summary>
        /// 查询除属性外的完整数据，用于绘制流程图
        /// </summary>
        public FlowConfig GetFlowConfig()
        {
            FlowConfig rst = new FlowConfig();
            rst.Nodes = new List<NodeViewModel>();
            rst.Links = new List<LinkViewModel>();
            var nodes = nodeDAL.ReadAll(null);
            foreach (DTO.NodeDTO nd in nodes)
            {
                NodeViewModel node = mapper.Map<NodeViewModel>(nd);
                node.Conditions = new List<ConditionModel>();
                var points = pointDAL.ReadByNode(new DTO.PointDTO() { NODEID = nd.ID });
                foreach (var pt in points)
                {
                    var cond = mapper.Map<ConditionModel>(pt);
                    node.Conditions.Add(cond);
                }
                rst.Nodes.Add(node);
            }
            var links = linkDAL.read(null);
            foreach (var lk in links)
            {
                LinkViewModel link = mapper.Map<LinkViewModel>(lk);
                rst.Links.Add(link);
            }
            return rst;
        }

        /// <summary>
        /// 查询指定流程节点的详细信息
        /// </summary>
        public NodeViewModel GetNodeInfo(string nodeid, bool needText)
        {
            var nodes = nodeDAL.ReadById(new DTO.NodeDTO() { ID = nodeid });
            if (nodes.Count != 1)
                return null;
            NodeViewModel node = mapper.Map<NodeViewModel>(nodes[0]);
            // 读取Point表
            node.Conditions = new List<ConditionModel>();
            var points = pointDAL.ReadByNode(new DTO.PointDTO() { NODEID = node.Id });
            foreach (var item in points)
            {
                var cond = mapper.Map<ConditionModel>(item);
                node.Conditions.Add(cond);
            }
            // 读取Property表
            node.Properties = new List<PropertyModel>();
            if (needText)
            {
                PropertyModel pt = mapper.Map<PropertyModel>(node);
                node.Properties.Add(pt);
            }
            var ps = propertyDAL.ReadByNode(new DTO.PropertyDTO() { NODEID = node.Id });
            foreach (var item in ps)
            {
                PropertyModel p = mapper.Map<PropertyModel>(item);
                node.Properties.Add(p);
            }
            return node;
        }

        /// <summary>
        /// 查询一个属性的详细信息，用于在属性修改窗体上显示
        /// </summary>
        public PropertyModel GetProperty(string propertyId)
        {
            var props = propertyDAL.ReadById(new DTO.PropertyDTO() { ID = propertyId });
            if (props.Count == 0)
            {
                // 将节点的Text字段封装成一个Property
                var nodes = nodeDAL.ReadById(new DTO.NodeDTO() { ID = propertyId });
                if (nodes.Count == 0)
                {
                    return null;
                }
                PropertyModel pt = mapper.Map<PropertyModel>(nodes[0]);
                return pt;
            }
            PropertyModel prop = mapper.Map<PropertyModel>(props[0]);
            return prop;
        }

        /// <summary>
        /// 查询节点相关的连接线，用于流程节点删除的同时删除连接线
        /// </summary>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        public List<LinkViewModel> GetLinesByNode(string nodeId)
        {
            List<LinkViewModel> rst = new List<LinkViewModel>();
            var points = pointDAL.ReadByNode(new DTO.PointDTO() { NODEID = nodeId });
            foreach (var item in points)
            {
                var fms = linkDAL.ReadByFrom(new DTO.LinkDTO() { FROMPOINT = item.ID });
                foreach (var lk in fms)
                {
                    var p = mapper.Map<LinkViewModel>(lk);
                    rst.Add(p);
                }
                var tos = linkDAL.ReadByTo(new DTO.LinkDTO() { TOPOINT = item.ID });
                foreach (var lk in tos)
                {
                    var p = mapper.Map<LinkViewModel>(lk);
                    rst.Add(p);
                }
            }
            return rst;
        }
        /// <summary>
        /// 获得所有信号量连接点，用于流程界面删除时界面清空连接点缓存
        /// </summary>
        /// <param name="nodeid"></param>
        /// <returns></returns>
        public List<string> GetPointsByNode(string nodeid)
        {
            List<string> rst = new List<string>();
            var pts = pointDAL.ReadByNode(new DTO.PointDTO() { NODEID = nodeid });
            foreach (var item in pts)
            {
                rst.Add(item.ID);
            }
            return rst;
        }

        #endregion

        public BaseCommand<TestTotalPayload> GetCommand(string type)
        {
            switch (type.ToUpper())
            {
                case "SQLEXECUTE":
                    return new SqlExecuteCommand<TestTotalPayload>();
                    //case "INJECT":
                    //    return InjectCommand.NewCommand();
                    //default:
                    //    return CommonCommand.NewCommand();
            }
            return null;
        }

    }
}

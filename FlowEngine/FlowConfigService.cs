﻿using FlowEngine.Command;
using FlowEngine.DAL;
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
        private readonly NodeDAL nodeDAL = null;
        private readonly LinkDAL linkDAL = null;
        private readonly PointDAL pointDAL = null;
        private readonly PropertyDAL propertyDAL = null;
        public FlowConfigService(NodeDAL nodeDAL, LinkDAL linkDAL, PointDAL pointDAL, PropertyDAL propertyDAL)
        {
            this.nodeDAL = nodeDAL;
            this.linkDAL = linkDAL;
            this.pointDAL = pointDAL;
            this.propertyDAL = propertyDAL;
        }
        public FlowConfig GetFlowConfig()
        {
            FlowConfig rst = new FlowConfig();
            rst.Nodes = new List<NodeViewModel>();
            rst.Links = new List<LinkViewModel>();
            var nodes = nodeDAL.read(null);
            foreach (var nd in nodes)
            {
                NodeViewModel node = new NodeViewModel();
                node.Id = nd.ID;
                node.Type = nd.TYPE;
                node.Text = nd.TEXT;
                node.X = nd.X;
                node.Y = nd.Y;
                node.Points = new Dictionary<string, int>();
                var points = pointDAL.ReadByNode(new DTO.Point() { NODEID = nd.ID });
                foreach (var pt in points)
                {
                    node.Points.Add(pt.ID, pt.SEQ);
                }
                rst.Nodes.Add(node);
            }
            var links = linkDAL.read(null);
            foreach (var lk in links)
            {
                LinkViewModel link = new LinkViewModel();
                link.Id = lk.ID;
                link.From = lk.LINKFROM;
                link.To = lk.LINKTO;
                rst.Links.Add(link);
            }
            return rst;
        }

        public int UpdateNodeLocation(string id, int x, int y)
        {
            DTO.Node node = new DTO.Node();
            node.ID = id;
            node.X = x;
            node.Y = y;
            return nodeDAL.UpdateLocation(node);
        }

        public NodeViewModel CreateNode(string type, int x, int y)
        {
            // 获取command
            ICommand cmd = GetCommand(type);
            // 插入node表
            DTO.Node nd = new DTO.Node();
            nd.ID = cmd.Id;
            nd.TYPE = type;
            nd.TEXT = cmd.Name;
            nd.X = x;
            nd.Y = y;
            if (nodeDAL.insert(nd) != 1)
            {
                return null;
            }
            // 插入Point表
            var pres = cmd.GetPrecondition();
            foreach (var item in pres)
            {
                DTO.Point pt = new DTO.Point();
                pt.ID = item.Id;
                pt.NODEID = item.CommandId;
                pt.SEQ = item.Seq;
                if (pointDAL.insert(pt) != 1)
                {
                    return null;
                }
            }
            var posts = cmd.GetPostcondition();
            foreach (var item in posts)
            {
                DTO.Point pt = new DTO.Point();
                pt.ID = item.Id;
                pt.NODEID = item.CommandId;
                pt.SEQ = item.Seq;
                if (pointDAL.insert(pt) != 1)
                {
                    return null;
                }
            }
            // 装箱
            NodeViewModel node = new NodeViewModel();
            node.Id = nd.ID;
            node.Type = nd.TYPE;
            node.Text = nd.TEXT;
            node.X = nd.X;
            node.Y = nd.Y;
            node.Points = new Dictionary<string, int>();
            foreach (var item in pres)
            {
                node.Points.Add(item.Id, item.Seq);
            }
            foreach (var item in posts)
            {
                node.Points.Add(item.Id, item.Seq);
            }
            return node;
        }

        public void DeleteNode(string id)
        {
            var lines = GetLinesByNode(id);
            foreach (var item in lines)
            {
                linkDAL.Delete(new DTO.Link() { ID = item.Id });
            }
            nodeDAL.Delete(new DTO.Node() { ID = id });
            pointDAL.DeleteByNode(new DTO.Point() { NODEID = id });
        }

        public NodeViewModel GetNodeInfo(string id)
        {
            var nodes = nodeDAL.ReadById(new DTO.Node() { ID = id });
            if (nodes.Count != 1)
                return null;
            // 装箱
            var nd = nodes[0];
            NodeViewModel node = new NodeViewModel();
            node.Id = nd.ID;
            node.Type = nd.TYPE;
            node.Text = nd.TEXT;
            node.X = nd.X;
            node.Y = nd.Y;
            // 读取Point表
            node.Points = new Dictionary<string, int>();
            var points = pointDAL.ReadByNode(new DTO.Point() { NODEID = node.Id });
            foreach (var item in points)
            {
                node.Points.Add(item.ID, item.SEQ);
            }
            // 读取Property表
            node.Properties = new List<PropertyViewModel>();
            PropertyViewModel pt = new PropertyViewModel();
            pt.Id = node.Id;
            pt.Name = "文本";
            pt.Value = node.Text;
            pt.Condition = 0;
            node.Properties.Add(pt);
            var ps = propertyDAL.ReadByNode(new DTO.PropertyDTO() { NODEID = node.Id });
            foreach (var item in ps)
            {
                PropertyViewModel p = new PropertyViewModel();
                p.Id = item.ID;
                p.Name = item.NAME;
                p.Value = item.VALUE;
                p.Condition = item.CONDITION;
                p.Description = item.DESCRIPTION;
                node.Properties.Add(p);
            }
            return node;
        }

        public int UpdateNodeText(string id, string text)
        {
            DTO.Node node = new DTO.Node();
            node.ID = id;
            node.TEXT = text;
            return nodeDAL.UpdateText(node);
        }
        public ICommand GetCommand(string type)
        {
            switch (type.ToUpper())
            {
                case "SQLEXECUTE":
                    return SqlExecuteCommand.NewCommand();
                case "INJECT":
                    return InjectCommand.NewCommand();
                default:
                    return CommonCommand.NewCommand();
            }
        }

        public LinkViewModel CreateLine(string point1, string point2)
        {
            DTO.Link link = new DTO.Link();
            link.ID = Guid.NewGuid().ToString("N");
            link.LINKFROM = point1;
            link.LINKTO = point2;
            if (linkDAL.insert(link) != 1)
            {
                return null;
            }
            LinkViewModel lv = new LinkViewModel();
            lv.Id = link.ID;
            lv.From = link.LINKFROM;
            lv.To = link.LINKTO;
            return lv;
        }

        public void DeleteLine(string id)
        {
            linkDAL.Delete(new DTO.Link() { ID = id });
        }

        public List<LinkViewModel> GetLinesByNode(string nodeId)
        {
            List<LinkViewModel> rst = new List<LinkViewModel>();
            var points = pointDAL.ReadByNode(new DTO.Point() { NODEID = nodeId });
            foreach (var item in points)
            {
                var fms = linkDAL.ReadByFrom(new DTO.Link() { LINKFROM = item.ID });
                foreach (var lk in fms)
                {
                    var p = new LinkViewModel();
                    p.Id = lk.ID;
                    p.From = lk.LINKFROM;
                    p.To = lk.LINKTO;
                    rst.Add(p);
                }
                var tos = linkDAL.ReadByTo(new DTO.Link() { LINKTO = item.ID });
                foreach (var lk in tos)
                {
                    var p = new LinkViewModel();
                    p.Id = lk.ID;
                    p.From = lk.LINKFROM;
                    p.To = lk.LINKTO;
                    rst.Add(p);
                }
            }
            return rst;
        }

        public List<string> GetPointsByNode(string nodeid)
        {
            List<string> rst = new List<string>();
            var pts = pointDAL.ReadByNode(new DTO.Point() { NODEID = nodeid });
            foreach (var item in pts)
            {
                rst.Add(item.ID);
            }
            return rst;
        }

        public PropertyViewModel CreateNodeProperty(string nodeid)
        {
            // 生成唯一属性名
            var all = propertyDAL.ReadAll(null);
            string name = "property";
            int idx = 1;
            while (all.Exists(f => f.NAME.Equals(name + idx, StringComparison.CurrentCultureIgnoreCase)))
            {
                idx++;
            }

            // 写入数据库
            DTO.PropertyDTO property = new DTO.PropertyDTO();
            property.ID = Guid.NewGuid().ToString("N");
            property.NODEID = nodeid;
            property.NAME = name + idx;
            property.VALUE = "";
            property.CONDITION = 0;
            if (propertyDAL.insert(property) == 0)
                return null;

            // 装箱
            PropertyViewModel rst = new PropertyViewModel();
            rst.Id = property.ID;
            rst.Condition = property.CONDITION;
            rst.Description = property.DESCRIPTION;
            rst.Name = property.NAME;
            rst.Value = property.VALUE;
            return rst;
        }
    }
}

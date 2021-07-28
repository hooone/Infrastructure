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
        public FlowConfigService(NodeDAL nodeDAL, LinkDAL linkDAL, PointDAL pointDAL)
        {
            this.nodeDAL = nodeDAL;
            this.linkDAL = linkDAL;
            this.pointDAL = pointDAL;
        }
        public FlowConfig GetFlowConfig()
        {
            FlowConfig rst = new FlowConfig();
            rst.Nodes = new List<NodeViewModel>();
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
                rst.Nodes.Add(node);
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
            // 插入node表
            DTO.Node nd = new DTO.Node();
            nd.ID = Guid.NewGuid().ToString("N");
            nd.TYPE = type;
            nd.TEXT = "新节点";
            nd.X = x;
            nd.Y = y;
            if (nodeDAL.insert(nd) != 1)
            {
                return null;
            }
            // 装箱
            NodeViewModel node = new NodeViewModel();
            node.Id = nd.ID;
            node.Type = nd.TYPE;
            node.Text = nd.TEXT;
            node.X = nd.X;
            node.Y = nd.Y;
            node.Points = new Dictionary<string, int>();
            return node;
        }

        public void DeleteNode(string id)
        {
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
            node.Points = new Dictionary<string, int>();
            return node;
        }

        public int UpdateNodeText(string id, string text)
        {
            DTO.Node node = new DTO.Node();
            node.ID = id;
            node.TEXT = text;
            return nodeDAL.UpdateText(node);
        }
    }
}

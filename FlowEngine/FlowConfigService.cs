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
            rst.Nodes = new List<NodeProperty>();
            var nodes = nodeDAL.read();
            foreach (var nd in nodes)
            {
                NodeProperty node = new NodeProperty();
                node.Id = nd.ID;
                node.Text = nd.TEXT;
                node.X = nd.X;
                node.Y = nd.Y;
                node.LinkIn = new Dictionary<string, int>();
                node.LinkOut = new Dictionary<string, int>();
                rst.Nodes.Add(node);
            }
            return rst;
        }
    }
}

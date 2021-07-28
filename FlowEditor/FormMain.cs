using Autofac;
using FlowEditor.Nodes;
using FlowEngine;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FlowEditor
{
    public partial class FormMain : Form
    {
        private readonly FlowConfigService service = null;
        Dictionary<string, Node> nodes = new Dictionary<string, Node>();
        Dictionary<string, Node> point_nodes = new Dictionary<string, Node>();
        public FormMain()
        {
            InitializeComponent();
            service = Launcher.Container.Resolve<FlowConfigService>();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            LoadFlow();
        }
        private void LoadFlow()
        {
            // 清空现有数据
            this.canvas.Controls.Clear();
            this.canvas.HorizontalScroll.Value = 0;
            this.canvas.VerticalScroll.Value = 0;
            nodes.Clear();
            point_nodes.Clear();

            // 读取配置文件
            var config = service.GetFlowConfig();

            // 添加流程节点
            foreach (var prop in config.Nodes)
            {
                CreateNode(prop.Id, prop.Text, prop.X, prop.Y, prop.Points);
            }
        }
        #region 添加流程节点
        private void CreateNode(string id, string text, int x, int y, Dictionary<string, int> linkPoint)
        {
            Node node = new OtherNode();
            node.Id = id;
            node.SetText(text);
            node.Location = new Point(x, y);
            node.DragEnd += Node_DragEnd;
            this.canvas.Controls.Add(node);
            nodes.Add(id, node);
            if (linkPoint != null)
            {
                foreach (var p in linkPoint)
                {
                    node.SetPointId(p.Value, p.Key);
                }
            }
            foreach (var item in node.GetPoints())
            {
                point_nodes.Add(item, node);
            }
        }

        private void Node_DragEnd(Node node)
        {
            MoveNode(node.Id, node.Location.X, node.Location.Y);
        }
        #endregion

        #region 修改流程节点
        private void MoveNode(string id, int x, int y)
        {
            x += this.canvas.HorizontalScroll.Value;
            y += this.canvas.VerticalScroll.Value;
            service.UpdateLocation(id, x, y);
        }
        #endregion

        private void FormMain_Load(object sender, EventArgs e)
        {
            this.LoadFlow();
        }
    }
}

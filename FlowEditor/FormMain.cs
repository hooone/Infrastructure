using Autofac;
using FlowEditor.Nodes;
using FlowEngine;
using FlowEngine.Model;
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
            this.otherNode1.Type = "OTHER";
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            this.LoadFlow();
            InitDragCreateNode();
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
                CreateNode(prop.Id, prop.Type, prop.Text, prop.X, prop.Y, prop.Points);
            }
        }
        #region 流程选择
        private void button1_Click(object sender, EventArgs e)
        {
            LoadFlow();
        }
        #endregion

        #region 添加流程节点
        // 添加节点到canvas
        private void CreateNode(string id, string type, string text, int x, int y, Dictionary<string, int> linkPoint)
        {
            Node node = new OtherNode();
            node.Id = id;
            node.Type = type;
            node.SetText(text);
            node.Location = new Point(x - this.canvas.HorizontalScroll.Value, y - this.canvas.VerticalScroll.Value);
            node.DragEnd += NodeMove_DragEnd;
            node.Click += Node_Click;
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

        // 工具栏页面切换
        private void TabControl1_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            InitDragCreateNode();
        }

        // 向工具栏添加假控件
        private List<Node> drags = new List<Node>();
        private void InitDragCreateNode()
        {
            var page = this.tabControl1.TabPages[this.tabControl1.SelectedIndex];
            // 移除原有假控件
            foreach (var item in drags)
            {
                this.Controls.Remove(item);
            }
            drags.Clear();
            // 插入假控件
            foreach (Node node in page.Controls)
            {
                node.SetDragEnable(false);
                var dragNode = (Node)Activator.CreateInstance(node.GetType());
                dragNode.BackColor = node.BackColor;
                dragNode.Cursor = System.Windows.Forms.Cursors.Hand;
                dragNode.Type = node.Type;
                dragNode.Location = new Point(this.tabControl1.Left + page.Left + node.Location.X, this.tabControl1.Top + page.Top + node.Location.Y);
                dragNode.Size = node.Size;
                dragNode.DragStart += DragNode_DragStart;
                dragNode.DragEnd += DragNode_DragEnd;
                this.Controls.Add(dragNode);
                drags.Add(dragNode);
                dragNode.BringToFront();
            }
        }

        // 拖动生成结束
        private void DragNode_DragEnd(Node node)
        {
            this.Controls.Remove(node);
            this.drags.Remove(node);
            if (node.Location.X > this.canvas.Location.X
                && this.Location.X < this.canvas.Location.X + this.canvas.Width
                && node.Location.Y > this.canvas.Location.Y
                && this.Location.Y < this.canvas.Location.Y + this.canvas.Height)
            {
                // 生成新控件
                NodeProperty prop = service.CreateNode(node.Type,
                    node.Location.X - this.canvas.Location.X + this.canvas.HorizontalScroll.Value,
                    node.Location.Y - this.canvas.Location.Y + this.canvas.VerticalScroll.Value);
                if (prop == null)
                    return;
                CreateNode(prop.Id, prop.Type, prop.Text, prop.X, prop.Y, prop.Points);
            }
        }

        // 拖动生成开始
        private void DragNode_DragStart(Node node)
        {
            var page = this.tabControl1.TabPages[this.tabControl1.SelectedIndex];
            var dragNode = (Node)Activator.CreateInstance(node.GetType());
            dragNode.BackColor = node.BackColor;
            dragNode.Type = node.Type;
            dragNode.Cursor = System.Windows.Forms.Cursors.Hand;
            dragNode.Location = new Point(node.Location.X, node.Location.Y);
            dragNode.Size = node.Size;
            dragNode.DragStart += DragNode_DragStart;
            dragNode.DragEnd += DragNode_DragEnd;
            this.Controls.Add(dragNode);
            drags.Add(dragNode);
            dragNode.BringToFront();
        }

        #endregion

        #region 修改流程节点
        // 拖动完成
        private void NodeMove_DragEnd(Node node)
        {
            var x = node.Location.X + this.canvas.HorizontalScroll.Value;
            var y = node.Location.Y + this.canvas.VerticalScroll.Value;
            service.UpdateLocation(node.Id, x, y);
        }
        private Nodes.Node selectNode = null;
        private void Node_Click(object sender, EventArgs e)
        {
            if (selectNode != null)
            {
                selectNode.HighLight(false);
                selectNode = null;
            }
            var node = (Nodes.Node)sender;
            node.HighLight(true);
            selectNode = node;
        }
        // 删除节点
        private void NodeDelete()
        {
            if (selectNode != null)
            {
                this.canvas.Controls.Remove(selectNode);
                nodes.Remove(selectNode.Id);
                service.DeleteNode(selectNode.Id);
            }
        }
        #endregion

        // 删除键
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Delete)
            {
                NodeDelete();
            }
            return true;
        }
    }
}

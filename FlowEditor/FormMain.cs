using Autofac;
using FlowEditor.Nodes;
using FlowEngine;
using FlowEngine.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
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
            this.canvas.MouseWheel += canvas_Scroll;
            this.canvas.ChangeUICues += canvas_Scroll;
            this.canvas.GotFocus += canvas_Scroll;
            this.canvas.Layout += canvas_Scroll;
            this.canvas.Leave += canvas_Scroll;
            this.canvas.Paint += canvas_Scroll;
        }
        private void FormMain_Load(object sender, EventArgs e)
        {
            this.LoadFlow();
            // 初始化工具栏
            InitDragToolsNode();
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
            // 添加线
            foreach (var item in config.Links)
            {
                AddLine(item.Id, item.To, item.From);
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
            node.DragStart += Node_DragStart;
            node.DragEnd += NodeMove_DragEnd;
            node.PointClickEvent += Node_PointClickEvent;
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
            InitDragToolsNode();
        }

        // 向工具栏添加假控件
        private List<Node> drags = new List<Node>();
        private void InitDragToolsNode()
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
                NodeViewModel prop = service.CreateNode(node.Type,
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
        // 拖动修改位置
        private void NodeMove_DragEnd(Node node)
        {
            var x = node.Location.X + this.canvas.HorizontalScroll.Value;
            var y = node.Location.Y + this.canvas.VerticalScroll.Value;
            service.UpdateNodeLocation(node.Id, x, y);
        }

        // 节点拖动开始时，重置所有线的位置
        private void Node_DragStart(Node control)
        {
            foreach (var item in nodes)
            {
                item.Value.ResetLine();
            }
        }
        // 选择节点
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
            var info = service.GetNodeInfo(node.Id);
            if (info == null)
                return;
            this.textBox1.Text = info.Text;
            // 清除线选择
            if (selectLine != null)
            {
                selectLine.BackColor = Color.FromArgb(144, 144, 144);
                selectLine = null;
            }
            // 清除连接点选择
            if (selectOutNode != null)
            {
                selectOutNode.HighLightPoint(selectOutId, false);
                selectOutNode = null;
            }
            if (selectInNode != null)
            {
                selectInNode.HighLightPoint(selectInId, false);
                selectInNode = null;
            }
        }

        // 保存属性修改
        private void savebtn_Click(object sender, EventArgs e)
        {
            if (selectNode == null)
                return;
            service.UpdateNodeText(selectNode.Id, this.textBox1.Text);
            var info = service.GetNodeInfo(selectNode.Id);
            if (info == null)
                return;
            this.textBox1.Text = info.Text;
            nodes[info.Id].SetText(info.Text);
        }
        // 删除节点
        private void NodeDelete()
        {
            if (selectNode != null)
            {
                // 移除节点
                this.canvas.Controls.Remove(selectNode);
                nodes.Remove(selectNode.Id);
                // 移除连接线
                var linedata = service.GetLinesByNode(selectNode.Id);
                foreach (var item in linedata)
                {
                    if (lines.ContainsKey(item.Id))
                    {
                        this.canvas.Controls.Remove(lines[item.Id]);
                        lines.Remove(item.Id);
                    }
                }
                // 移除point_nodes缓存
                var points = service.GetPointsByNode(selectNode.Id);
                foreach (var item in points)
                {
                    point_nodes.Remove(item);
                }
                // 删除数据库
                service.DeleteNode(selectNode.Id);
                selectNode = null;
            }
        }
        #endregion

        #region 添加连接线
        private Nodes.Node selectInNode = null;
        private string selectInId = "";
        private Nodes.Node selectOutNode = null;
        private string selectOutId = "";
        // 连接点选择
        private void Node_PointClickEvent(Nodes.Node node, bool isOut, string Id)
        {
            // 相同元素选择
            if (isOut && selectInNode == node)
            {
                if (selectInNode != null)
                {
                    selectInNode.HighLightPoint(selectInId, false);
                }
                selectInNode = null;
                if (selectOutNode != null)
                {
                    selectOutNode.HighLightPoint(selectOutId, false);
                }
                selectOutNode = node;
                selectOutId = Id;
                selectOutNode.HighLightPoint(selectOutId, true);
            }
            // 相同元素选择
            else if (!isOut && selectOutNode == node)
            {
                if (selectOutNode != null)
                {
                    selectOutNode.HighLightPoint(selectOutId, false);
                }
                selectOutNode = null;
                if (selectInNode != null)
                {
                    selectInNode.HighLightPoint(selectInId, false);
                }
                selectInNode = node;
                selectInId = Id;
                selectInNode.HighLightPoint(selectInId, true);

            }
            // 连线
            else if (isOut && selectInNode != null)
            {
                // 写入
                selectOutId = Id;
                selectOutNode = node;
                // 添加线
                CreateLine(selectInId, selectOutId);
                // 复位
                if (selectOutNode != null)
                {
                    selectOutNode.HighLightPoint(selectOutId, false);
                    selectOutNode = null;
                }
                if (selectInNode != null)
                {
                    selectInNode.HighLightPoint(selectInId, false);
                    selectInNode = null;
                }
            }
            // 连线
            else if (!isOut && selectOutNode != null)
            {
                // 写入
                selectInId = Id;
                selectInNode = node;
                // 添加线
                CreateLine(selectInId, selectOutId);
                // 复位
                if (selectOutNode != null)
                {
                    selectOutNode.HighLightPoint(selectOutId, false);
                    selectOutNode = null;
                }
                if (selectInNode != null)
                {
                    selectInNode.HighLightPoint(selectInId, false);
                    selectInNode = null;
                }
            }
            // 选择
            else if (isOut)
            {
                if (selectOutNode != null)
                {
                    selectOutNode.HighLightPoint(selectOutId, false);
                }
                selectOutNode = node;
                selectOutId = Id;
                selectOutNode.HighLightPoint(selectOutId, true);
            }
            // 选择
            else if (!isOut)
            {
                if (selectInNode != null)
                {
                    selectInNode.HighLightPoint(selectInId, false);
                }
                selectInNode = node;
                selectInId = Id;
                selectInNode.HighLightPoint(selectInId, true);
            }
            // 清除线选择
            if (selectLine != null)
            {
                selectLine.BackColor = Color.FromArgb(144, 144, 144);
                selectLine = null;
            }
            // 清除节点选择
            if (selectNode != null)
            {
                selectNode.HighLight(false);
                selectNode = null;
            }
        }

        private Dictionary<string, LinkLine> lines = new Dictionary<string, LinkLine>();
        // 添加连接线到canvas
        private void AddLine(string id, string point1, string point2)
        {
            if (string.IsNullOrWhiteSpace(point1) || string.IsNullOrWhiteSpace(point2))
                return;
            if (lines.ContainsKey(id))
                return;
            if (!point_nodes.ContainsKey(point1) || !point_nodes.ContainsKey(point2))
                return;
            LinkLine line = new LinkLine();
            line.Id = id;
            line.Click += Line_Click;
            this.canvas.Controls.Add(line);
            lines.Add(id, line);
            var from = point_nodes[point2];
            from.RegisterLine(point2, line);
            var to = point_nodes[point1];
            to.RegisterLine(point1, line);
        }

        // 创建连接线
        private void CreateLine(string point1, string point2)
        {
            var link = service.CreateLine(point1, point2);
            if (link == null)
                return;
            AddLine(link.Id, link.From, link.To);
        }


        #endregion

        #region 删除连接线
        private LinkLine selectLine = null;
        private void Line_Click(object sender, EventArgs e)
        {

            var line = (LinkLine)sender;
            bool select = (line.BackColor.R == line.BackColor.G && line.BackColor.G == 144);
            if (selectLine != null)
            {
                selectLine.BackColor = Color.FromArgb(144, 144, 144);
            }
            if (select)
            {
                line.BackColor = Color.FromArgb(0xFF, 0x7F, 0x0E);
                selectLine = line;
            }
            // 清除连接点选择
            if (selectOutNode != null)
            {
                selectOutNode.HighLightPoint(selectOutId, false);
                selectOutNode = null;
            }
            if (selectInNode != null)
            {
                selectInNode.HighLightPoint(selectInId, false);
                selectInNode = null;
            }
            // 清除节点选择
            if (selectNode != null)
            {
                selectNode.HighLight(false);
                selectNode = null;
            }
        }
        // 删除连接线
        private void LineDelete()
        {
            if (selectLine != null)
            {
                this.canvas.Controls.Remove(selectLine);
                lines.Remove(selectLine.Id);
                service.DeleteLine(selectLine.Id);
                selectLine = null;
            }
        }
        #endregion
        // 删除按键
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Delete)
            {
                NodeDelete();
                LineDelete();
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
        /// <summary>
        /// 水平滚动条
        /// </summary>
        public static int HScrollValue = 0;
        /// <summary>
        /// 竖直滚动条
        /// </summary>
        public static int VScrollValue = 0;
        private void canvas_Scroll(object sender, object e)
        {
            HScrollValue = this.canvas.HorizontalScroll.Value;
            VScrollValue = this.canvas.VerticalScroll.Value;
        }

    }
}

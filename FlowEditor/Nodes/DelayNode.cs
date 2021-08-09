using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

namespace FlowEditor.Nodes
{
    public partial class DelayNode : Node
    {
        private const int _Radius = 6;
        public delegate void NodeEvent(Control control);
        public DelayNode()
        {
            this.Type = "Delay";
            InitializeComponent();
        }
        #region 属性
        public override void SetText(string text)
        {
            int width = Math.Max(100, 40 + text.Length * 23);
            this.Width = width;
            this.Text = text;
            this.linkOutPoint1.Location = new Point(this.Width - 8, 11);
            this.Region = new System.Drawing.Region(Round(0, 0, this.Width, this.Height));
            this.Invalidate();
        }

        private string inPointId = Guid.NewGuid().ToString("N") + "aaaaa";
        private string outPointId = Guid.NewGuid().ToString("N") + "aaaaa";
        public override void SetPointId(int index, string pointId)
        {
            if (index == 1)
            {
                inPointId = pointId;
            }
            if (index == 2)
            {
                outPointId = pointId;
            }
        }
        public override List<string> GetPoints()
        {
            List<string> rst = new List<string>();
            rst.Add(inPointId);
            rst.Add(outPointId);
            return rst;
        }

        private List<LinkLine> InLines = new List<LinkLine>();
        private List<LinkLine> OutLines = new List<LinkLine>();
        public override void RegisterLine(string pointId, LinkLine line)
        {
            if (pointId == inPointId)
            {
                InLines.Add(line);
                line.SetEnd(this.Location.X + 4, this.Location.Y + this.Height / 2);
            }
            else if (pointId == outPointId)
            {
                OutLines.Add(line);
                line.SetStart(this.Location.X + this.Width - 4, this.Location.Y + this.Height / 2);
            }
        }
        public override void DeRegisterLine(LinkLine line)
        {
            InLines.Remove(line);
            OutLines.Remove(line);
        }

        #endregion

        #region 外观
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            //图标工作区
            Color i = Color.FromArgb(20, 0, 0, 0);
            Brush b = new SolidBrush(i);
            GraphicsPath IconRect = IconRound(3, 1, 30, this.Height - 1);
            e.Graphics.FillPath(b, IconRect);
            //边框颜色
            Color c = Color.FromArgb(144, 144, 144);
            Pen p = new Pen(c);
            p.DashStyle = DashStyle.Solid;
            p.Width = 1;
            GraphicsPath Rect = Round(3, 1, this.Width - 4, this.Height - 1);
            e.Graphics.DrawPath(p, Rect);
            Color c2 = Color.FromArgb(36, 0, 0, 0);
            Pen p2 = new Pen(c2);
            e.Graphics.DrawLine(p2, 30, 0, 30, this.Height);
            // 字体
            var font = new Font("楷体", 16);
            Color i2 = Color.FromArgb(0, 0, 0);
            Brush b2 = new SolidBrush(i2);
            e.Graphics.DrawString(this.Text, font, b2, 35, (this.Height - 20) / 2);
        }
        public GraphicsPath IconRound(int x, int y, int width, int height)
        {
            GraphicsPath oPath = new GraphicsPath();
            int thisWidth = width;
            int thisHeight = height;
            int angle = _Radius;
            oPath.AddArc(x, y, angle, angle, 180, 90);
            oPath.AddLine(x, y, width, 0);
            oPath.AddLine(width, 0, width, height);
            oPath.AddArc(x, thisHeight - angle, angle, angle, 90, 90);
            oPath.CloseAllFigures();
            return oPath;
        }
        public GraphicsPath Round(int x, int y, int width, int height)
        {
            GraphicsPath oPath = new GraphicsPath();
            int thisWidth = width;
            int thisHeight = height;
            int angle = _Radius;
            oPath.AddArc(x, y, angle, angle, 180, 90);
            oPath.AddArc(thisWidth - angle, y, angle, angle, 270, 90);
            oPath.AddArc(thisWidth - angle, thisHeight - angle, angle, angle, 0, 90);
            oPath.AddArc(x, thisHeight - angle, angle, angle, 90, 90);
            oPath.CloseAllFigures();
            return oPath;
        }
        #endregion

        #region 拖动
        private bool dragAble = true;
        bool MoveFlag = false;
        int xPos = 0;
        int yPos = 0;
        public override void SetDragEnable(bool enable)
        {
            dragAble = enable;
        }
        private void Node_MouseDown(object sender, MouseEventArgs e)
        {
            if (!dragAble)
                return;
            MoveFlag = true;//已经按下.
            xPos = e.X;//当前x坐标.
            yPos = e.Y;//当前y坐标.
            OnDragStart();
        }

        private void Node_MouseUp(object sender, MouseEventArgs e)
        {
            MoveFlag = false;
            if (this.Location.X < 0)
                this.Location = new Point(0, this.Location.Y);
            if (this.Location.Y < 0)
                this.Location = new Point(this.Location.X, 0);
            if (this.Location.X > 760 - this.Width)
                this.Location = new Point(760 - this.Width, this.Location.Y);
            if (this.Location.Y > 620 - this.Height)
                this.Location = new Point(this.Location.X, 620 - this.Height);
            foreach (var line in InLines)
            {
                line.SetEnd(this.Location.X + 4, this.Location.Y + this.Height / 2);
            }
            foreach (var line in OutLines)
            {
                line.SetStart(this.Location.X + this.Width - 4, this.Location.Y + this.Height / 2);
            }
            OnDragEnd();
        }
        private void Node_MouseMove(object sender, MouseEventArgs e)
        {
            if (MoveFlag)
            {
                var deltaX = Convert.ToInt16(e.X - xPos);//设置x坐标.
                var deltaY = Convert.ToInt16(e.Y - yPos);//设置y坐标.
                if (deltaX != 0 || deltaY != 0)
                {
                    this.Left += deltaX;
                    this.Top += deltaY;
                    foreach (var line in InLines)
                    {
                        line.SetEnd(this.Location.X + 4, this.Location.Y + this.Height / 2);
                    }
                    foreach (var line in OutLines)
                    {
                        line.SetStart(this.Location.X + this.Width - 4, this.Location.Y + this.Height / 2);
                    }
                }
                if (this.Parent.Name != "canvas")
                {
                    if (this.Location.X > 230 && this.Location.Y > 50
                        && this.Location.X < 230 + 760 && this.Location.Y < 50 + 620)
                    {
                        this.Cursor = Cursors.Arrow;
                    }
                    else
                    {
                        this.Cursor = Cursors.No;
                    }
                }
            }
        }
        public override void ResetLine()
        {
            foreach (var line in InLines)
            {
                line.SetEnd(this.Location.X + 4, this.Location.Y + this.Height / 2);
            }
            foreach (var line in OutLines)
            {
                line.SetStart(this.Location.X + this.Width - 4, this.Location.Y + this.Height / 2);
            }
        }
        #endregion

        #region 选中
        public override void HighLight(bool select)
        {
            if (!select)
                this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(216)))), ((int)(((byte)(191)))), ((int)(((byte)(216)))));
            else
                this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(246)))), ((int)(((byte)(215)))), ((int)(((byte)(246)))));
        }


        private void LinkOutPoint1_Click(object sender, System.EventArgs e)
        {
            if (sender == linkInPoint1)
            {
                OnPointClick(false, inPointId);
            }
            else if (sender == linkOutPoint1)
            {
                OnPointClick(true, outPointId);
            }
        }

        public override void HighLightPoint(string pointId, bool select)
        {
            Color c = select ? Color.Red : Color.FromArgb(217, 217, 217);
            if (pointId == inPointId)
            {
                linkInPoint1.BackColor = c;
            }
            if (pointId == outPointId)
            {
                linkOutPoint1.BackColor = c;
            }
        }

        #endregion
    }

}
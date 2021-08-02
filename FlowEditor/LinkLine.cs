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
using System.Diagnostics;

namespace FlowEditor
{
    public delegate void LineEvent(LinkLine line);
    public partial class LinkLine : Label
    {
        public LinkLine()
        {
            InitializeComponent();
            this.Location = new Point(0 - FormMain.HScrollValue, 0 - FormMain.VScrollValue);
            this.Region = new Region(Round(0, 0, 0, 0));
        }
        public string Id { get; set; }
        public GraphicsPath Round(int startX, int startY, int width, int height)
        {
            GraphicsPath oPath = new GraphicsPath();
            if (width == 0 && height == 0)
            {
                return oPath;
            }
            oPath.AddBezier(FormMain.HScrollValue + startX,
                FormMain.VScrollValue + startY,
                FormMain.HScrollValue + startX + 60,
                FormMain.VScrollValue + startY,
                FormMain.HScrollValue + startX + width - 60,
                FormMain.VScrollValue + startY + height,
                FormMain.HScrollValue + startX + width,
                FormMain.VScrollValue + startY + height);
            Pen p = new Pen(Color.Black, 3);
            oPath.Widen(p);
            return oPath;
        }

        private int startX = 0;
        private int startY = 0;
        private int endX = 0;
        private int endY = 0;
        internal void SetEnd(int x, int y)
        {
            endX = x;
            endY = y;
            Redraw();
        }

        internal void SetStart(int x, int y)
        {
            startX = x;
            startY = y;
            Redraw();
        }
        private void Redraw()
        {
            this.Region = new Region(Round(startX, startY, endX - startX, endY - startY));
        }
        //protected override System.Drawing.Point ScrollToControl(Control activeControl)
        //{
        //    return DisplayRectangle.Location;
        //}
    }
}

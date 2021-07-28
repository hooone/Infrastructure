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

namespace FlowEditor
{
    public partial class LinkLine : UserControl
    {
        public LinkLine()
        {
            InitializeComponent();
            this.Region = new Region(Round(0, 0, 0, 0));
        }
        public string Key = "";
        public GraphicsPath Round(int startX, int startY, int width, int height)
        {
            GraphicsPath oPath = new GraphicsPath();
            if (width == 0 && height == 0)
            {
                return oPath;
            }
            oPath.AddBezier(startX, startY, startX + 100, startY, startX + width - 100, startY + height, startX + width, startY + height);
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
    }
}

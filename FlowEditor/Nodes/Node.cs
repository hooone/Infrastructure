using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FlowEditor.Nodes
{
    public delegate void NodeEvent(Node control);
    public abstract class Node : UserControl
    {
        // 流程节点
        public string Id { get; set; }
        public abstract void SetText(string text);
        public abstract void HighLight(bool high);
        public abstract void SetDragEnable(bool enable);
        public event NodeEvent DragStart;
        public event NodeEvent DragEnd;
        public void OnDragStart()
        {
            DragStart?.Invoke(this);
        }
        public void OnDragEnd()
        {
            DragEnd?.Invoke(this);
        }
        // 连接点
        public abstract void SetPointId(int index, string pointId);
        public abstract List<string> GetPoints();
        public abstract void SelectPoint(string pointId, bool select);
        // 连接线
        public abstract void RegisterLine(string pointId, LinkLine line);
        public abstract void DeRegisterLine(LinkLine line);
    }
}

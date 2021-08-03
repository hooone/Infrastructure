using System.Windows.Forms;

namespace FlowEditor.Nodes
{
    partial class InjectNode
    {
        /// <summary> 
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region 组件设计器生成的代码

        /// <summary> 
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.linkInPoint1 = new LinkPoint();
            this.linkOutPoint1 = new LinkPoint();
            this.SuspendLayout();
            // 
            // linkInPoint1
            // 
            this.linkInPoint1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(217)))), ((int)(((byte)(217)))));
            this.linkInPoint1.Location = new System.Drawing.Point(0, 11);
            this.linkInPoint1.Name = "linkInPoint1";
            this.linkInPoint1.Size = new System.Drawing.Size(8, 8);
            this.linkInPoint1.Click += LinkOutPoint1_Click;
            this.linkInPoint1.TabIndex = 0;
            // 
            // linkOutPoint1
            // 
            this.linkOutPoint1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(217)))), ((int)(((byte)(217)))));
            this.linkOutPoint1.Location = new System.Drawing.Point(92, 11);
            this.linkOutPoint1.Name = "linkOutPoint1";
            this.linkOutPoint1.Size = new System.Drawing.Size(8, 8);
            this.linkOutPoint1.Click += LinkOutPoint1_Click;
            this.linkOutPoint1.TabIndex = 1;
            // 
            // InjectNode
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(166)))), ((int)(((byte)(187)))), ((int)(((byte)(207)))));
            this.Controls.Add(this.linkOutPoint1);
            this.Controls.Add(this.linkInPoint1);
            this.Name = "InjectNode";
            this.Text = "变量";
            this.Cursor = Cursors.Hand;
            this.Size = new System.Drawing.Size(100, 30);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.Node_MouseDown);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.Node_MouseMove);
            this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.Node_MouseUp);
            this.ResumeLayout(false);
        }

        #endregion
        private LinkPoint linkInPoint1;
        private LinkPoint linkOutPoint1;
    }
}

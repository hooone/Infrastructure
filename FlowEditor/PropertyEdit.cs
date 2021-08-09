using FlowEngine;
using FlowEngine.Model;
using System;
using System.Windows.Forms;

namespace FlowEditor
{
    public partial class PropertyEdit : Form
    {
        public string NodeId { get; set; }
        public string PropertyId { get; set; }
        public string PropName { get; private set; }
        public string Value { get; private set; }
        public string Description { get; private set; }

        private readonly FlowConfigService service;
        public PropertyEdit(FlowConfigService service, string propertyId)
        {
            this.service = service;
            this.PropertyId = propertyId;
            InitializeComponent();
        }

        private void PropertyEdit_Load(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(this.PropertyId))
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            }
            PropertyModel prop = service.GetProperty(this.PropertyId);
            this.textBox1.Text = prop.Name;
            this.textBox1.Enabled = prop.IsCustom;
            this.textBox2.Text = prop.Description;
            this.textBox2.Enabled = prop.IsCustom;
            this.textBox3.Text = prop.Value;
            this.comboBox1.SelectedIndex = (int)prop.Operation;
            this.comboBox2.SelectedIndex = (int)prop.DataType;
            this.comboBox2.Enabled = prop.IsCustom;
            this.Value = prop.Value;
            this.PropName = prop.Name;
            this.Description = prop.Description;
            this.NodeId = prop.NodeId;
            if (prop.Operation == OperationType.ResultValue)
            {
                this.comboBox1.Enabled = false;
                this.comboBox2.Enabled = false;
                this.textBox3.Enabled = false;
            }
            this.button3.Visible = prop.IsCustom;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var rst = service.UpdateProperty(this.NodeId, this.PropertyId, this.textBox1.Text, ((DataType)this.comboBox2.SelectedIndex).ToString(), this.comboBox1.SelectedIndex, this.textBox3.Text, this.textBox2.Text);
            if (rst == -1)
            {
                MessageBox.Show("属性名重复，保存失败。");
                return;
            }
            else if (rst == -2)
            {
                MessageBox.Show("输入的值无效，保存失败。");
                return;
            }
            this.PropName = this.textBox1.Text;
            this.Description = this.textBox2.Text;
            this.Value = this.textBox3.Text;
            this.DialogResult = DialogResult.OK;
            this.Close();

        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("确认删除该属性?", "确认", MessageBoxButtons.YesNo) != DialogResult.Yes)
                return;
            service.DeleteProperty(this.PropertyId);
            this.DialogResult = DialogResult.Retry;
            this.Close();
        }
    }
}

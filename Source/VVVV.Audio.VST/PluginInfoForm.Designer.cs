namespace VVVV.Audio.VST
{
    partial class PluginInfoForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.GroupBox groupBox1;
            System.Windows.Forms.GroupBox groupBox2;
            this.PluginPropertyListVw = new System.Windows.Forms.ListView();
            this.PropertyNameHdr = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.PropertyValueHdr = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.PluginParameterListVw = new System.Windows.Forms.ListView();
            this.ParameterNameHdr = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.ParameterValueHdr = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.ParameterLabelHdr = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.ParameterShortLabelHdr = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            groupBox1 = new System.Windows.Forms.GroupBox();
            groupBox2 = new System.Windows.Forms.GroupBox();
            groupBox1.SuspendLayout();
            groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(this.PluginPropertyListVw);
            groupBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            groupBox1.Location = new System.Drawing.Point(0, 0);
            groupBox1.Name = "groupBox1";
            groupBox1.Padding = new System.Windows.Forms.Padding(5);
            groupBox1.Size = new System.Drawing.Size(625, 233);
            groupBox1.TabIndex = 1;
            groupBox1.TabStop = false;
            groupBox1.Text = "Plugin Properties";
            // 
            // PluginPropertyListVw
            // 
            this.PluginPropertyListVw.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.PropertyNameHdr,
            this.PropertyValueHdr});
            this.PluginPropertyListVw.Dock = System.Windows.Forms.DockStyle.Fill;
            this.PluginPropertyListVw.FullRowSelect = true;
            this.PluginPropertyListVw.HideSelection = false;
            this.PluginPropertyListVw.Location = new System.Drawing.Point(5, 18);
            this.PluginPropertyListVw.MultiSelect = false;
            this.PluginPropertyListVw.Name = "PluginPropertyListVw";
            this.PluginPropertyListVw.Size = new System.Drawing.Size(615, 210);
            this.PluginPropertyListVw.TabIndex = 0;
            this.PluginPropertyListVw.UseCompatibleStateImageBehavior = false;
            this.PluginPropertyListVw.View = System.Windows.Forms.View.Details;
            // 
            // PropertyNameHdr
            // 
            this.PropertyNameHdr.Text = "Property Name";
            this.PropertyNameHdr.Width = 180;
            // 
            // PropertyValueHdr
            // 
            this.PropertyValueHdr.Text = "Property Value";
            this.PropertyValueHdr.Width = 180;
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(this.PluginParameterListVw);
            groupBox2.Dock = System.Windows.Forms.DockStyle.Fill;
            groupBox2.Location = new System.Drawing.Point(0, 0);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new System.Drawing.Size(625, 230);
            groupBox2.TabIndex = 2;
            groupBox2.TabStop = false;
            groupBox2.Text = "Programs && Parameters";
            // 
            // PluginParameterListVw
            // 
            this.PluginParameterListVw.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.PluginParameterListVw.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.ParameterNameHdr,
            this.ParameterValueHdr,
            this.ParameterLabelHdr,
            this.ParameterShortLabelHdr});
            this.PluginParameterListVw.FullRowSelect = true;
            this.PluginParameterListVw.HideSelection = false;
            this.PluginParameterListVw.Location = new System.Drawing.Point(7, 19);
            this.PluginParameterListVw.MultiSelect = false;
            this.PluginParameterListVw.Name = "PluginParameterListVw";
            this.PluginParameterListVw.Size = new System.Drawing.Size(612, 205);
            this.PluginParameterListVw.TabIndex = 2;
            this.PluginParameterListVw.UseCompatibleStateImageBehavior = false;
            this.PluginParameterListVw.View = System.Windows.Forms.View.Details;
            // 
            // ParameterNameHdr
            // 
            this.ParameterNameHdr.Text = "Parameter Name";
            this.ParameterNameHdr.Width = 119;
            // 
            // ParameterValueHdr
            // 
            this.ParameterValueHdr.Text = "Value";
            this.ParameterValueHdr.Width = 50;
            // 
            // ParameterLabelHdr
            // 
            this.ParameterLabelHdr.Text = "Label";
            this.ParameterLabelHdr.Width = 59;
            // 
            // ParameterShortLabelHdr
            // 
            this.ParameterShortLabelHdr.Text = "Short Lbl";
            this.ParameterShortLabelHdr.Width = 66;
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(groupBox1);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(groupBox2);
            this.splitContainer1.Size = new System.Drawing.Size(625, 467);
            this.splitContainer1.SplitterDistance = 233;
            this.splitContainer1.TabIndex = 2;
            // 
            // PluginInfoForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitContainer1);
            this.Name = "PluginInfoForm";
            this.Size = new System.Drawing.Size(625, 467);
            groupBox1.ResumeLayout(false);
            groupBox2.ResumeLayout(false);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.ListView PluginPropertyListVw;
        private System.Windows.Forms.ColumnHeader PropertyNameHdr;
        private System.Windows.Forms.ColumnHeader PropertyValueHdr;
        private System.Windows.Forms.ListView PluginParameterListVw;
        private System.Windows.Forms.ColumnHeader ParameterNameHdr;
        private System.Windows.Forms.ColumnHeader ParameterValueHdr;
        private System.Windows.Forms.ColumnHeader ParameterLabelHdr;
        private System.Windows.Forms.ColumnHeader ParameterShortLabelHdr;

    }
}
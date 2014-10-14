/*
 * Created by SharpDevelop.
 * User: Tebjan Halm
 * Date: 23.12.2013
 * Time: 02:44
 * 
 * 
 */
namespace VVVV.Nodes.Nodes.VST
{
	partial class VstPluginControl
	{
		/// <summary>
		/// Designer variable used to keep track of non-visual components.
		/// </summary>
		private System.ComponentModel.IContainer components = null;
		
		/// <summary>
		/// Disposes resources used by the control.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing) {
				if (components != null) {
					components.Dispose();
				}
			}
			base.Dispose(disposing);
		}
		
		/// <summary>
		/// This method is required for Windows Forms designer support.
		/// Do not change the method contents inside the source code editor. The Forms designer might
		/// not be able to load this method if it was changed manually.
		/// </summary>
		private void InitializeComponent()
		{
            this.FMainTableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.InfoButton = new System.Windows.Forms.Button();
            this.ProgramComboBox = new System.Windows.Forms.ComboBox();
            this.numericUpDown1 = new System.Windows.Forms.NumericUpDown();
            this.EditButton = new System.Windows.Forms.Button();
            this.CountLabel = new System.Windows.Forms.Label();
            this.PluginPanel = new System.Windows.Forms.Panel();
            this.ExposeButton = new System.Windows.Forms.Button();
            this.DeleteButton = new System.Windows.Forms.Button();
            this.LastParamLabel = new System.Windows.Forms.Label();
            this.FMainTableLayoutPanel1.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).BeginInit();
            this.SuspendLayout();
            // 
            // FMainTableLayoutPanel1
            // 
            this.FMainTableLayoutPanel1.BackColor = System.Drawing.SystemColors.ControlDark;
            this.FMainTableLayoutPanel1.ColumnCount = 1;
            this.FMainTableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.FMainTableLayoutPanel1.Controls.Add(this.tableLayoutPanel1, 0, 0);
            this.FMainTableLayoutPanel1.Controls.Add(this.PluginPanel, 0, 1);
            this.FMainTableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.FMainTableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.FMainTableLayoutPanel1.Margin = new System.Windows.Forms.Padding(0);
            this.FMainTableLayoutPanel1.Name = "FMainTableLayoutPanel1";
            this.FMainTableLayoutPanel1.RowCount = 2;
            this.FMainTableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 27F));
            this.FMainTableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.FMainTableLayoutPanel1.Size = new System.Drawing.Size(737, 438);
            this.FMainTableLayoutPanel1.TabIndex = 0;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 10;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 25F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 60F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 60F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 160F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 10F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 10F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 60F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 60F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 252F));
            this.tableLayoutPanel1.Controls.Add(this.InfoButton, 3, 0);
            this.tableLayoutPanel1.Controls.Add(this.ProgramComboBox, 4, 0);
            this.tableLayoutPanel1.Controls.Add(this.numericUpDown1, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.EditButton, 2, 0);
            this.tableLayoutPanel1.Controls.Add(this.CountLabel, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.ExposeButton, 7, 0);
            this.tableLayoutPanel1.Controls.Add(this.DeleteButton, 8, 0);
            this.tableLayoutPanel1.Controls.Add(this.LastParamLabel, 9, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(737, 27);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // InfoButton
            // 
            this.InfoButton.Dock = System.Windows.Forms.DockStyle.Fill;
            this.InfoButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.InfoButton.Location = new System.Drawing.Point(128, 3);
            this.InfoButton.Name = "InfoButton";
            this.InfoButton.Size = new System.Drawing.Size(54, 21);
            this.InfoButton.TabIndex = 2;
            this.InfoButton.Text = "Info";
            this.InfoButton.UseVisualStyleBackColor = true;
            this.InfoButton.Click += new System.EventHandler(this.InfoButton_Click);
            // 
            // ProgramComboBox
            // 
            this.ProgramComboBox.BackColor = System.Drawing.SystemColors.ControlDark;
            this.ProgramComboBox.DisplayMember = "Text";
            this.ProgramComboBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ProgramComboBox.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.ProgramComboBox.FormattingEnabled = true;
            this.ProgramComboBox.Location = new System.Drawing.Point(188, 3);
            this.ProgramComboBox.Name = "ProgramComboBox";
            this.ProgramComboBox.Size = new System.Drawing.Size(154, 21);
            this.ProgramComboBox.TabIndex = 1;
            this.ProgramComboBox.Text = "Program";
            this.ProgramComboBox.SelectedIndexChanged += new System.EventHandler(this.ProgramComboBox_SelectedIndexChanged);
            // 
            // numericUpDown1
            // 
            this.numericUpDown1.BackColor = System.Drawing.SystemColors.Control;
            this.numericUpDown1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.numericUpDown1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.numericUpDown1.Location = new System.Drawing.Point(28, 4);
            this.numericUpDown1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 3);
            this.numericUpDown1.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.numericUpDown1.Name = "numericUpDown1";
            this.numericUpDown1.Size = new System.Drawing.Size(34, 20);
            this.numericUpDown1.TabIndex = 0;
            this.numericUpDown1.ValueChanged += new System.EventHandler(this.numericUpDown1_ValueChanged);
            // 
            // EditButton
            // 
            this.EditButton.Dock = System.Windows.Forms.DockStyle.Fill;
            this.EditButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.EditButton.Location = new System.Drawing.Point(68, 3);
            this.EditButton.Name = "EditButton";
            this.EditButton.Size = new System.Drawing.Size(54, 21);
            this.EditButton.TabIndex = 1;
            this.EditButton.Text = "Edit";
            this.EditButton.UseVisualStyleBackColor = true;
            this.EditButton.Click += new System.EventHandler(this.EditButton_Click);
            // 
            // CountLabel
            // 
            this.CountLabel.AutoSize = true;
            this.CountLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.CountLabel.Location = new System.Drawing.Point(3, 7);
            this.CountLabel.Margin = new System.Windows.Forms.Padding(3, 7, 3, 0);
            this.CountLabel.Name = "CountLabel";
            this.CountLabel.Size = new System.Drawing.Size(19, 20);
            this.CountLabel.TabIndex = 3;
            this.CountLabel.Text = "0";
            this.CountLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // PluginPanel
            // 
            this.PluginPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.PluginPanel.Location = new System.Drawing.Point(0, 27);
            this.PluginPanel.Margin = new System.Windows.Forms.Padding(0);
            this.PluginPanel.Name = "PluginPanel";
            this.PluginPanel.Size = new System.Drawing.Size(737, 411);
            this.PluginPanel.TabIndex = 1;
            // 
            // ExposeButton
            // 
            this.ExposeButton.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ExposeButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.ExposeButton.Location = new System.Drawing.Point(368, 3);
            this.ExposeButton.Name = "ExposeButton";
            this.ExposeButton.Size = new System.Drawing.Size(54, 21);
            this.ExposeButton.TabIndex = 4;
            this.ExposeButton.Text = "Expose";
            this.ExposeButton.UseVisualStyleBackColor = true;
            this.ExposeButton.Click += new System.EventHandler(this.ExposeButton_Click);
            // 
            // DeleteButton
            // 
            this.DeleteButton.Dock = System.Windows.Forms.DockStyle.Fill;
            this.DeleteButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.DeleteButton.Location = new System.Drawing.Point(428, 3);
            this.DeleteButton.Name = "DeleteButton";
            this.DeleteButton.Size = new System.Drawing.Size(54, 21);
            this.DeleteButton.TabIndex = 5;
            this.DeleteButton.Text = "Delete";
            this.DeleteButton.UseVisualStyleBackColor = true;
            this.DeleteButton.Click += new System.EventHandler(this.DeleteButton_Click);
            // 
            // LastParamLabel
            // 
            this.LastParamLabel.AutoSize = true;
            this.LastParamLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.LastParamLabel.Location = new System.Drawing.Point(488, 7);
            this.LastParamLabel.Margin = new System.Windows.Forms.Padding(3, 7, 3, 0);
            this.LastParamLabel.Name = "LastParamLabel";
            this.LastParamLabel.Size = new System.Drawing.Size(246, 20);
            this.LastParamLabel.TabIndex = 6;
            this.LastParamLabel.Text = "LastParam";
            // 
            // VstPluginControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ControlDarkDark;
            this.Controls.Add(this.FMainTableLayoutPanel1);
            this.Name = "VstPluginControl";
            this.Size = new System.Drawing.Size(737, 438);
            this.FMainTableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).EndInit();
            this.ResumeLayout(false);

		}
        private System.Windows.Forms.TableLayoutPanel FMainTableLayoutPanel1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.NumericUpDown numericUpDown1;
        private System.Windows.Forms.Button InfoButton;
        private System.Windows.Forms.Button EditButton;
        private System.Windows.Forms.Panel PluginPanel;
        private System.Windows.Forms.ComboBox ProgramComboBox;
        private System.Windows.Forms.Label CountLabel;
        private System.Windows.Forms.Button ExposeButton;
        private System.Windows.Forms.Button DeleteButton;
        private System.Windows.Forms.Label LastParamLabel;
	}
}

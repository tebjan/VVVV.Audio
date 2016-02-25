namespace VVVV.Audio.VST
{
    partial class GenericUI
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.panel1 = new System.Windows.Forms.Panel();
            this.FPluginLabel = new System.Windows.Forms.Label();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.FPluginLabel);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(509, 32);
            this.panel1.TabIndex = 0;
            // 
            // FPluginLabel
            // 
            this.FPluginLabel.AutoSize = true;
            this.FPluginLabel.Dock = System.Windows.Forms.DockStyle.Top;
            this.FPluginLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold);
            this.FPluginLabel.Location = new System.Drawing.Point(0, 0);
            this.FPluginLabel.Name = "FPluginLabel";
            this.FPluginLabel.Padding = new System.Windows.Forms.Padding(5);
            this.FPluginLabel.Size = new System.Drawing.Size(63, 27);
            this.FPluginLabel.TabIndex = 0;
            this.FPluginLabel.Text = "Plugin";
            // 
            // GenericUI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ControlDark;
            this.Controls.Add(this.panel1);
            this.DoubleBuffered = true;
            this.Name = "GenericUI";
            this.Size = new System.Drawing.Size(509, 212);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label FPluginLabel;
    }
}

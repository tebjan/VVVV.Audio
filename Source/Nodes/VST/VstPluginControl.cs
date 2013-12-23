/*
 * Created by SharpDevelop.
 * User: Tebjan Halm
 * Date: 23.12.2013
 * Time: 02:44
 * 
 * 
 */
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using VVVV.Audio.VST;
using Jacobi.Vst.Core;
using Jacobi.Vst.Interop.Host;

namespace VVVV.Nodes.Nodes.VST
{
	/// <summary>
	/// Description of VstPluginControl.
	/// </summary>
	public partial class VstPluginControl : UserControl
	{
		public VstPluginControl(VSTHostNode node)
		{
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();
            Node = node;
			
			//
			// TODO: Add constructor code after the InitializeComponent() call.
			//
		}

        public VSTHostNode Node 
        { 
            get;
            protected set;
        }

        VSTSignal FSelectedSignal;
        
        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            FSelectedSignal = Node.GetPluginContext((int)((NumericUpDown)sender).Value);
        }

        private void EditButton_Click(object sender, EventArgs e)
        {
            SetEditor();
        }

        private VstPluginContext OpenContext;
        void SetEditor()
        {
            PluginPanel.Controls.Clear();
            OpenContext = FSelectedSignal.PluginContext;
            OpenContext.PluginCommandStub.EditorOpen(PluginPanel.Handle);
        }

        void SetInfo()
        {
            if (OpenContext != null)
            {
                OpenContext.PluginCommandStub.EditorClose();
                OpenContext = null;
            }

            PluginPanel.Controls.Clear();
            PluginPanel.Controls.Add(FSelectedSignal.InfoForm);
        }

        private void InfoButton_Click(object sender, EventArgs e)
        {
            SetInfo();
        }

	}
}

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

        //the current signal we look at
        VSTSignal FSelectedSignal;
        public VSTSignal SelectedSignal
        {
            get
            {
                return FSelectedSignal;
            }
            set
            {
                if (FSelectedSignal != value)
                {
                    FSelectedSignal = value;
                    LoadPrograms();
                    SetEditor();
                }
            }
        }

        private void LoadPrograms()
        {
            ProgramComboBox.Items.Clear();
            ProgramComboBox.Items.AddRange(FSelectedSignal.ProgramNames);
        }
        
        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            SelectedSignal = Node.GetPluginContext((int)((NumericUpDown)sender).Value);
            LoadPrograms();
            SetEditor();
        }

        //open editor
        private void EditButton_Click(object sender, EventArgs e)
        {
            SetEditor();
        }

        //open info
        private void InfoButton_Click(object sender, EventArgs e)
        {
            SetInfo();
        }

        private VstPluginContext OpenContext;
        void SetEditor()
        {
            if (OpenContext != FSelectedSignal.PluginContext)
            {
                ClearCurrentView();
                OpenContext = FSelectedSignal.PluginContext;
                OpenContext.PluginCommandStub.EditorOpen(PluginPanel.Handle);
            }
        }

        void SetInfo()
        {
            ClearCurrentView();
            PluginPanel.Controls.Add(FSelectedSignal.InfoForm);
        }

        //clear all displayed info or editor
        void ClearCurrentView()
        {
            if (OpenContext != null)
            {
                OpenContext.PluginCommandStub.EditorClose();
                OpenContext = null;
            }

            PluginPanel.Controls.Clear();
        }

        //select program
        private void ProgramComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            FSelectedSignal.PluginContext.PluginCommandStub.SetProgram(ProgramComboBox.SelectedIndex);
            FSelectedSignal.InfoForm.FillParameterList();
        }

        //set the count to display
        int FLastCount = 0;
        internal void SetSliceCount(int count)
        {
            if (FLastCount != count)
            {
                CountLabel.Text = count.ToString();
                FLastCount = count;
            }
        }
    }
}

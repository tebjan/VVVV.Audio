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
using System.Runtime.InteropServices;

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
                    if (FSelectedSignal != null)
                    {
                        FSelectedSignal.LastParamChangeInfo = null;
                        FSelectedSignal.PluginChanged = null;
                        FSelectedSignal.InfoFormParameterCheck -= FSelectedSignal_InfoFormParameterCheck;
                    }

                    FSelectedSignal = value;
                    FSelectedSignal.LastParamChangeInfo = DisplayLastParam;
                    FSelectedSignal.PluginChanged = () => SetEditor(true);
                    FSelectedSignal.InfoFormParameterCheck += FSelectedSignal_InfoFormParameterCheck;
                    LoadPrograms();
                    SetEditor();
                }
            }
        }

        private void FSelectedSignal_InfoFormParameterCheck(object sender, ItemCheckEventArgs e)
        {
            var expose = e.NewValue == CheckState.Checked;

            if(expose)
                Node.ExposePin(GetParamPinDefinitionString());
            else
                Node.RemovePin(GetParamPinDefinitionString());
        }
        

        private void DisplayLastParam(string value)
        {
            LastParamLabel.Text = value;
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
        protected bool FIsInfoDisplay;
        private void EditButton_Click(object sender, EventArgs e)
        {
            if(FIsInfoDisplay)
            {
                SetEditor();
                EditButton.Text = "Info";
            }
            else
            {
                SetInfo();
                EditButton.Text = "Editor";
            }
            
            FIsInfoDisplay = !FIsInfoDisplay;

        }

        //open info
        private void SaveButton_Click(object sender, EventArgs e)
        {
            SelectedSignal.NeedsSave = true;
        }

        private VstPluginContext OpenContext;
        private bool FHasEditor;
        void SetEditor(bool reset = false)
        {
            if (reset || OpenContext != FSelectedSignal.PluginContext)
            {
                ClearCurrentView();
                OpenContext = FSelectedSignal.PluginContext;
                if (HasEditor(OpenContext))
                {
                    OpenContext.PluginCommandStub.EditorOpen(PluginPanel.Handle);
                    FHasEditor = true;
                    if(FGenericUI != null)
                    {
                        PluginPanel.Controls.Remove(FGenericUI);
                        FGenericUI.Dispose();
                        FGenericUI = null;
                    }
                }
                else
                {
                    FGenericUI = new GenericUI(OpenContext);
                    FGenericUI.Dock = DockStyle.Fill;
                    PluginPanel.Controls.Add(FGenericUI);
                    FHasEditor = false;
                }
            }
        }


        public int EditorHandle => PluginPanel.Handle.ToInt32();

        private bool HasEditor(VstPluginContext ctx)
        {
            //if (ctx.PluginInfo.Flags.HasFlag(VstPluginFlags.HasEditor))
            //    return true;

            try
            {
                var rect = new Rectangle();
                ctx.PluginCommandStub.EditorGetRect(out rect);
                return rect.Width > 0;
            }
            catch (Exception)
            {

                return false;
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
        private GenericUI FGenericUI;

        internal void SetSliceCount(int count)
        {
            if (FLastCount != count)
            {
                CountLabel.Text = count.ToString();
                FLastCount = count;
            }
        }

        private string GetParamPinDefinitionString()
        {
            var ctx = FSelectedSignal.PluginContext;
            var paramIndex = FSelectedSignal.ParamIndex;
            var paramName = ctx.PluginCommandStub.GetParameterName(paramIndex);
            var pluginName = ctx.PluginCommandStub.GetEffectName();
            return paramIndex.ToString() + "|" + paramName + "|" + pluginName;
        }

        private void ExposeButton_Click(object sender, EventArgs e)
        {
            Node.ExposePin(GetParamPinDefinitionString());
        }

        private void DeleteButton_Click(object sender, EventArgs e)
        {
            Node.RemovePin(GetParamPinDefinitionString());
        }

        public void RefreshUI(int index)
        {
            if(FGenericUI != null)
            {
                FGenericUI.RefreshValue(index);
            }
        }
    }
}

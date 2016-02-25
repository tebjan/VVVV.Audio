using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Jacobi.Vst.Interop.Host;
using Jacobi.Vst.Core;
using VVVV.Utils.VMath;

namespace VVVV.Audio.VST
{
    public partial class GenericUI : UserControl
    {
        private VstPluginContext PluginContext;
        private List<TrackBar> PluginParameterListVw = new List<TrackBar>();

        public GenericUI(VstPluginContext openContext)
        {
            this.PluginContext = openContext;
            InitializeComponent();
            SetupControls();
        }

        public void SetupControls()
        {
            PluginParameterListVw.Clear();

            var paramCount = PluginContext.PluginInfo.ParameterCount;
            FPluginLabel.Text = PluginContext.PluginCommandStub.GetEffectName();
            Controls.Remove(FPluginLabel);

            //create sliders
            for (int i = 0; i < paramCount ; i++)
            {
                var value = PluginContext.PluginCommandStub.GetParameter(i);

                if(PluginContext.PluginCommandStub.CanParameterBeAutomated(i))
                    AddParameter(value, i);
            }

            //add sliders in reverse for layout
            for (int i = paramCount - 1; i >= 0; i--)
            {
                var trackbar = PluginParameterListVw[i];
                var label = (Label)trackbar.Tag;
                Controls.Add(trackbar);
                Controls.Add(label);
            }

            Controls.Add(FPluginLabel);
        }

        string GetParamText(int index)
        {
            string name = PluginContext.PluginCommandStub.GetParameterName(index);
            string unit = PluginContext.PluginCommandStub.GetParameterLabel(index);
            string display = PluginContext.PluginCommandStub.GetParameterDisplay(index);
            return $"{name}: {display} {unit}";
        }

        private void AddParameter(float value, int index)
        {
            var yPos = index * 60;
            var label = new Label();
            label.Padding = new Padding(5, 0, 0, 0); ;
            label.Font = new Font(label.Font.FontFamily, 12);
            label.Text = GetParamText(index);
            label.Dock = DockStyle.Top;
            label.Location = new Point(0, yPos);
            label.Tag = index;

            var trackbar = new TrackBar();
            trackbar.Maximum = 1000;  
            trackbar.Location = new Point(0, yPos + 30);
            trackbar.Dock = DockStyle.Top;
            trackbar.TickFrequency = 1;
            trackbar.TickStyle = TickStyle.None;
            trackbar.Value = VMath.Clamp((int)(value * 1000), trackbar.Minimum, trackbar.Maximum);
            trackbar.MouseDown += Trackbar_MouseDown;
            trackbar.MouseUp += Trackbar_MouseUp;
            trackbar.ValueChanged += Trackbar_ValueChanged;
            trackbar.Tag = label;

            PluginParameterListVw.Add(trackbar);
        }

        private void Trackbar_MouseDown(object sender, MouseEventArgs e)
        {
            var trackbar = sender as TrackBar;
            if (trackbar != null)
            {
                var label = (Label)trackbar.Tag;
                var index = (int)label.Tag;
                PluginContext.HostCommandStub.BeginEdit(index);
            }
        }


        private void Trackbar_MouseUp(object sender, MouseEventArgs e)
        {
            var trackbar = sender as TrackBar;
            if (trackbar != null)
            {
                var label = (Label)trackbar.Tag;
                var index = (int)label.Tag;
                PluginContext.HostCommandStub.EndEdit(index);
            }
        }


        private void Trackbar_ValueChanged(object sender, EventArgs e)
        {
            var trackbar = sender as TrackBar;
            if(trackbar != null)
            {
                var label = (Label)trackbar.Tag;
                var index = (int)label.Tag;
                
                PluginContext.PluginCommandStub.SetParameter(index, trackbar.Value / 1000.0f);
                
                label.Text = GetParamText(index);
            }
        }

        internal void RefreshValue(int index)
        {
            if (PluginContext.PluginInfo.ParameterCount > index)
            {
                var trackbar = PluginParameterListVw[index];
                var value = PluginContext.PluginCommandStub.GetParameter(index);
                trackbar.Value = VMath.Clamp((int)(value*1000), trackbar.Minimum, trackbar.Maximum);
                var label = (Label)trackbar.Tag;
                label.Text = GetParamText(index);
                
            }
        }
    }
}

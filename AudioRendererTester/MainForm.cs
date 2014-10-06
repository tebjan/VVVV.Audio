/*
 * Created by SharpDevelop.
 * User: TF
 * Date: 06.10.2014
 * Time: 22:48
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using VVVV.Audio;

namespace AudioRendererTester
{
    /// <summary>
    /// Description of MainForm.
    /// </summary>
    public partial class MainForm : Form
    {
        AudioRender FRenderer = new AudioRender();
        
        public MainForm()
        {
            //
            // The InitializeComponent() call is required for Windows Forms designer support.
            //
            InitializeComponent();
        }
        
        float PerSample(double time, int sampleNumber)
        {
            return (float)Math.Sin(time * 440 * Math.PI*2) * 0.5f;
        }
        
        float PerSample2(double time, int sampleNumber)
        {
            return (float)Math.Sin(time * 880 * Math.PI*2) * 0.5f;
        }
        
		void Button1Click(object sender, EventArgs e)
		{
		    FRenderer.Render(PerSample);
		}
		void Button2Click(object sender, EventArgs e)
		{
		    FRenderer.Render(null);
		}
		void Button3Click(object sender, EventArgs e)
		{
	       FRenderer.Render(PerSample2);
		}
		void MainFormFormClosed(object sender, FormClosedEventArgs e)
		{
		    FRenderer.Dispose();
		}
    }
}

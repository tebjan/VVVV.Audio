#region usings
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

using Jacobi.Vst.Core;
using Jacobi.Vst.Framework;
using Jacobi.Vst.Interop.Host;
using NAudio.CoreAudioApi;
using NAudio.Utils;
using NAudio.Wave;
using NAudio.Wave.Asio;
using NAudio.Wave.SampleProviders;
using VVVV.Audio;
using VVVV.Core.Logging;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.PluginInterfaces.V2.Graph;
using VVVV.PluginInterfaces.V2.NonGeneric;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;
using System.IO;

#endregion usings

namespace VVVV.Audio.VST
{
	public class VSTSignal : MultiChannelInputSignal
	{
		public VstPluginContext PluginContext;
        internal PluginInfoForm InfoForm;
		protected int FInputCount, FOutputCount;
		IWin32Window FOwnerWindow;
		
		public VSTSignal(string filename, IWin32Window ownerWindow)
		{
			FOwnerWindow = ownerWindow;
            Filename = filename;
		}

        private string FFilename;

        public string Filename
        {
            get { return FFilename; }
            set 
            {
                if (value != FFilename)
                {
                    FFilename = value;
                    Load(FFilename);
                }
            }
        }


        public string[] ProgramNames = new string[0];

        protected void Load(string filename)
        {
            if (File.Exists(filename))
            {

                PluginContext = OpenPlugin(filename);

                SetOutputCount(PluginContext.PluginInfo.AudioOutputCount);

                PluginContext.PluginCommandStub.MainsChanged(true);
                PluginContext.PluginCommandStub.SetSampleRate(WaveFormat.SampleRate);
                PluginContext.PluginCommandStub.SetBlockSize(AudioService.Engine.Settings.BufferSize);

                PluginContext.PluginCommandStub.StartProcess();

                FInputCount = PluginContext.PluginInfo.AudioInputCount;
                FOutputCount = PluginContext.PluginInfo.AudioOutputCount;

                FInputMgr = new VstAudioBufferManager(FInputCount, AudioService.Engine.Settings.BufferSize);
                FOutputMgr = new VstAudioBufferManager(FOutputCount, AudioService.Engine.Settings.BufferSize);

                FInputBuffers = FInputMgr.ToArray();
                FOutputBuffers = FOutputMgr.ToArray();

                // plugin does not support processing audio
                if ((PluginContext.PluginInfo.Flags & VstPluginFlags.CanReplacing) == 0)
                {
                    MessageBox.Show("This plugin does not process any audio.");
                    return;
                }

                InfoForm = new PluginInfoForm();
                InfoForm.PluginContext = PluginContext;
                InfoForm.DataToForm();
                InfoForm.Dock = DockStyle.Fill;

                GetProgramNames();
            }
        }

        //HACK: very evil hack
        private void GetProgramNames()
        {
            var ctx = OpenPlugin(FFilename);

            ProgramNames = new string[ctx.PluginInfo.ProgramCount];

            for (int i = 0; i < ctx.PluginInfo.ProgramCount; i++)
            {
                ctx.PluginCommandStub.SetProgram(i);
                ProgramNames[i] = ctx.PluginCommandStub.GetProgramName();
            }

            ctx.Dispose();
        }
		
		private void HostCmdStub_PluginCalled(object sender, PluginCalledEventArgs e)
		{
			HostCommandStub hostCmdStub = (HostCommandStub)sender;
			
			// can be null when called from inside the plugin main entry point.
			if (hostCmdStub.PluginContext.PluginInfo != null)
			{
				Debug.WriteLine("Plugin " + hostCmdStub.PluginContext.PluginInfo.PluginID + " called:" + e.Message);
			}
			else
			{
				Debug.WriteLine("The loading Plugin called:" + e.Message);
			}
		}
		
		private VstPluginContext OpenPlugin(string pluginPath)
		{
			try
			{
				HostCommandStub hostCmdStub = new HostCommandStub();
				hostCmdStub.PluginCalled += new EventHandler<PluginCalledEventArgs>(HostCmdStub_PluginCalled);
				
				VstPluginContext ctx = VstPluginContext.Create(pluginPath, hostCmdStub);
				
				// add custom data to the context
				ctx.Set("PluginPath", pluginPath);
				ctx.Set("HostCmdStub", hostCmdStub);
				
				// actually open the plugin itself
				ctx.PluginCommandStub.Open();
				
				return ctx;
			}
			catch (Exception e)
			{
				MessageBox.Show(FOwnerWindow, e.ToString(), "VST Load", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			
			return null;
		}
		
		VstAudioBufferManager FInputMgr = new VstAudioBufferManager(2, 1);
        VstAudioBufferManager FOutputMgr = new VstAudioBufferManager(2, 1);
		VstAudioBuffer[] FInputBuffers;
		VstAudioBuffer[] FOutputBuffers;
		protected void ManageBuffers(int count)
		{
			if(FInputMgr.BufferSize != count)
			{
				FInputMgr.Dispose();
				FOutputMgr.Dispose();
				
				FInputMgr = new VstAudioBufferManager(FInputCount, count);
				FOutputMgr = new VstAudioBufferManager(FOutputCount, count);
				
				FInputBuffers = FInputMgr.ToArray();
				FOutputBuffers = FOutputMgr.ToArray();
			}
		}
		
		protected override void FillBuffers(float[][] buffer, int offset, int count)
		{
            if (PluginContext != null)
            {
                ManageBuffers(count);

                if (FInput != null)
                {
                    FInputMgr.ClearAllBuffers();
                    for (int b = 0; b < FInputCount; b++)
                    {
                        var inSig = FInput[b];
                        if (inSig != null)
                        {
                            var vstBuffer = FInputBuffers[b % FInputCount];

                            //read input, use buffer[0] as temp buffer
                            inSig.Read(buffer[0], offset, count);

                            //copy to vst buffer
                            for (int i = 0; i < count; i++)
                            {
                                vstBuffer[i] += buffer[0][i];
                            }
                        }
                    }
                }

                //PluginContext.PluginCommandStub.SetBlockSize(count);

                //process the shit
                PluginContext.PluginCommandStub.ProcessReplacing(FInputBuffers, FOutputBuffers);


                for (int i = 0; i < FOutputBuffers.Length; i++)
                {
                    for (int j = 0; j < count; j++)
                    {
                        buffer[i][j] = FOutputBuffers[i][j];
                    }
                }
            }
		}
		
		public override void Dispose()
		{
			//close and dipose vst
            if (PluginContext != null && PluginContext.PluginCommandStub != null)
			{
				PluginContext.PluginCommandStub.StopProcess();
				PluginContext.PluginCommandStub.MainsChanged(false);
				PluginContext.Dispose();
                
			}

            if (FInputMgr != null)
            {
                FInputMgr.Dispose();
                FOutputMgr.Dispose();
            }
			
			base.Dispose();
		}
		
	}
}



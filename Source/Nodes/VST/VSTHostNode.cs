#region usings
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Drawing;
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
using VVVV.Audio.VST;
using VVVV.Core.Logging;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.PluginInterfaces.V2.Graph;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

#endregion usings

namespace VVVV.Nodes
{
	public class VSTSignal : MultiChannelInputSignal
	{
		protected VstPluginContext PluginContext;
		protected int inputCount, outputCount;
		
		public VSTSignal(VstPluginContext ctx, ISpread<AudioSignal> input)
			: base(input, ctx.PluginInfo.AudioOutputCount)
		{
			
			PluginContext = ctx;
			
			PluginContext.PluginCommandStub.SetSampleRate(44100f);
			PluginContext.PluginCommandStub.MainsChanged(true);
			PluginContext.PluginCommandStub.StartProcess();
			
			inputCount = PluginContext.PluginInfo.AudioInputCount;
            outputCount = PluginContext.PluginInfo.AudioOutputCount;
            
            inputMgr = new VstAudioBufferManager(inputCount, 512);
			outputMgr = new VstAudioBufferManager(outputCount, 512);
			
			inputBuffers = inputMgr.ToArray();
			outputBuffers = outputMgr.ToArray();
			
			// plugin does not support processing audio
            if ((PluginContext.PluginInfo.Flags & VstPluginFlags.CanReplacing) == 0)
            {
                MessageBox.Show("This plugin does not process any audio.");
                return;
            }
		}
		
		VstAudioBufferManager inputMgr;
		VstAudioBufferManager outputMgr;
		VstAudioBuffer[] inputBuffers;
		VstAudioBuffer[] outputBuffers;
		protected void ManageBuffers(int count)
		{
			if(inputMgr.BufferSize != count)
			{
				inputMgr.Dispose();
				outputMgr.Dispose();
				
				inputMgr = new VstAudioBufferManager(inputCount, count);
				outputMgr = new VstAudioBufferManager(outputCount, count);
				
				inputBuffers = inputMgr.ToArray();
				outputBuffers = outputMgr.ToArray();
			}
		}
		
		protected override void FillBuffers(float[][] buffer, int offset, int count)
		{
			ManageBuffers(count);
			
			if(FInput != null)
			{
				inputMgr.ClearAllBuffers();
				for (int b = 0; b < FInput.SliceCount; b++)
				{
					var inSig = FInput[b];
					if(inSig != null)
					{
						var vstBuffer = inputBuffers[b%inputCount];
						
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

			PluginContext.PluginCommandStub.SetBlockSize(count);

			//process the shit
			PluginContext.PluginCommandStub.ProcessReplacing(inputBuffers, outputBuffers);
			

			for (int i = 0; i < outputBuffers.Length; i++)
			{
				for (int j = 0; j < count; j++)
				{
					buffer[i][j] = outputBuffers[i][j];
				}
			}

		}
		
		public override void Dispose()
		{
			PluginContext.PluginCommandStub.StopProcess();
			PluginContext.PluginCommandStub.MainsChanged(false);
			base.Dispose();
			inputMgr.Dispose();
			outputMgr.Dispose();
		}
		
	}
	
	[PluginInfo(Name = "VSTHost", Category = "Audio", Version = "Source", Help = "Loads a VST plugin", AutoEvaluate = true, Tags = "plugin, effect")]
	public class VSTHostNode : UserControl, IPluginEvaluate
	{
		[Input("Input")]
		IDiffSpread<AudioSignal> Input;
		
		[Output("Output")]
		ISpread<AudioSignal> OutBuffer;
		
		VstPluginContext PluginContext;
		VSTSignal VstSignal;
		
		IHDEHost FHost;
		
		[Import]
		IPluginHost FPlugHost;
		
		[ImportingConstructor]
		public VSTHostNode([Import] IHDEHost host)
		{
			FHost = host;
			
			var mf = new MainForm();
			mf.ShowDialog();
			PluginContext = mf.SelectedPluginContext;
			
            var PluginCommandStub = PluginContext.PluginCommandStub;

            PluginCommandStub.MainsChanged(true);
			
			Rectangle wndRect = new Rectangle();

			this.Text = PluginCommandStub.GetEffectName();

			if (PluginCommandStub.EditorGetRect(out wndRect))
			{
				PluginCommandStub.EditorOpen(this.Handle);
			}
			
			//when window mode changes
			FHost.BeforeComponentModeChange += delegate(object sender, ComponentModeEventArgs args) 
			{
				var me = (FPlugHost as INode);
				if(me == args.Window.Node.InternalCOMInterf)
					PluginCommandStub.EditorClose();
			};
			
			FHost.AfterComponentModeChange += delegate(object sender, ComponentModeEventArgs args)
			{ 
				var me = (FPlugHost as INode);
				if(me == args.Window.Node.InternalCOMInterf)
					PluginCommandStub.EditorOpen(this.Handle);
			};
			
		}
		
		int FFrameDivider = 0;
		public void Evaluate(int SpreadMax)
		{
			if(Input.IsChanged)
			{
				VstSignal = new VSTSignal(PluginContext, Input);
				OutBuffer.SliceCount = PluginContext.PluginInfo.AudioOutputCount;
				OutBuffer.AssignFrom(VstSignal.Outputs);
			}
			
			//let plugin editor draw itself
			if(FFrameDivider == 0)
				PluginContext.PluginCommandStub.EditorIdle();
			
			FFrameDivider++;
			FFrameDivider %= 4;
		}
	}
}



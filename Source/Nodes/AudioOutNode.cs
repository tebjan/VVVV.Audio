#region usings
using System;
using System.ComponentModel.Composition;
using System.Collections.Generic;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;
using VVVV.Audio;

using NAudio.Wave;
using NAudio.Wave.Asio;
using NAudio.CoreAudioApi;
using NAudio.Wave.SampleProviders;
using NAudio.Utils;


using VVVV.Core.Logging;
#endregion usings

namespace VVVV.Nodes
{
	[PluginInfo(Name = "AudioOut", Category = "Audio", Help = "Audio Out", AutoEvaluate = true, Tags = "Asio, Wasapi, DirectSound, Wave")]
	public class AudioOutNode : IPluginEvaluate, IDisposable
	{
		#region fields & pins
		[Input("Input")]
		IDiffSpread<AudioSignal> FInput;
	
		//[Output("Output")]
		//ISpread<double> FOutput;
		
		[Import()]
		ILogger FLogger;
		#endregion fields & pins
	
		private ISpread<AudioSignal> LastSignals;
		public AudioOutNode()
		{
			LastSignals = new Spread<AudioSignal>();
		}
		
		public void Dispose()
		{
			AudioService.Engine.RemoveOutput(LastSignals);
		}
		
		private void AssignSignals()
		{
			AudioService.Engine.RemoveOutput(LastSignals);
			AudioService.Engine.AddOutput(FInput);
			LastSignals.AssignFrom(FInput);
		}		
		
		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			if(FInput.IsChanged)
			{
				AssignSignals();
			}
		}
	}
}



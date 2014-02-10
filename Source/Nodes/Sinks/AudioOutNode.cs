#region usings
using System;
using System.ComponentModel.Composition;
using System.Collections.Generic;
using System.Linq;

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
	[PluginInfo(Name = "AudioOut", Category = "VAudio", Help = "Audio Out", AutoEvaluate = true, Tags = "Asio")]
	public class AudioOutNode : IPluginEvaluate, IDisposable
	{
		#region fields & pins
		[Input("Input")]
		public IDiffSpread<AudioSignal> FInput;
		
		[Import()]
		ILogger FLogger;
		#endregion fields & pins
	
		private ISpread<MasterChannel> LastSignals;
		public AudioOutNode()
		{
			LastSignals = new Spread<MasterChannel>();
		}
		
		public void Dispose()
		{
			AudioService.Engine.RemoveOutput(LastSignals);
		}	
		
		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			if(FInput.IsChanged)
			{
				AudioService.Engine.RemoveOutput(LastSignals);
				LastSignals.SliceCount = SpreadMax;
				for (int i = 0; i < SpreadMax; i++)
				{
					LastSignals[i] = new MasterChannel(FInput[i], i);
				}
				
				AudioService.Engine.AddOutput(LastSignals);
			}
		}
	}
}



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
	public class LevelMeterSignal : SinkSignal<double>
	{
		public LevelMeterSignal(AudioSignal input)
			: base(44100)
		{
			if (input == null)
				throw new ArgumentNullException("Input of LevelMeterSignal construcor is null");
			FInput = input;
		}
		
		protected AudioSignal FInput;
		
		protected override void FillBuffer(float[] buffer, int offset, int count)
		{
			FInput.Read(buffer, offset, count);
			
			var max = 0.0;
			for (int i = offset; i < count; i++)
			{
				max = Math.Max(max, buffer[i]);
			}
			
			FStack.Push(Math.Max(20.0 * Math.Log10(max), -90.0));
		}
	}
	
	[PluginInfo(Name = "Level", Category = "Audio", Version = "Sink", Help = "Calculates the max dBs", Tags = "Meter, dB")]
	public class LevelMeterSignalNode : IPluginEvaluate
	{
		[Input("Input", DefaultValue = 0.1)]
		IDiffSpread<AudioSignal> FInput;
		
		[Output("Level")]
		ISpread<double> FLevelOut;
		
		Spread<LevelMeterSignal> FLevelMeters = new Spread<LevelMeterSignal>();
		
		public void Evaluate(int SpreadMax)
		{
			if(FInput.IsChanged)
			{
				//delete and dispose all inputs
				FLevelMeters.ResizeAndDispose(0, () => new LevelMeterSignal(FInput[0]));
				
				FLevelMeters.SliceCount = SpreadMax;
				for (int i = 0; i < SpreadMax; i++)
				{
					if(FInput[i] != null)
						FLevelMeters[i] = (new LevelMeterSignal(FInput[i]));
					
				}
				
				FLevelOut.SliceCount = SpreadMax;
			}
			
			//output value
			for (int i = 0; i < SpreadMax; i++)
			{
				
				if(FLevelMeters[i] != null)
				{
					var val = 0.0;
					FLevelMeters[i].GetLatestValue(out val);
					FLevelOut[i] = val;
				}
				else
				{
					FLevelOut[i] = 0;
				}
			}
		}
	}
}



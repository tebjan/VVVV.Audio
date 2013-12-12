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
			FInput = input;
		}
		
		public AudioSignal FInput;
		
		protected override void FillBuffer(float[] buffer, int offset, int count)
		{
			if(FInput != null)
			{
				FInput.Read(buffer, offset, count);
				
				var max = 0.0;
				for (int i = offset; i < count; i++)
				{
					max = Math.Max(max, Math.Abs(buffer[i]));
				}
				
				this.SetLatestValue(max);
			}
		}
	}
	
	[PluginInfo(Name = "Meter", Category = "Audio", Version = "Sink", Help = "Calculates the max dBs", Tags = "Meter, dB")]
	public class LevelMeterSignalNode : IPluginEvaluate
	{
		[Input("Input", DefaultValue = 0.1)]
		IDiffSpread<AudioSignal> FInput;
		
		[Input("Smoothing")]
		IDiffSpread<double> FSmoothing;
		
		[Output("Level")]
		ISpread<double> FLevelOut;

        [Output("Level dBs")]
        ISpread<double> FLeveldBsOut;
		
		Spread<LevelMeterSignal> FBufferReaders = new Spread<LevelMeterSignal>();
		
		public void Evaluate(int SpreadMax)
		{
			if(FInput.IsChanged)
			{
				FBufferReaders.SliceCount = SpreadMax;
				for (int i = 0; i < SpreadMax; i++)
				{
                    if (FInput[i] != null)
                    {
                        if (FBufferReaders[i] != null)
                        {
                            FBufferReaders[i].Dispose();
                        }
                        FBufferReaders[i] = (new LevelMeterSignal(FInput[i]));
                    }
                    else
                    {
                        if (FBufferReaders[i] != null)
                            FBufferReaders[i].FInput = null;
                    }
					
				}
				
				FLevelOut.SliceCount = SpreadMax;
                FLeveldBsOut.SliceCount = SpreadMax;
			}
			
			//output value
			for (int i = 0; i < SpreadMax; i++)
			{
				
				if(FBufferReaders[i] != null)
				{
					var val = 0.0;
					FBufferReaders[i].GetLatestValue(out val);
					var smooth = FSmoothing[i];
                    var level = FLevelOut[i] * smooth + val * (1-smooth);
					FLevelOut[i] = level;
                    FLeveldBsOut[i] = AudioUtils.SampleTodBs(level);
				}
				else
				{
					FLevelOut[i] = 0;
				}
			}
		}
	}
}



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
		{
			FInput = input;
		}
		
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
	
	[PluginInfo(Name = "Meter", Category = "Audio", Version = "Sink", Help = "Calculates the max dBs", Tags = "Meter, dB, Level")]
	public class LevelMeterSignalNode : GenericAudioSinkNodeWithOutputs<LevelMeterSignal, double>
	{		
		[Input("Smoothing")]
		IDiffSpread<double> FSmoothing;

        [Output("Level dBs")]
        ISpread<double> FLeveldBsOut;
		
		[Output("Level")]
		ISpread<double> FLevelOut;

        protected override void SetOutputs(int i, LevelMeterSignal instance)
        {
            if (instance != null)
            {
                var val = 0.0;
                instance.GetLatestValue(out val);
                var smooth = FSmoothing[i];
                var level = FLevelOut[i] * smooth + val * (1 - smooth);
                FLevelOut[i] = level;
                FLeveldBsOut[i] = AudioUtils.SampleTodBs(level);
            }
            else
            {
                FLeveldBsOut[i] = 0;
                FLevelOut[i] = 0;
            }
        }

        protected override void SetOutputSliceCount(int sliceCount)
        {
            FLevelOut.SliceCount = sliceCount;
            FLeveldBsOut.SliceCount = sliceCount;
        }

        protected override LevelMeterSignal GetInstance(int i)
        {
            return new LevelMeterSignal(FInputs[i]);
        }

        protected override void SetParameters(int i, LevelMeterSignal instance)
        {
            instance.Input = FInputs[i];
        }
    }
}



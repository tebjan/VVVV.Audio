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
	public class WaveRecorderSignal : SinkSignal<int>
	{
		public WaveRecorderSignal(AudioSignal input)
		{
			if (input == null)
				throw new ArgumentNullException("Input of LevelMeterSignal construcor is null");
			FInput = input;
		}
		

		protected override void FillBuffer(float[] buffer, int offset, int count)
		{
			FInput.Read(buffer, offset, count);
		}
	}
	
	
	[PluginInfo(Name = "Recorder", Category = "Audio", Version = "Sink", Help = "Records audio to disk", Tags = "Writer, File, Wave")]
	public class WaveRecorderNode : GenericAudioSinkNodeWithOutputs<WaveRecorderSignal, int>
	{

        protected override void SetOutputs(int i, WaveRecorderSignal instance)
        {
            throw new NotImplementedException();
        }

        protected override void SetOutputSliceCount(int sliceCount)
        {
            throw new NotImplementedException();
        }

        protected override WaveRecorderSignal GetInstance(int i)
        {
            return new WaveRecorderSignal(FInputs[i]);
        }

        protected override void SetParameters(int i, WaveRecorderSignal instance)
        {
            throw new NotImplementedException();
        }
    }
}



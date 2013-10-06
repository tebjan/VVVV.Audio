#region usings
using System;
using System.ComponentModel.Composition;
using System.Collections.Generic;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using NAudio.Wave;
using NAudio.Wave.Asio;
using NAudio.CoreAudioApi;
using NAudio.Wave.SampleProviders;
using NAudio.Utils;

using VVVV.Core.Logging;
#endregion usings

namespace VVVV.Audio
{
	
	//DRAFT
	public interface IAudioEffect
	{
		unsafe void Process(float* sample);
	}
	
	public class VolumeProcessor : IAudioEffect
	{
		public float Volume
		{
			get;
			set;
		}
		
		public unsafe void Process(float* sample)
		{
			for(int i=0; i<2; i++)
			{
				sample[i] = sample[i] * Volume;
			}
		}
	}
	
	public class AudioEffectSignal : AudioSignal
	{
		public AudioEffectSignal(int sampleRate)
			: base(sampleRate)
		{
		}
		
		protected IAudioEffect Effect;
		protected AudioSignal Input;
		
		public unsafe int Read(float[] buffer, int offset, int count)
		{
			Input.Read(buffer, offset, count);
			
			var channels = this.WaveFormat.Channels;
			
			for (int i = 0; i < (count/channels); i++)
			{
				fixed(float* sample = &buffer[i*channels])
				{
					Effect.Process(sample);
				}
			}
			
			return count;
			
		}
	}
}



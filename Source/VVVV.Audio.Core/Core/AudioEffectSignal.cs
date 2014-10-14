#region usings
using System;
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
		public AudioEffectSignal()
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



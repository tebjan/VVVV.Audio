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
	public class SineSignal : AudioSignal
	{
		public SineSignal(float frequency, float gain)
			: base(44100)
		{
			Frequency = frequency;
			Gain = gain;
		}
		
		public float Frequency;
		public float Gain = 0.1f;
		private float TwoPi = (float)(Math.PI * 2);
		private float phase = 0;
		
		protected override void FillBuffer(float[] buffer, int offset, int count)
		{
			
			var sampleRate = this.WaveFormat.SampleRate;
			var increment = TwoPi*Frequency/sampleRate;
			for (int i = 0; i < count; i++)
			{
				// Sinus Generator
				buffer[i] = Gain*(float)Math.Sin(phase);
				
				phase += increment;
				if(phase > TwoPi)
					phase -= TwoPi;
				else if(phase < 0)
					phase += TwoPi;
			}
			
		}
	}
	
	[PluginInfo(Name = "Sine", Category = "Audio", Version = "Source", Help = "Creates a sine wave", AutoEvaluate = true, Tags = "Wave")]
	public class SineSignalNode : IPluginEvaluate
	{
		[Input("Frequency", DefaultValue = 440)]
		IDiffSpread<float> Frequency;
		
		[Input("Gain", DefaultValue = 0.1)]
		IDiffSpread<float> Gain;
		
		[Output("Audio Out")]
		ISpread<AudioSignal> OutBuffer;
		
		public void Evaluate(int SpreadMax)
		{
			OutBuffer.ResizeAndDispose(SpreadMax, index => new SineSignal(Frequency[index], Gain[index]));
			
			if(Frequency.IsChanged)
			{
				for(int i=0; i<SpreadMax; i++)
				{
					if(OutBuffer[i] == null) OutBuffer[i] = new SineSignal(Frequency[i], Gain[i]); 
					
					(OutBuffer[i] as SineSignal).Frequency = Frequency[i];
				}
			}
			
			if(Gain.IsChanged)
			{
				for(int i=0; i<SpreadMax; i++)
				{
					(OutBuffer[i] as SineSignal).Gain  = Gain[i];
				}
			}
		}
	}
}



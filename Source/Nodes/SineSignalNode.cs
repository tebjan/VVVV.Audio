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
		public SineSignal(double frequency)
			: base(44100)
		{
			Frequency = frequency;
		}
		
		private double Frequency;
		public double Gain = 0.1;
		private double TwoPi = Math.PI * 2;
		private int nSample = 0;
		
		protected override void FillBuffer(float[] buffer, int offset, int count)
		{
			
			var sampleRate = this.WaveFormat.SampleRate;
			var multiple = TwoPi*Frequency/sampleRate;
			for (int i = 0; i < count; i++)
			{
				// Sinus Generator
				
				buffer[i] = (float)(Gain*Math.Sin(nSample*multiple));
				
				unchecked
				{
					nSample++;
				}
			}
			
		}
		
	}
	
	[PluginInfo(Name = "Sine", Category = "Naudio", Version = "Source", Help = "Creates a sine wave", AutoEvaluate = true, Tags = "Wave")]
	public class SineSignalNode : IPluginEvaluate
	{
		[Input("Frequency", DefaultValue = 440)]
		IDiffSpread<double> Frequency;
		
		[Input("Gain", DefaultValue = 0.1)]
		IDiffSpread<double> Gain;
		
		[Output("Audio Out")]
		ISpread<AudioSignal> OutBuffer;
		
		public void Evaluate(int SpreadMax)
		{
			//OutBuffer.ResizeAndDispose(SpreadMax, index =>  new SineSignal(Frequency[index]));
			
			if(Frequency.IsChanged)
			{
				OutBuffer.SliceCount = SpreadMax;
				for(int i=0; i<SpreadMax; i++)
				{
					OutBuffer[i] = new SineSignal(Frequency[i]);
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



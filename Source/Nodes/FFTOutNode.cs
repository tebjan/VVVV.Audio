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
using Lomont;

using VVVV.Core.Logging;
#endregion usings

namespace VVVV.Nodes
{
	public class FFTOutSignal : SinkSignal<double[]>
	{
		protected LomontFFT FFFT = new LomontFFT();
		public FFTOutSignal(AudioSignal input)
			: base(44100)
		{
			if (input == null)
				throw new ArgumentNullException("Input of LevelMeterSignal construcor is null");
			Source = input;
		}
		
		double[] FFFTBuffer = new double[1];
		protected override void FillBuffer(float[] buffer, int offset, int count)
		{
			if(FFFTBuffer.Length != count)
				FFFTBuffer = new double[count];
			
			FSource.Read(buffer, offset, count);
			
			for (int i = 0; i < count; i++)
			{
				FFFTBuffer[i] = buffer[i];
			}
			
			FFFT.RealFFT(FFFTBuffer, true);
			FStack.Push((double[])FFFTBuffer.Clone());
		}
	}
	
	[PluginInfo(Name = "FFT", Category = "Audio", Version = "Sink", Help = "Calculates the FFT of an audio buffer", Tags = "Spectrum, Frequencies")]
	public class FFTOutNode : IPluginEvaluate
	{
		[Input("Input")]
		IDiffSpread<AudioSignal> FInput;
		
		[Output("Output")]
		ISpread<ISpread<double>> FLevelOut;
		
		Spread<FFTOutSignal> FBufferReaders = new Spread<FFTOutSignal>();
		
		public void Evaluate(int SpreadMax)
		{
			if(FInput.IsChanged)
			{
				//delete and dispose all inputs
				FBufferReaders.ResizeAndDispose(0, () => new FFTOutSignal(FInput[0]));
				
				FBufferReaders.SliceCount = SpreadMax;
				for (int i = 0; i < SpreadMax; i++)
				{
					if(FInput[i] != null)
						FBufferReaders[i] = (new FFTOutSignal(FInput[i]));
					
				}
				
				FLevelOut.SliceCount = SpreadMax;
			}
			
			//output value
			for (int i = 0; i < SpreadMax; i++)
			{
				if(FBufferReaders[i] != null)
				{
					var spread = FLevelOut[i];
					double[] val = null;
					FBufferReaders[i].GetLatestValue(out val);
					if(val != null)
					{
						if(spread == null)
						{
							spread = new Spread<double>(val.Length);
						}
						spread.SliceCount = val.Length;
						spread.AssignFrom(val);
					}
				}
				else
				{
					FLevelOut[i].SliceCount = 0;
				}
			}
		}
	}
}



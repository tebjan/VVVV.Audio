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
		{
			FSource = input;
		}
		
		AudioSignal FSource;
		double[] FFFTBuffer = new double[1];
		protected override void FillBuffer(float[] buffer, int offset, int count)
		{
            if (FInput != null)
            {
                if (FFFTBuffer.Length != count)
                    FFFTBuffer = new double[count];

                FSource.Read(buffer, offset, count);

                buffer.ReadDouble(FFFTBuffer, offset, count);

                FFFT.RealFFT(FFFTBuffer, true);

                this.SetLatestValue(FFFTBuffer);
            }
            else
            {
                this.SetLatestValue(new double[1]);
            }
		}
	}
	
	[PluginInfo(Name = "FFT", Category = "VAudio", Version = "Sink", Help = "Calculates the FFT of an audio buffer", Tags = "Spectrum, Frequencies")]
	public class FFTOutNode : GenericAudioSinkNodeWithOutputs<FFTOutSignal, double[]>
	{
		[Output("Output")]
		ISpread<ISpread<double>> FFFTOut;

        protected override void SetOutputs(int i, FFTOutSignal instance)
        {
            if (instance != null)
            {
                var spread = FFFTOut[i];
                double[] val = null;
                instance.GetLatestValue(out val);
                if (val != null)
                {
                    if (spread == null)
                    {
                        spread = new Spread<double>(val.Length);
                    }
                    spread.SliceCount = val.Length;
                    spread.AssignFrom(val);
                }
            }
            else
            {
                FFFTOut[i].SliceCount = 0;
            }
        }

        protected override void SetOutputSliceCount(int sliceCount)
        {
            FFFTOut.SliceCount = sliceCount;
        }

        protected override FFTOutSignal GetInstance(int i)
        {
            return new FFTOutSignal(FInputs[i]);
        }

        protected override void SetParameters(int i, FFTOutSignal instance)
        {
            instance.Input = FInputs[i];
        }
    }
}



#region usings
using System;
using System.ComponentModel.Composition;
using System.Collections.Generic;

using NAudio.Utils;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;
using VVVV.Audio;

using VVVV.Core.Logging;
#endregion usings

namespace VVVV.Nodes
{	
    
	[PluginInfo(Name = "FFT", Category = "VAudio", Version = "Sink", Help = "Calculates the FFT of an audio signal", Tags = "Spectrum, Frequencies")]
	public class FFTOutNode : GenericAudioSinkNode<FFTOutSignal>
	{
	    [Input("Window Function")]
		public IDiffSpread<WindowFunction> FWindowFuncIn;
	    
	    [Input("Buffer Size", DefaultValue = 256)]
		public IDiffSpread<int> FSize;
		
		[Output("Output")]
		ISpread<ISpread<double>> FFFTOut;

//		[Output("Output Complex")]
//		ISpread<ISpread<double>> FFFTOutComplex;
		
		uint UpperPow2(uint v)
		{
		    v--;
		    v |= v >> 1;
		    v |= v >> 2;
		    v |= v >> 4;
		    v |= v >> 8;
		    v |= v >> 16;
		    v++;
		    return v;
		}
        
		readonly float Min150dB = (float)Decibels.DecibelsToLinear(-150);
        protected override void SetOutputs(int i, FFTOutSignal instance)
        {
            if (instance != null)
            {
//                var spreadComplex = FFFTOutComplex[i];
                var spread = FFFTOut[i];
                double[] fftData = instance.FFTOut;
                if (fftData != null)
                {
                    if(spread == null)
                    {
                        spread = new Spread<double>(fftData.Length);
                    }
                    
//                    if (spreadComplex == null)
//                    {
//                        spreadComplex = new Spread<double>(val.Length);
//                    }
                    
                    var halfSize = fftData.Length / 2;
                    spread.SliceCount = halfSize;
                    spread[0] = 0;
                    
                    var nn = 2;
                    for (int n = 1; n < halfSize; n++) 
                    {
                        var real = fftData[nn++];
                        var imag = fftData[nn++];
                        spread[n] = Decibels.LinearToDecibels(Math.Max(Math.Sqrt(real * real + imag * imag), Min150dB)) / 150 + 1;
                    }

//                    spreadComplex.SliceCount = val.Length;
//                    spreadComplex.AssignFrom(val);
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
//            FFFTOutComplex.SliceCount = sliceCount;
        }

        protected override FFTOutSignal GetInstance(int i)
        {
            return new FFTOutSignal(FInputs[i]);
        }

        protected override void SetParameters(int i, FFTOutSignal instance)
        {
            instance.Input = FInputs[i];
            instance.Size = (int)UpperPow2((uint)FSize[i]);
            instance.WindowFunc = FWindowFuncIn[i];
        }
    }
}



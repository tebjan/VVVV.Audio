#region usings
using System;
using System.ComponentModel.Composition;
using System.Collections.Generic;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;
using VVVV.Audio;

using VVVV.Core.Logging;
#endregion usings

namespace VVVV.Nodes
{
	
	
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



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

	[PluginInfo(Name = "A2V", Category = "VAudio", Version = "Sink", Help = "Outputs the latest audio sample as value", Tags = "Meter")]
	public class AudioToValueNode : GenericAudioSinkNode<AudioToValueSignal>
	{		
		[Input("Smoothing")]
		public IDiffSpread<double> FSmoothing;
		
		[Output("Sample")]
		public ISpread<double> FSampleOut;
		
        protected override void SetOutputs(int i, AudioToValueSignal instance)
        {
            if (instance != null)
            {
                var smooth = FSmoothing[i];
                var level = FSampleOut[i] * smooth + instance.Value * (1 - smooth);
                FSampleOut[i] = level;
            }
            else
            {
                FSampleOut[i] = 0;
            }
        }

        protected override void SetOutputSliceCount(int sliceCount)
        {
            FSampleOut.SliceCount = sliceCount;
        }

        protected override AudioToValueSignal GetInstance(int i)
        {
            return new AudioToValueSignal(FInputs[i]);
        }

        protected override void SetParameters(int i, AudioToValueSignal instance)
        {
            instance.InputSignal.Value = FInputs[i];
        }
    }
}



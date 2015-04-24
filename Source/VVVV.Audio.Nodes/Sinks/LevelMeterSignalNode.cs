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

	[PluginInfo(Name = "Meter", Category = "VAudio", Version = "Sink", Help = "Calculates the max dBs", Tags = "Meter, dB, Level")]
	public class LevelMeterSignalNode : GenericAudioSinkNode<LevelMeterSignal>
	{		
		[Input("Smoothing")]
		public IDiffSpread<double> FSmoothing;

        [Output("Level dBs")]
        public ISpread<double> FLeveldBsOut;
		
		[Output("Level")]
		public ISpread<double> FLevelOut;
		
		readonly float Min150dB = (float)Decibels.DecibelsToLinear(-150);

        protected override void SetOutputs(int i, LevelMeterSignal instance)
        {
            if (instance != null)
            {
                var smooth = FSmoothing[i];
                var level = FLevelOut[i] * smooth + instance.Max * (1 - smooth);
                FLevelOut[i] = level;
                FLeveldBsOut[i] = Decibels.LinearToDecibels(Math.Max(level, Min150dB));
            }
            else
            {
                FLeveldBsOut[i] = 0;
                FLevelOut[i] = 0;
            }
        }

        protected override void SetOutputSliceCount(int sliceCount)
        {
            FLevelOut.SliceCount = sliceCount;
            FLeveldBsOut.SliceCount = sliceCount;
        }

        protected override LevelMeterSignal GetInstance(int i)
        {
            return new LevelMeterSignal(FInputs[i]);
        }

        protected override void SetParameters(int i, LevelMeterSignal instance)
        {
            instance.InputSignal.Value = FInputs[i];
        }
    }
}



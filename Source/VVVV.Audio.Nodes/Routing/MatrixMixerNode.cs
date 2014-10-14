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

	
	
	
	[PluginInfo(Name = "MatrixMixer", Category = "VAudio", Version = "Filter", Help = "Mixes the input signals to any output", AutoEvaluate = true, Tags = "mix, map, multichannel")]
    public class MatrixMixerNode : IPluginEvaluate, IPartImportsSatisfiedNotification
	{
		[Input("Input")]
		public IDiffSpread<AudioSignal> FInput;

        [Input("Gain")]
        public IDiffSpread<float> Gain;
		
		[Input("Output Count", DefaultValue = 2, IsSingle = true)]
		public IDiffSpread<int> FOutChannels;
		
		[Output("Output")]
		public ISpread<AudioSignal> OutBuffer;
		
		MatrixMixerSignal FMixer = new MatrixMixerSignal();
		
		public void Evaluate(int SpreadMax)
		{
			
			if(FInput.IsChanged || FOutChannels.IsChanged)
			{
				FMixer.Input = FInput;
				FMixer.OutputChannelCount = FOutChannels[0];
				//OutBuffer.SliceCount = FOutChannels[0];
				OutBuffer.AssignFrom(FMixer.Outputs);
				FMixer.GainMatrix.AssignFrom(Gain);
			}
			
			if(Gain.IsChanged)
			{
				FMixer.GainMatrix.AssignFrom(Gain);
			}
		}

        public void OnImportsSatisfied()
        {
            Gain.SliceCount = 4;
            Gain.AssignFrom(new float[] { 1, 0, 0, 1 });
        }
    }
}



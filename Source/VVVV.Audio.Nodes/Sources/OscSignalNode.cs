#region usings
using System;
using System.ComponentModel.Composition;
using System.Collections.Generic;
using System.Linq.Expressions;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;
using VVVV.Audio;
using VVVV.Core.Logging;

#endregion usings

namespace VVVV.Nodes
{

	[PluginInfo(Name = "Osc", Category = "VAudio", Version = "Source", Help = "Creates an audio wave", AutoEvaluate = true, Tags = "Sine, Triangle, Square, Sawtooth, Wave")]
	public class OscSignalNode : GenericAudioSourceNode<OscSignal>
	{
	    [Input("WaveForm")]
		public IDiffSpread<WaveFormSelection> WaveForm;
	    
		[Input("Frequency", DefaultValue = 440)]
		public IDiffSpread<float> Frequency;
		
		[Input("Slope", DefaultValue = 0.5)]
		public IDiffSpread<float> FSlope;
		
		[Input("FMLevel")]
		public IDiffSpread<float> FFMLevel;
		
		[Input("FM")]
		public IDiffSpread<AudioSignal> FFMInput;
		
		[Input("PTR")]
		public IDiffSpread<bool> FPTR;
		
		[Input("Gain", DefaultValue = 0.1)]
		public IDiffSpread<float> Gain;
		
		protected override void SetParameters(int i, OscSignal instance)
		{
		    instance.WaveForm = WaveForm[i];
			instance.Gain = Gain[i];
			instance.Frequency = Frequency[i];
			instance.Slope = FSlope[i];
			instance.PTR = FPTR[i];
			instance.Input = FFMInput[i];
			instance.FMLevel = FFMLevel[i];
		}

        protected override OscSignal GetInstance(int i)
		{
			return new OscSignal(Frequency[i], Gain[i]);
		}
	}
	
	
	[PluginInfo(Name = "MultiSine", Category = "VAudio", Version = "Source", Help = "Creates a spread of sine waves", AutoEvaluate = true, Tags = "LFO, additive, synthesis")]
	public class MultiSineSignalNode : GenericAudioSourceNode<MultiSineSignal>
	{
		[Input("Frequency", DefaultValue = 440)]
		public IDiffSpread<ISpread<float>> Frequency;
		
		[Input("Gain", DefaultValue = 0.1)]
		public IDiffSpread<ISpread<float>> Gain;
		
		protected override int GetSpreadMax(int originalSpreadMax)
		{
			return Math.Max(Frequency.SliceCount, Gain.SliceCount);
		}
		
		protected override void SetParameters(int i, MultiSineSignal instance)
		{
			instance.Gains = Gain[i];
			instance.Frequencies = Frequency[i];
		}

        protected override MultiSineSignal GetInstance(int i)
		{
			return new MultiSineSignal(Frequency[i], Gain[i]);
		}
	}
}



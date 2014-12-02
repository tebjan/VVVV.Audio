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

	/// <summary>
	/// Description of ResampleFilter.
	/// </summary>
	[PluginInfo(Name = "Resample", Category = "VAudio", Version = "Filter", Help = "Resamples the input signal to any output sample rate", AutoEvaluate = true, Tags = "sample rate, converter")]
	public class ResampleNode : GenericAudioSourceNode<ResampleSignal>
	{
	    [Input("Input")]
		public IDiffSpread<AudioSignal> FInputs;
	    
		[Input("Source Rate", DefaultValue = 44100, StepSize = 100)]
		public IDiffSpread<double> FSrcRateIn;
		
		[Input("Destination Rate", DefaultValue = 44100, StepSize = 100)]
		public IDiffSpread<double> FDstRateIn;
		
		[Input("Required Transition Band", DefaultValue = 3)]
		public IDiffSpread<double> FReqTransBandIn;
		
		[Input("Destination Rate Is Engine Rate")]
		public IDiffSpread<bool> FDstIsEngineRateIn;
		
		[Output("Resampler Input Latency")]
        public ISpread<int> FLatencyOut;
		
		protected override void SetParameters(int i, ResampleSignal instance)
		{
			instance.InputSignal.Value = FInputs[i];
			instance.DestinationRateIsEngineRate = FDstIsEngineRateIn[i];
			instance.SetupConverter(FSrcRateIn[i], FDstRateIn[i], FReqTransBandIn[i]);
		}

        protected override ResampleSignal GetInstance(int i)
		{
			return new ResampleSignal(FSrcRateIn[i], FDstRateIn[i], FInputs[i], FReqTransBandIn[i]);
		}
		
		protected override void SetOutputSliceCount(int sliceCount)
		{
			FLatencyOut.SliceCount = sliceCount;
		}
		
		protected override void SetOutputs(int i, ResampleSignal instance)
		{
			FLatencyOut[i] = instance.Latency;
		}
	}
}



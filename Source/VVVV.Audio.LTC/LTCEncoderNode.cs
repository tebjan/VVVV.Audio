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
using LTCSharp;

#endregion usings

namespace VVVV.Nodes
{

	[PluginInfo(Name = "LTCEncoder", Category = "VAudio", Version = "Source", Help = "Creates a LTC audio signal", AutoEvaluate = true, Tags = "Wave")]
	public class LTCEncoderSignalNode : GenericAudioSourceNode<LTCEncoderSignal>
	{
		[Input("Play")]
		public IDiffSpread<bool> FPlayIn;
		
		[Input("Timecode")]
		public IDiffSpread<Timecode> FTimecodeIn;
		
		[Input("Do Seek", IsBang = true)]
		public IDiffSpread<bool> FDoSeekIn;
		
		[Input("FPS", DefaultValue = 25)]
		public IDiffSpread<double> FFPSIn;
		
		[Input("TV Standard", DefaultEnumEntry = "TV625_50i")]
		public IDiffSpread<TVStandard> FTVStandardIn;
		
		[Input("BG Flags", DefaultEnumEntry = "NONE")]
		public IDiffSpread<BGFlags> FBGFlagsIn;
		
		[Output("Position")]
		public ISpread<Timecode> FPositionOut;
		
		protected override void SetParameters(int i, LTCEncoderSignal instance)
		{
			instance.Encoder.Play = FPlayIn[i];
			
			if(FFPSIn.IsChanged)
				instance.Encoder.SetFPS(FFPSIn[i]);
			
			if(FTVStandardIn.IsChanged || FBGFlagsIn.IsChanged)
			{
				var tc = instance.Encoder.Timecode;
				instance.Init(FFPSIn[i], FTVStandardIn[i], FBGFlagsIn[i]);
				instance.Encoder.Timecode = tc;
			}
			
			if(FDoSeekIn[i])
				instance.Encoder.Timecode = FTimecodeIn[i];
		}
		
		protected override void SetOutputs(int i, LTCEncoderSignal instance)
		{
			FPositionOut[i] = instance.Encoder.Timecode;
		}
		
		protected override void SetOutputSliceCount(int sliceCount)
		{
			FPositionOut.SliceCount = sliceCount;
		}

        protected override LTCEncoderSignal GetInstance(int i)
		{
			return new LTCEncoderSignal(FFPSIn[i], FTVStandardIn[i], FBGFlagsIn[i]);
		}
	}
}



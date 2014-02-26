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


using VVVV.Core.Logging;
#endregion usings

namespace VVVV.Nodes
{

	
	public class MatrixMixerSignal : MultiChannelInputSignal
	{
		public ISpread<float> GainMatrix
		{
			get;
			protected set;
		}
		
		public int OutputChannelCount
		{
			get
			{
				return this.FOutputCount;
			}
			set
			{
				SetOutputCount(value);
				GainMatrix = new Spread<float>(value);
			}
		}
		
		float[] FTempBuffer = new float[1];
		protected override void FillBuffers(float[][] buffer, int offset, int count)
		{
			if(FInput != null && FInput.SliceCount != 0)
			{
				FTempBuffer = BufferHelpers.Ensure(FTempBuffer, count);
				for (int outSlice = 0; outSlice < FOutputCount; outSlice++) 
				{
					var outbuf = buffer[outSlice];
					for (int inSlice = 0; inSlice < FInput.SliceCount; inSlice++)
					{
						var gain = GainMatrix[outSlice + inSlice * FOutputCount];
						var inSig = FInput[inSlice];
						if(inSig != null)
						{
							inSig.Read(FTempBuffer, offset, count);
							
							if(inSlice == 0)
							{
								for (int j = 0; j < count; j++)
								{
									outbuf[j] = FTempBuffer[j] * gain;
								}
							}
							else
							{
								for (int j = 0; j < count; j++)
								{
									outbuf[j] += FTempBuffer[j] * gain;
								}
							}
						}
					}
				}
			}
		}
	}
	
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



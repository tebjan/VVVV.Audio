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
		public MatrixMixerSignal(ISpread<AudioSignal> input, int outChannels)
			: base(input, outChannels)
		{
			GainMatrix = new Spread<float>(outChannels);
		}
		
		public ISpread<float> GainMatrix
		{
			get;
			protected set;
		}
		
		float[] FTempBuffer = new float[1];
		protected override void FillBuffer(float[][] buffer, int offset, int count)
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
	
	[PluginInfo(Name = "MatrixMixer", Category = "Audio", Version = "Filter", Help = "Mixes the input signals to any output", AutoEvaluate = true, Tags = "mix, map, multichannel")]
	public class MatrixMixerNode : IPluginEvaluate
	{
		[Input("Input")]
		IDiffSpread<AudioSignal> FInput;
		
		[Input("Gain")]
		IDiffSpread<float> Gain;
		
		[Input("Output Count", DefaultValue = 2)]
		IDiffSpread<int> FOutChannels;
		
		[Output("Output")]
		ISpread<AudioSignal> OutBuffer;
		
		MatrixMixerSignal FMixer;
		
		public void Evaluate(int SpreadMax)
		{
			
			if(FInput.IsChanged || FOutChannels.IsChanged)
			{
				FMixer = new MatrixMixerSignal(FInput, FOutChannels[0]);
				OutBuffer.SliceCount = FOutChannels[0];
				OutBuffer.AssignFrom(FMixer.Outputs);
				FMixer.GainMatrix.AssignFrom(Gain);
			}
			
			if(Gain.IsChanged)
			{
				FMixer.GainMatrix.AssignFrom(Gain);
			}
		}
	}
}



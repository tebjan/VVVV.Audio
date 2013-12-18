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
	public class ResamplerPullBuffer : CircularPullBuffer
	{
		R8BrainSampleRateConverter FConverter;
		
		public ResamplerPullBuffer(ISampleProvider input, R8BrainSampleRateConverter converter)
			: base(4096, input)
		{
			FConverter = converter;
			PullCount = FConverter.Latency + 1024;
		}
		
		double[] FInBuffer = new double[1];
		double[] FOutBuffer = new double[1];
		float[] FOutFloats = new float[1];
		
		public override void Pull(int count)
		{
			if(FTmpBuffer.Length != count)
			{
	    		FTmpBuffer = new float[count];
	    		FInBuffer = new double[count];
			}
	    	
	    	Input.Read(FTmpBuffer, 0, count);
	    	
	    	FTmpBuffer.ReadDouble(FInBuffer, 0, count);
	    	
	    	var samples = FConverter.Process(FInBuffer, ref FOutBuffer);
	    	
	    	if(FOutFloats.Length < samples)
	    	{
	    		FOutFloats = new float[samples];
	    	}
	    	
	    	FOutFloats.WriteDouble(FOutBuffer, 0, samples);
	    	
	    	Write(FOutFloats, 0, samples);

		}
	}
	
	public class ResampleSignal : AudioSignalInput
	{
		
		ResamplerPullBuffer FPullBuffer;
		
		public ResampleSignal(double srcRate, double dstRate, AudioSignal input, double reqTransBand = 3)
		{
			FConverter = new R8BrainSampleRateConverter(srcRate, dstRate, 4096, reqTransBand, R8BrainResamplerResolution.R8Brain16);
			FPullBuffer = new ResamplerPullBuffer(input, FConverter);
		}
		
		
		public int Latency
		{
			get
			{
				return FConverter.Latency;
			}
		}
		
		public void Prepare()
		{
			
		}
		
		R8BrainSampleRateConverter FConverter;
	
		double[] FConverterBuffer = new double[4096];
		
		protected override void FillBuffer(float[] buffer, int offset, int count)
		{
			FPullBuffer.Read(buffer, offset, count);
		}
	}
	
	/// <summary>
	/// Description of ResampleFilter.
	/// </summary>
	[PluginInfo(Name = "Resample", Category = "Audio", Version = "Filter", Help = "Resamples the input signal to any output sample rate", AutoEvaluate = true, Tags = "sample rate, converter")]
	public class ResampleNode : GenericAudioFilterNode<ResampleSignal>
	{
		[Input("Source Rate", DefaultValue = 44100, StepSize = 100)]
		IDiffSpread<double> FSrcRateIn;
		
		[Input("Destination Rate", DefaultValue = 44100, StepSize = 100)]
		IDiffSpread<double> FDstRateIn;
		
		[Input("Required Transition Band", DefaultValue = 3)]
		IDiffSpread<double> FReqTransBandIn;
		
		public ResampleNode()
		{
		}
		
		protected override void SetParameters(int i, ResampleSignal instance)
		{
			instance.Input = FInputs[i];
		}
		
		protected override AudioSignal GetInstance(int i)
		{
			return new ResampleSignal(FSrcRateIn[i], FDstRateIn[i], FInputs[i], FReqTransBandIn[i]);
		}
	}
}



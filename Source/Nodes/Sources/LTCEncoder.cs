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
using NAudio.Wave;
using NAudio.Wave.Asio;
using NAudio.CoreAudioApi;
using NAudio.Wave.SampleProviders;
using NAudio.Utils;
using VVVV.Core.Logging;
using LTCSharp;

#endregion usings

namespace VVVV.Nodes
{
	public class LTCPullBuffer : CircularPullBuffer
	{
		LTCSharp.Encoder FEncoder;
		
		public LTCPullBuffer(LTCSharp.Encoder encoder)
			: base(4096)
		{
			FEncoder = encoder;
			
			//fill the buffer with the first pull
			Pull(PullCount);
		}
		
		byte[] FByteBuffer = new byte[4096];
		float[] FOutFloats = new float[4096];

		public override void Pull(int count)
		{	  
			//create samples
			FEncoder.encodeFrame();
			var samples = FEncoder.getBuffer(FByteBuffer, 0);
	    	
	    	for (int i = 0; i < samples; i++) 
	    	{
	    		FOutFloats[i] = (FByteBuffer[i] - 127) / 128.0f;
	    	}
	    	
	    	Write(FOutFloats, 0, samples);
	    	
	    	FEncoder.incrementFrame();
		}

	}

	public class LTCEncoderSignal : AudioSignal
	{
		LTCSharp.Encoder FEncoder;
		LTCPullBuffer FRingBuffer;
		
		public LTCEncoderSignal()
		{
			Init();
		}
		
		private void Init()
		{
			if(FEncoder != null)
				FEncoder.Dispose();
			
			FEncoder = new Encoder(AudioEngine.Instance.Settings.SampleRate, 25, TVStandard.TV525_60i, BGFlags.NONE);
			FRingBuffer = new LTCPullBuffer(FEncoder);
		}
		
		public void SetTimecode(Timecode time)
		{
			lock(FEncoder)
			{
				FEncoder.setTimecode(time);
			}
		}

		public Timecode GetTimecode()
		{
			Timecode time;
			lock(FEncoder)
			{
				time = FEncoder.getTimecode();
			}
			return time;
		}
		
		protected override void FillBuffer(float[] buffer, int offset, int count)
		{
			FRingBuffer.Read(buffer, offset, count);
		}
		
		public override void Dispose()
		{
			if(FEncoder != null)
				FEncoder.Dispose();
			base.Dispose();
		}
	}
	
	[PluginInfo(Name = "LTCEncoder", Category = "VAudio", Version = "Source", Help = "Creates a LTC audio signal", AutoEvaluate = true, Tags = "Wave")]
	public class LTCEncoderSignalNode : GenericAudioSourceNodeWithOutputs<LTCEncoderSignal>
	{
		[Input("Timecode")]
		public IDiffSpread<Timecode> FTimecodeIn;
		
		[Input("Do Seek", IsBang = true)]
		public IDiffSpread<bool> FDoSeekIn;
		
		[Output("Position")]
		public ISpread<Timecode> FPositionOut;
		
		protected override void SetParameters(int i, LTCEncoderSignal instance)
		{
			if(FDoSeekIn[i])
				instance.SetTimecode(FTimecodeIn[i]);
		}
		
		protected override void SetOutputs(int i, LTCEncoderSignal instance)
		{
			FPositionOut[i] = instance.GetTimecode();
		}
		
		protected override void SetOutputSliceCount(int sliceCount)
		{
			FPositionOut.SliceCount = sliceCount;
		}

        protected override LTCEncoderSignal GetInstance(int i)
		{
			return new LTCEncoderSignal();
		}
	}
}



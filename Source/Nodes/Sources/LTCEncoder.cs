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
		
		public Timecode Timecode
		{
			set
			{
				lock(FEncoder)
				{
					FEncoder.setTimecode(value);
				}
			}
		
			get
			{
				lock(FEncoder)
				{
					return FEncoder.getTimecode();
				}
			}
		}
		
		public void SetFPS(double fps)
		{
			lock(FEncoder)
			{
				FEncoder.setBufferSize(AudioService.Engine.Settings.SampleRate, fps);
			}
		}
		
		byte[] FByteBuffer = new byte[4096];
		float[] FOutFloats = new float[4096];
		
		public bool Play { get; set; }

		public override void Pull(int count)
		{	  
			lock(FEncoder)
			{
				//create samples
				FEncoder.encodeFrame();
				var samples = FEncoder.getBuffer(FByteBuffer, 0);
				
				for (int i = 0; i < samples; i++)
				{
					FOutFloats[i] = (FByteBuffer[i] - 127) / 128.0f;
				}
				
				Write(FOutFloats, 0, samples);
				
				if(Play)
					FEncoder.incrementFrame();
			}
		}
		
		
		public override void Dispose()
		{
			if(FEncoder != null)
				FEncoder.Dispose();
			
			base.Dispose();
		}

	}

	public class LTCEncoderSignal : AudioSignal
	{
		LTCPullBuffer FEncoderRingBuffer;
		
		public LTCEncoderSignal(double fps, TVStandard tvStd, BGFlags bgFlags)
		{
			Init(fps, tvStd, bgFlags);
		}
		
		private struct EncoderParams
		{
		    public double FPS;
		    public TVStandard TVStandard;
		    public BGFlags BGFlags;
		}
		
		EncoderParams LastEncoderParams;
		public void Init(double fps, TVStandard tvStd, BGFlags bgFlags)
		{
		    LastEncoderParams = new EncoderParams { FPS = fps, TVStandard = tvStd, BGFlags = bgFlags };
			if(FEncoderRingBuffer != null)
				FEncoderRingBuffer.Dispose();
			
			var encoder = new Encoder(AudioEngine.Instance.Settings.SampleRate, fps, tvStd, bgFlags);
			FEncoderRingBuffer = new LTCPullBuffer(encoder);
		}
		
        protected override void Engine_SampleRateChanged(object sender, EventArgs e)
        {
            base.Engine_SampleRateChanged(sender, e);
            Init(LastEncoderParams.FPS, LastEncoderParams.TVStandard, LastEncoderParams.BGFlags);
        }
		
		
		public LTCPullBuffer Encoder
		{
			get
			{
				return FEncoderRingBuffer;
			}
		}

		
		protected override void FillBuffer(float[] buffer, int offset, int count)
		{
			FEncoderRingBuffer.Read(buffer, offset, count);
		}
		
		public override void Dispose()
		{
			if(FEncoderRingBuffer != null)
				FEncoderRingBuffer.Dispose();
			base.Dispose();
		}
	}
	
	[PluginInfo(Name = "LTCEncoder", Category = "VAudio", Version = "Source", Help = "Creates a LTC audio signal", AutoEvaluate = true, Tags = "Wave")]
	public class LTCEncoderSignalNode : GenericAudioSourceNodeWithOutputs<LTCEncoderSignal>
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



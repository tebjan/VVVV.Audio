#region usings
using System;
using System.ComponentModel.Composition;
using System.Collections.Generic;
using System.Linq;

using System.Runtime.InteropServices;
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
using LTCSharp;


using VVVV.Core.Logging;
#endregion usings

namespace VVVV.Nodes
{
	public class LTCDecoderSignal : SinkSignal<Timecode>
	{
		LTCSharp.Decoder FDecoder;
		
		public LTCDecoderSignal(AudioSignal input)
		{
			FInput = input;
			FDecoder = new Decoder(AudioEngine.Instance.Settings.SampleRate, 25, 2);
		}
		
		protected override void Engine_SampleRateChanged(object sender, EventArgs e)
		{
			base.Engine_SampleRateChanged(sender, e);
			
			if(FDecoder != null)
				FDecoder.Dispose();
			
			FDecoder = new Decoder(AudioEngine.Instance.Settings.SampleRate, 25, 2);
		}
		
		protected override void FillBuffer(float[] buffer, int offset, int count)
		{
			if(FInput != null)
			{
				FInput.Read(buffer, offset, count);
				
				FDecoder.Write(buffer, count, 0);

				if(FDecoder.GetQueueLength() > 0)
					this.SetLatestValue(FDecoder.Read().getTimecode());
			}
		}
	}
	
	[PluginInfo(Name = "LTCDecoder", Category = "VAudio", Version = "Sink", Help = "Decodes LTC audio signals", Tags = "timecode, SMPTE, synchronization")]
	public class LTCDecoderSignalNode : GenericAudioSinkNodeWithOutputs<LTCDecoderSignal, Timecode>
	{		
		
		[Output("Timecode")]
		public ISpread<Timecode> FTimecodeOut;

        protected override void SetOutputs(int i, LTCDecoderSignal instance)
        {
            if (instance != null)
            {
                Timecode val;
                instance.GetLatestValue(out val);
                FTimecodeOut[i] = val;
            }
            else
            {
            	FTimecodeOut[i] = new Timecode();
            }
        }

        protected override void SetOutputSliceCount(int sliceCount)
        {
            FTimecodeOut.SliceCount = sliceCount;
        }

        protected override LTCDecoderSignal GetInstance(int i)
        {
            return new LTCDecoderSignal(FInputs[i]);
        }

        protected override void SetParameters(int i, LTCDecoderSignal instance)
        {
            instance.Input = FInputs[i];
        }
    }
}



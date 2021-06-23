#region usings
using System;
using System.Linq;
using LTCSharp;
#endregion
namespace VVVV.Audio
{
    public class LTCDecoderSignal : SinkSignal
    {
        LTCSharp.Decoder FDecoder;

        public LTCDecoderSignal(AudioSignal input)
        {
            InputSignal.Value = input;
            FDecoder = new Decoder(AudioService.Engine.Settings.SampleRate, 25, 2);
        }

        protected override void Engine_SampleRateChanged(object sender, EventArgs e)
        {
            base.Engine_SampleRateChanged(sender, e);
            if (FDecoder != null)
                FDecoder.Dispose();
            FDecoder = new Decoder(AudioService.Engine.Settings.SampleRate, 25, 2);
        }

        public Timecode Timecode;
        protected override void FillBuffer(float[] buffer, int offset, int count)
        {
            if (InputSignal.Value != null) {
                InputSignal.Read(buffer, offset, count);
                FDecoder.Write(buffer, count, 0);
                if (FDecoder.GetQueueLength() > 0)
                    Timecode = FDecoder.Read().getTimecode();
            }
        }
    }
}





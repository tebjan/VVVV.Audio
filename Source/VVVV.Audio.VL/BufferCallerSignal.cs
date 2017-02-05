using System;
using System.Collections.Generic;
using VL.Lib.Animation;
using VL.Lib.Collections;
using VL.Lib.VAudio;

namespace VVVV.Audio.Signals
{
    internal class SilenceSignal : AudioSignal
    {

    }

    public class BufferCallerSignal : MultiChannelSignal
    {
        internal AudioBufferStereo StereoBuffer = new AudioBufferStereo();
        Time FTime;
        List<AudioSignal> Input;
        AudioSignal[] SilenceSignals = new AudioSignal[2];

        public BufferCallerSignal()
        {
            Input = new List<AudioSignal>();
            Input.Add(new SilenceSignal());
            Input.Add(new SilenceSignal());
            Input.CopyTo(SilenceSignals);
        }

        protected override void Engine_SampleRateChanged(object sender, EventArgs e)
        {
            base.Engine_SampleRateChanged(sender, e);

            StereoBuffer.SampleRate = SampleRate;
            UpdateBufferTime();
        }

        protected override void Engine_BufferSizeChanged(object sender, EventArgs e)
        {
            base.Engine_BufferSizeChanged(sender, e);

            StereoBuffer.Size = BufferSize;
            UpdateBufferTime();
        }

        Time FBufferTimeIncrement;
        void UpdateBufferTime()
        {
            FBufferTimeIncrement = (double)BufferSize / SampleRate; 
        }

        public Action<AudioBufferStereo> PerBuffer;

        protected override void FillBuffers(float[][] buffer, int offset, int count)
        {
            //lock (FLock)
            {
                var perBuffer = PerBuffer;
                if (perBuffer != null)
                {
                    var leftIn = Input[0];
                    var rightIn = Input[1];
                    leftIn.Read(buffer[0], offset, count);
                    rightIn.Read(buffer[1], offset, count);
                    StereoBuffer.PrepareBuffer(buffer[0], buffer[1], FTime);
                    perBuffer(StereoBuffer);
                }
                else
                {
                    for (int i = 0; i < buffer.Length; i++)
                    {
                        buffer[i].ReadSilence(offset, count);
                    }
                }

                FTime += FBufferTimeIncrement;
            }
        }

        public void SetInput(IEnumerable<AudioSignal> stereoInput)
        {
            var enumerator = stereoInput.GetEnumerator();
            for (int i = 0; i < 2; i++)
            {
                if(enumerator.MoveNext())
                {
                    Input[i] = enumerator.Current ?? SilenceSignals[i];
                }
                else
                {
                    Input[i] = SilenceSignals[i];
                }
            }
        }

        public override void Dispose()
        {
            SilenceSignals[0].Dispose();
            SilenceSignals[1].Dispose();
            base.Dispose();
        }
    }
}

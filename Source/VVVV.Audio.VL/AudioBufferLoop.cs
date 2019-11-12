using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using VL.Core;
using VL.Lib.Animation;

namespace VL.Lib.VAudio
{
    public struct StereoSample
    {
        public readonly float Left;

        public readonly float Right;

        public StereoSample(float left, float right)
        {
            Left = left;
            Right = right;
        }

        public void Split(out float left, out float right)
        {
            left = Left;
            right = Right;
        }
    }

    public class AudioBufferStereo
    {
        private float[] FLeft;
        private float[] FRight;
        internal int Size;
        internal int SampleRate;
        internal Time StartTime;

        public void GetConstants(out int count, out int sampleRate, out Time startTime)
        {
            count = Size;
            sampleRate = SampleRate;
            startTime = StartTime;
        }

        public void PrepareBuffer(float[] left, float[] right, Time startTime)
        {
            FLeft = left;
            FRight = right;
            StartTime = startTime;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetLeftRight(int index, float left, float right)
        {
            FLeft[index] = left;
            FRight[index] = right;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetStereoSample(int index, StereoSample sample)
        {
            FLeft[index] = sample.Left;
            FRight[index] = sample.Right;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StereoSample GetStereoSample(int index)
        {
            return new StereoSample(FLeft[index], FRight[index]);
        }

        public AudioBufferStereo Clone()
        {
            return new AudioBufferStereo() { FLeft = (float[])this.FLeft.Clone(), FRight = (float[])this.FRight.Clone() };
        }
    }

    public class AudioBufferMulti
    {
        private float[][] FBuffers;
        internal int Channels = 2;
        internal int Size;
        internal int SampleRate;
        internal Time StartTime;

        public void GetConstants(out int count, out int channels, out int sampleRate, out Time startTime)
        {
            count = Size;
            channels = Channels;
            sampleRate = SampleRate;
            startTime = StartTime;
        }

        public void PrepareBuffer(float[][] buffers, Time startTime)
        {
            FBuffers = buffers;
            StartTime = startTime;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetSamples(int index, float[] samples)
        {
            for (int i = 0; i < Channels; i++)
            {
                FBuffers[i][index] = samples[i];
            }
        }

        //public AudioBufferStereo Clone()
        //{
        //    return new AudioBufferMultiChannel() { FBuffers = (float[])this.FBuffers.Clone(), FRight = (float[])this.FRight.Clone() };
        //}
    }

    public class AudioBufferLoop<TState, TSampleAccum> : IDisposable
    {
        public readonly AudioSampleFrameClock SampleClock;

        public AudioBufferLoop()
        {
            SampleClock = new AudioSampleFrameClock();
        }

        TState State;

        public void Dispose()
        {
            var disposable = State as IDisposable;
            disposable?.Dispose();
        }

        public TSampleAccum Update(AudioBufferStereo buffer, TSampleAccum input, bool reset, Func<IFrameClock, TState> create, Func<TState, StereoSample, TSampleAccum, int, Tuple<TState, StereoSample, TSampleAccum>> update)
        {
            if (reset || State == null)
                State = create(SampleClock);

            var iterationCount = buffer.Size;
            var timeIncrement = 1.0 / buffer.SampleRate;
            //SampleClock.SetFrameTime(buffer.StartTime);
            var accum = input;
            for (int i = 0; i < iterationCount; i++)
            {
                var result = update(State, buffer.GetStereoSample(i), accum, i);
                State = result.Item1;
                buffer.SetStereoSample(i, result.Item2);
                accum = result.Item3;
                SampleClock.IncrementTime(timeIncrement);
            }

            return input;
        }
    }

    public class AudioBufferLoopMulti<TState, TSampleAccum> : IDisposable
    {
        public readonly AudioSampleFrameClock SampleClock;

        public AudioBufferLoopMulti()
        {
            SampleClock = new AudioSampleFrameClock();
        }

        TState State;

        public void Dispose()
        {
            var disposable = State as IDisposable;
            disposable?.Dispose();
        }

        public TSampleAccum Update(AudioBufferMulti buffer, TSampleAccum input, bool reset, Func<IFrameClock, TState> create, Func<TState, TSampleAccum, int, Tuple<TState, float[], TSampleAccum>> update)
        {
            if (reset || State == null)
                State = create(SampleClock);

            var iterationCount = buffer.Size;
            var timeIncrement = 1.0 / buffer.SampleRate;
            //SampleClock.SetFrameTime(buffer.StartTime);
            var accum = input;
            for (int i = 0; i < iterationCount; i++)
            {
                var result = update(State, accum, i);
                State = result.Item1;
                buffer.SetSamples(i, result.Item2);
                accum = result.Item3;
                SampleClock.IncrementTime(timeIncrement);
            }

            return input;
        }
    }
}

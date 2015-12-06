using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FlareTic.Core.Audio
{
    [Flags()]
    public enum AudioFeaturesFlags
    {
        RMS = 1,
        PeakEnergy = RMS * 2,
        ZeroCrossingRate = PeakEnergy * 2,
        SpectralCentroid = ZeroCrossingRate * 2,
        SpectralCrest = SpectralCentroid * 2,
        SpectralFlatness = SpectralCrest * 2,
        EnergyDifference = SpectralFlatness * 2,
        SpectralDifference = EnergyDifference * 2,
        SpectralDifferenceHWR = SpectralDifference * 2,
        HighFrequencyContent = SpectralDifferenceHWR * 2,
        PitchYin = HighFrequencyContent * 2
    };

    public unsafe class Gist : IDisposable
    {
        private IntPtr nativePointer;
        private int frameSize;

        private float[] spectrumData;
        private float[] featureData = new float[Enum.GetValues(typeof(AudioFeaturesFlags)).Length];

        private static class NativeMethodsx64
        {
            [DllImport("Gist.dll")]
            public static extern IntPtr CreateGistf(int frameSize, int sampleRate);

            [DllImport("Gist.dll")]
            public static extern void DeleteGistf(IntPtr nativePointer);

            [DllImport("Gist.dll")]
            public static extern void ProcessFramef(IntPtr nativePointer, IntPtr audioData, int sampleCount);

            [DllImport("Gist.dll")]
            public static extern void SetFrameSizef(IntPtr nativePointer, int frameSize);

            [DllImport("Gist.dll")]
            public static extern void RetrieveFeaturesf(IntPtr nativePointer, IntPtr featuresPtr, AudioFeaturesFlags flags);

            [DllImport("Gist.dll")]
            public static extern int GetMagnitudeSpectrumf(IntPtr nativePointer, IntPtr dataPtr);
        }

        public Gist(int sampleRate, int frameSize)
        {
            this.nativePointer = NativeMethodsx64.CreateGistf(frameSize,sampleRate);
            this.frameSize = frameSize;
            this.spectrumData = new float[frameSize / 2];
        }

        public int FrameSize
        {
            get { return this.frameSize; }
            set
            {
                if (this.frameSize != value)
                {
                    this.frameSize = value;
                    NativeMethodsx64.SetFrameSizef(this.nativePointer, this.frameSize);
                    this.spectrumData = new float[frameSize / 2];
                }
            }
        }

        public void ProcessFrame(float[] data)
        {
            ProcessFrame(data, data.Length);
        }

        public void ProcessFrame(float[] data, int sampleCount)
        {
            fixed (float* fptr = &data[0])
            {
                NativeMethodsx64.ProcessFramef(this.nativePointer, new IntPtr(fptr), sampleCount);
            }
        }

        public float[] SpectrumData
        {
            get
            {
                fixed (float* fptr = &this.spectrumData[0])
                {
                    int res = NativeMethodsx64.GetMagnitudeSpectrumf(this.nativePointer, new IntPtr(fptr));
                }
                return this.spectrumData;
            }
        }

        public float GetFeature(AudioFeaturesFlags flag)
        {
            fixed (float* fptr = &this.featureData[0])
            {
                NativeMethodsx64.RetrieveFeaturesf(this.nativePointer, new IntPtr(fptr), flag);
            }
            return this.featureData[0];
        }

        public Dictionary<AudioFeaturesFlags, float> GetFeatures(AudioFeaturesFlags flags)
        {
            Dictionary<AudioFeaturesFlags, float> result = new Dictionary<AudioFeaturesFlags, float>();
            fixed (float* fptr = &this.featureData[0])
            {
                NativeMethodsx64.RetrieveFeaturesf(this.nativePointer, new IntPtr(fptr), flags);
            }

            int offset = 0;
            int feature = 1;
            for (int i = 0; i < 11; i++)
            {
                AudioFeaturesFlags f = (AudioFeaturesFlags)feature;
                if (flags.HasFlag(f))
                {
                    result.Add(f, this.featureData[offset]);
                    offset++;
                }
                feature *= 2;
            }
            return result;
        }

        public void Dispose()
        {
            if (nativePointer != IntPtr.Zero)
            {
                NativeMethodsx64.DeleteGistf(this.nativePointer);
                this.nativePointer = IntPtr.Zero;
            }
        }
    }
}

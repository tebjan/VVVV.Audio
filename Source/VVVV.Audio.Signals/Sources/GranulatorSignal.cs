#region usings
using System;
using System.IO;
using NAudio.Utils;
using System.Collections.Generic;
#endregion

namespace VVVV.Audio
{
    internal class RefCounted<T>
    {

        public RefCounted(T value)
        {
            Value = value;
        }

        public readonly T Value;

        public bool IsOrphan => FRefCount <= 0;
        public int RefCount => FRefCount;
        int FRefCount;

        public int Inc()
        {
            return ++FRefCount;
        }

        public int Dec()
        {
            return --FRefCount;
        }
    }

    internal static class GranulatorFilePool
    {
        private static Dictionary<string, RefCounted<AudioFileReaderVVVV>> OpenFiles = new Dictionary<string, RefCounted<AudioFileReaderVVVV>>();

        public static AudioFileReaderVVVV GetOrAddFileReader(string path)
        {
            var file = default(RefCounted<AudioFileReaderVVVV>);

            if (OpenFiles.ContainsKey(path))
            {
                file = OpenFiles[path];
              
            }
            else
            {
                var reader = new AudioFileReaderVVVV(path);
                file = new RefCounted<AudioFileReaderVVVV>(reader);
            }

            file.Inc();
            return file.Value;
        }

        public static bool RemoveFileReader(string path)
        {
            if (OpenFiles.ContainsKey(path))
            {
                return OpenFiles[path].Dec() <= 0;
            }

            return false;
        }

        /// <summary>
        /// Removes files with no reference from RAM
        /// </summary>
        public static void FlushFiles()
        {
            var toRemove = new List<string>();

            foreach (var item in OpenFiles)
            {
                if (item.Value.IsOrphan)
                    toRemove.Add(item.Key);
            }

            foreach (var key in toRemove)
            {
                OpenFiles[key].Value.Dispose();
                OpenFiles.Remove(key);
            }
        }
    }

    public class Grain
    {
        public int SampleRate;
        private int FStart;
        private int FLength;
        public double Index;
        private double delta;
        public float[] Window;
        private int RandomStartOffset;
        private Random Random = new Random();

        public int CurrentIndex => (int)(FStart + RandomStartOffset + Index);

        /// <summary>
        /// Gets or sets the start in the file in seconds
        /// </summary>
        public double Start
        {
            get
            {
                return (double)FStart/SampleRate;
            }

            set
            {
                FStart = (int)(value * SampleRate);
            }
        }

        /// <summary>
        /// Gets or sets the start randomization per cycle in the file in seconds
        /// </summary>
        public double StartRandomization
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the length of the grain
        /// </summary>
        public double Length
        {
            get
            {
                return (double)FLength/SampleRate;
            }
            set
            {
                FLength = Math.Max((int)(value * SampleRate), 1);

                if (Window == null || Window.Length != FLength+1)
                {
                    Window = AudioUtils.CreateWindowFloat(FLength + 1, WindowFunction.Hann); 
                }
            }
        }

        /// <summary>
        /// Gets or sets the playback frequency of the grain in Hz
        /// </summary>
        public double Freq
        {
            get
            {
                return delta * BaseFreq;
            }

            set
            {
                delta = value / BaseFreq;
            }

        }

        /// <summary>
        /// Gets the base frequency in Hz defined by the length
        /// </summary>
        public double BaseFreq
        {
            get
            {
                return SampleRate / (double)FLength;
            }
        }

        public void Inc()
        {
            Index = (Index + delta);

            if(Index > FLength)
            {
                Index = 0;
                if(StartRandomization != 0)
                {
                    RandomStartOffset = (int)(((Random.NextDouble()*2 - 1) * StartRandomization) * SampleRate);
                }
            }
        }
    }

    public class GranulatorSignal : AudioSignal
    {
        SigParam<double> Start = new SigParam<double>("Start");
        SigParam<double> Length = new SigParam<double>("Length");
        SigParam<double> Freq = new SigParam<double>("Frequency");
        SigParam<double> StartRandomization = new SigParam<double>("Start Randomization");

        SigParamDiff<string> FileName = new SigParamDiff<string>("File Name");

        public GranulatorSignal()
        {
            FileName.ValueChanged += FileName_ValueChanged;
        }

        AudioFileReaderVVVV FileReader;

        void FileName_ValueChanged(string newFilename)
        {
            if(FileReader != null)
            {
                GranulatorFilePool.RemoveFileReader(FileReader.FileName);
            }

            if (File.Exists(newFilename))
            {
                FileReader = GranulatorFilePool.GetOrAddFileReader(newFilename);
                FileReader.CacheFile = true;
                FGrain.SampleRate = FileReader.WaveFormat.SampleRate;
            }
        }

        Grain FGrain = new Grain();

        protected override void FillBuffer(float[] buffer, int offset, int count)
        {
            if (FileReader != null)
            {
                var file = FileReader;

                FGrain.Start = Start.Value;
                FGrain.Length = Length.Value;
                FGrain.Freq = Freq.Value;
                FGrain.StartRandomization = StartRandomization.Value;

                for (int i = 0; i < count; i++)
                {
                    buffer[i + offset] = file.Cache[0][FGrain.CurrentIndex] * FGrain.Window[Math.Min(FGrain.Window.Length-1, (int)FGrain.Index)];

                    FGrain.Inc();
                }

            }
            else
            {
                buffer.ReadSilence(offset, count);
            }
        }
    }
}
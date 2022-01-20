#region usings
using System;
using System.Collections.Generic;
using System.Linq;

using NAudio.Wave;
using NAudio.Wave.Asio;
using NAudio.CoreAudioApi;
using System.Reactive.Subjects;


#endregion usings

namespace VVVV.Audio
{
    public class AudioEngine
    {
        //this mixes multiple sample providers from the graph to a waveprovider which is set to
        MasterWaveProvider MasterWaveProvider;
        
        //the driver wrapper
        public AsioOut AsioOut;
        public WasapiOut WasapiOut;
        
        //singleton pattern
        private static AudioEngine SInstance;
        public static AudioEngine Instance
        {
            get
            {
                if(SInstance == null)
                {
                    SInstance = new AudioEngine();
                }
                
                return SInstance;
            }
        }
        
        private AudioEngine()
        {
            Settings = new AudioEngineSettings { SampleRate = 44100, BufferSize = 512 };
            Timer = new AudioEngineTimer(Settings.SampleRate);
            var format = WaveFormat.CreateIeeeFloatWaveFormat(Settings.SampleRate, 1);
            MasterWaveProvider = new MasterWaveProvider(format, samples => OnFinishedReading(samples));
        }
        
        public AudioEngineSettings Settings
        {
            get;
            private set;
        }
        
        private object FTimerLock = new Object();
        public AudioEngineTimer Timer
        {
            get;
            private set;
        }
        
        /// <summary>
        /// the buffers from the audio input
        /// </summary>
        public float[][] InputBuffers
        {
            get
            {
                return FRecordBuffers;
            }
        }
        
        private bool FPlay;
        public bool Play
        {
            set
            {
                FPlay = value;
                if(FPlay) AsioOut.Play();
                else AsioOut.Pause();
            }
            
            get
            {
                return FPlay;
            }
            
        }
        
        public void Stop()
        {
            AsioOut.Stop();
        }

        //tells the subscribers to prepare for the next frame
        public event EventHandler FinishedReading;
        
        protected void OnFinishedReading(int calledSamples)
        {
            var handle = FinishedReading;
            if(handle != null)
                handle(this, new EventArgs());
            
            //lock(FTimerLock) //needed?
            {
                Timer.Progress(calledSamples);
            }
        }
        
        //add/remove outputs
        public void AddOutput(IEnumerable<MasterChannel> provider)
        {
            if(provider != null)
                foreach(var p in provider)
                    MasterWaveProvider.Add(p);
        }
        
        public void RemoveOutput(IEnumerable<MasterChannel> provider)
        {
            if(provider != null)
                foreach(var p in provider)
                    MasterWaveProvider.Remove(p);
        }
        
        //add/remove sinks
        public void AddSink(IAudioSink sink)
        {
            if (sink != null)
                MasterWaveProvider.AddSink(sink);
            
            System.Diagnostics.Debug.WriteLine("Sink Added: " + sink.GetType());
        }
        
        public void RemoveSink(IAudioSink sink)
        {
            if (sink != null)
                MasterWaveProvider.RemoveSink(sink);
            
            System.Diagnostics.Debug.WriteLine("Sink Removed: " + sink.GetType());
        }

        #region asio

        public IObservable<string> DriverSettingsChanged => SettingsChanged;
        private Subject<string> SettingsChanged = new Subject<string>();

        /// <summary>
        /// Initialize the driver in order to be able to read its SampleRate options
        /// </summary>
        /// <param name="driverName"></param>
        public void PreviewDriver(string driverName)
        {
            if (AsioOut == null || AsioOut.DriverName != driverName)
            {
                //dispose device if necessary
                if (this.AsioOut != null)
                    Cleanup();

                //create new driver
                this.AsioOut = new AsioOut(driverName);

                //trigger to update the dynamic SampleRate enum
                SettingsChanged.OnNext(driverName);
            }
        }

        /// <summary>
        /// Initializes the Audio Driver if necessary
        /// </summary>
        /// <param name="driverName"></param>
        /// <param name="sampleRate"></param>
        /// <param name="inputChannels"></param>
        /// <param name="inputChannelOffset"></param>
        /// <param name="outputChannels"></param>
        /// <param name="outputChannelOffset"></param>
        public void ChangeDriverSettings(string driverName, int sampleRate, int inputChannels, int inputChannelOffset, int outputChannels, int outputChannelOffset)
        {
            if (AsioOut == null || AsioOut.DriverName != driverName
               || MasterWaveProvider.WaveFormat.SampleRate != sampleRate
               || AsioOut.NumberOfInputChannels != inputChannels
               || AsioOut.InputChannelOffset != inputChannelOffset
               || AsioOut.NumberOfOutputChannels != outputChannels
               || AsioOut.ChannelOffset != outputChannelOffset)
            {
                //dispose device if necessary
                if (this.AsioOut != null)
                    Cleanup();

                //create new driver
                this.AsioOut = new AsioOut(driverName);

                //set channel offset
                AsioOut.ChannelOffset = outputChannelOffset;
                AsioOut.InputChannelOffset = inputChannelOffset;

                AsioOut.AudioAvailable += AudioEngine_AudioAvailable;
                AsioOut.DriverResetRequest += AsioOut_DriverResetRequest;

                //init driver
                MasterWaveProvider.WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, outputChannels);
                this.AsioOut.InitRecordAndPlayback(MasterWaveProvider, inputChannels, sampleRate);
                
                //register for recording
                FRecordBuffers = new float[AsioOut.DriverInputChannelCount][];
                for (int i = 0; i < FRecordBuffers.Length; i++)
                {
                    FRecordBuffers[i] = new float[512];
                }
                
                Settings.SampleRate = sampleRate;
                Settings.BufferSize = AsioOut.FramesPerBuffer;
                Timer.SampleRate = sampleRate;
                Timer.FillBeatBuffer(AsioOut.FramesPerBuffer);

                this.Settings.SampleRate = sampleRate;

                NeedsReset = false;

                SettingsChanged.OnNext(AsioOut.DriverName);
            }
        }

        //audio input
        protected float[][] FRecordBuffers;
        protected void AudioEngine_AudioAvailable(object sender, AsioAudioAvailableEventArgs e)
        {
            //create buffers if neccessary
            if(FRecordBuffers[0].Length != e.SamplesPerBuffer)
            {
                for (int i = 0; i < FRecordBuffers.Length; i++)
                {
                    FRecordBuffers[i] = new float[e.SamplesPerBuffer];
                }
            }
            
            //fill and convert buffers
            GetInputBuffers(FRecordBuffers, e);
        }
        
        //close
        public void Dispose()
        {
            Cleanup();
        }
        
        //close ASIO
        private void Cleanup()
        {
            if (this.AsioOut != null)
            {
                this.AsioOut.DriverResetRequest -= AsioOut_DriverResetRequest;
                this.AsioOut.AudioAvailable -= AudioEngine_AudioAvailable;
                this.AsioOut.Dispose();
                this.AsioOut = null;
            }
        }

        void AsioOut_DriverResetRequest(object sender, EventArgs e)
        {
            NeedsReset = true;
        }

        /// <summary>
        /// If this is true the engine driver should be reset
        /// </summary>
        public bool NeedsReset
        {
            get;
            private set;
        }

        /// <summary>
        /// Converts all the recorded audio into a buffer of 32 bit floating point samples
        /// </summary>
        /// <samples>The samples as 32 bit floating point, interleaved</samples>
        public int GetInputBuffers(float[][] samples, AsioAudioAvailableEventArgs e)
        {
            int channels = e.InputBuffers.Length;
            unsafe
            {
                if (e.AsioSampleType == AsioSampleType.Int32LSB)
                {
                    for (int ch = 0; ch < channels; ch++)
                    {
                        for (int n = 0; n < e.SamplesPerBuffer; n++)
                        {
                            samples[ch][n] = *((int*)e.InputBuffers[ch] + n) / (float)Int32.MaxValue;
                        }
                    }
                }
                else if (e.AsioSampleType == AsioSampleType.Int16LSB)
                {
                    for (int ch = 0; ch < channels; ch++)
                    {
                        for (int n = 0; n < e.SamplesPerBuffer; n++)
                        {
                            samples[ch][n] = *((short*)e.InputBuffers[ch] + n) / (float)Int16.MaxValue;
                        }
                    }
                }
                else if (e.AsioSampleType == AsioSampleType.Int24LSB)
                {
                    for (int ch = 0; ch < channels; ch++)
                    {
                        for (int n = 0; n < e.SamplesPerBuffer; n++)
                        {
                            byte *pSample = ((byte*)e.InputBuffers[ch] + n * 3);

                            //int sample = *pSample + *(pSample+1) << 8 + (sbyte)*(pSample+2) << 16;
                            int sample = pSample[0] | (pSample[1] << 8) | ((sbyte)pSample[2] << 16);
                            samples[ch][n] = sample / 8388608.0f;
                        }
                    }
                }
                else if (e.AsioSampleType == AsioSampleType.Float32LSB)
                {
                    for (int ch = 0; ch < channels; ch++)
                    {
                        for (int n = 0; n < e.SamplesPerBuffer; n++)
                        {
                            samples[ch][n] = *((float*)e.InputBuffers[ch] + n);
                        }
                    }
                }
                else
                {
                    throw new NotImplementedException(String.Format("ASIO Sample Type {0} not supported", e.AsioSampleType));
                }
            }
            return e.SamplesPerBuffer*channels;
        }
        
        #endregion asio
        
    }

    
    public class AudioEngineSettings
    {
        private int FBufferSize;
        public int BufferSize
        {
            get
            {
                return FBufferSize;
            }
            
            set
            {
                if(FBufferSize != value)
                {
                    FBufferSize = value;
                    OnBufferSizeChanged();
                }
            }
        }
        
        public event EventHandler BufferSizeChanged;
        
        void OnBufferSizeChanged()
        {
            var handler = BufferSizeChanged;
            if(handler != null)
            {
                handler(this, new EventArgs());
            }
        }
        
        private int FSampleRate;
        public int SampleRate
        {
            get
            {
                return FSampleRate;
            }
            
            set
            {
                if(FSampleRate != value)
                {
                    FSampleRate = value;
                    OnSampleRateChanged();
                }
            }
        }
        
        public event EventHandler SampleRateChanged;
        
        void OnSampleRateChanged()
        {
            var handler = SampleRateChanged;
            if(handler != null)
            {
                handler(this, new EventArgs());
            }
        }
    }
    
    /// <summary>
    /// Static and naive access to the AudioEngine
    /// TODO: find better life time management
    /// </summary>
    public static class AudioService
    {
        private static AudioEngine FAudioEngine;
        
        public static AudioEngine Engine
        {
            get
            {
                if(FAudioEngine == null)
                {
                    FAudioEngine = AudioEngine.Instance;
                }
                
                return FAudioEngine;
            }
        }
        
        public static void DisposeEngine()
        {
            FAudioEngine.Dispose();
            FAudioEngine = null;
        }
        
        public static void AddSink(IAudioSink sink)
        {
            FAudioEngine.AddSink(sink);
        }
        
        public static void RemoveSink(IAudioSink sink)
        {
            FAudioEngine.RemoveSink(sink);
        }
        
        public static BufferDictionary BufferStorage = new BufferDictionary();
    }
    
    public class BufferEventArgs : EventArgs
    {
        public float[] Buffer
        {
            get;
            set;
        }
        
        public string BufferName
        {
            get;
            set;
        }
    }
    
    public class BufferDictionary : Dictionary<string, float[]>
    {
        public BufferDictionary()
        {
            
        }
        
        public void SetBuffer(string key, float[] buffer)
        {
            this[key] = buffer;
            
            var handler = BufferSet;
            if(handler != null)
            {
                handler(this, new BufferEventArgs { Buffer = this[key], BufferName = key });
            }
        }
        
        public void RemoveBuffer(string key)
        {
            var buffer = this[key];
            this.Remove(key);
            
            var handler = BufferRemoved;
            if(handler != null)
            {
                handler(this, new BufferEventArgs { Buffer = buffer, BufferName = key });
            }
        }
        
        public event EventHandler<BufferEventArgs> BufferSet;
        public event EventHandler<BufferEventArgs> BufferRemoved;
    }
}
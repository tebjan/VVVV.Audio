#region usings
using System;
using System.Collections.Generic;
using System.Linq;

using NAudio.Wave;
using NAudio.Wave.Asio;
using NAudio.CoreAudioApi;
using System.Reactive.Subjects;
using System.Diagnostics;


#endregion usings

namespace VVVV.Audio
{
    public class AudioEngine
    {
        //this mixes multiple sample providers from the graph to a waveprovider which is set to
        MasterWaveProvider MasterWaveProvider;
        
        //the driver wrapper
        public AsioOut AsioDevice;
        public WasapiInOut WasapiDevice;
        public IWavePlayer CurrentDevice;

        public const string WasapiPrefix = "WASAPI: ";
        public const string WasapiSystemDevice = "Current System Device";
        public const string WasapiLoopbackPrefix = "Loopback: ";
        
        internal AudioEngine()
        {
            Settings = new AudioEngineSettings { SampleRate = 44100, BufferSize = 512 };
            Timer = new AudioEngineTimer(Settings.SampleRate);
            var format = WaveFormat.CreateIeeeFloatWaveFormat(Settings.SampleRate, 1);
            MasterWaveProvider = new MasterWaveProvider(format, OnStartedReading, OnFinishedReading);
        }

        private void OnStartedReading(int samples)
        {
            Settings.BufferSize = samples;
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
                if (FPlay) CurrentDevice.Play();
                else CurrentDevice.Pause();
            }
            
            get
            {
                return FPlay;
            }
            
        }
        
        public void Stop()
        {
            CurrentDevice.Stop();
        }

        //tells the subscribers to prepare for the next frame
        public event EventHandler FinishedReading;
        
        protected void OnFinishedReading(int calledSamples)
        {
            FinishedReading?.Invoke(this, new EventArgs());

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
        /// Initializes the Audio Driver if necessary
        /// </summary>
        /// <param name="driverName"></param>
        /// <param name="sampleRate"></param>
        /// <param name="inputChannels"></param>
        /// <param name="inputChannelOffset"></param>
        /// <param name="outputChannels"></param>
        /// <param name="outputChannelOffset"></param>
        public void ChangeDriverSettings(string driverName, string wasapiRecordingName, int sampleRate, int inputChannels, int inputChannelOffset, int outputChannels, int outputChannelOffset)
        {
            if (driverName.StartsWith(WasapiPrefix))
            {
                driverName = driverName.Replace(WasapiPrefix, "");
                ChangeWASAPIDriverSettings(driverName, wasapiRecordingName, sampleRate, inputChannels, inputChannelOffset, outputChannels, outputChannelOffset);
            }
            else
            {
                ChangeASIODriverSettings(driverName, sampleRate, inputChannels, inputChannelOffset, outputChannels, outputChannelOffset);
            }
        }

        public bool IsSampleRateSupported(int sampleRate)
        {
            if (CurrentDevice is AsioOut asioOut)
            {
                return asioOut.IsSampleRateSupported(sampleRate);
            }
            else if (CurrentDevice is WasapiOut wasapiOut)
            {
                return wasapiOut.OutputWaveFormat.SampleRate == sampleRate;
            }
            return false;
        }

        private void ChangeASIODriverSettings(string driverName, int sampleRate, int inputChannels, int inputChannelOffset, int outputChannels, int outputChannelOffset)
        {
            if (AsioDevice == null || AsioDevice.DriverName != driverName
                           || MasterWaveProvider.WaveFormat.SampleRate != sampleRate
                           || AsioDevice.NumberOfInputChannels != inputChannels
                           || AsioDevice.InputChannelOffset != inputChannelOffset
                           || AsioDevice.NumberOfOutputChannels != outputChannels
                           || AsioDevice.ChannelOffset != outputChannelOffset)
            {
                //dispose device if necessary
                Cleanup();

                //create new driver
                this.AsioDevice = new AsioOut(driverName);

                //set channel offset
                AsioDevice.ChannelOffset = outputChannelOffset;
                AsioDevice.InputChannelOffset = inputChannelOffset;

                //init driver
                MasterWaveProvider.WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, outputChannels);
                this.AsioDevice.InitRecordAndPlayback(MasterWaveProvider, inputChannels, sampleRate);

                //register for recording
                FRecordBuffers = new float[AsioDevice.DriverInputChannelCount][];
                for (int i = 0; i < FRecordBuffers.Length; i++)
                {
                    FRecordBuffers[i] = new float[512];
                }
                this.AsioDevice.AudioAvailable += AsioAudioAvailable;

                Settings.SampleRate = sampleRate;
                Settings.BufferSize = AsioDevice.FramesPerBuffer;
                Timer.SampleRate = sampleRate;
                Timer.FillBeatBuffer(AsioDevice.FramesPerBuffer);

                this.AsioDevice.DriverResetRequest += AsioOut_DriverResetRequest;

                this.Settings.SampleRate = sampleRate;

                NeedsReset = false;

                CurrentDevice = AsioDevice;

                SettingsChanged.OnNext(driverName);
            }
        }

        private void ChangeWASAPIDriverSettings(string driverName, string wasapiRecordingName, int sampleRate, int inputChannels, int inputChannelOffset, int outputChannels, int outputChannelOffset)
        {
            var mmDeviceEnumerator = new MMDeviceEnumerator();
            var allEndpoints = mmDeviceEnumerator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.Active);
            var renderDevices = allEndpoints.Where(d => d.DataFlow == DataFlow.Render);
            var captureDevices = allEndpoints.Where(d => d.DataFlow == DataFlow.Capture);

            MMDevice inputDevice;
            MMDevice outputDevice;

            //find output device
            if (driverName == WasapiSystemDevice)
            {
                if (mmDeviceEnumerator.HasDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia))
                {
                    outputDevice = mmDeviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                    driverName = outputDevice.FriendlyName;
                }
                else
                {
                    outputDevice = renderDevices.FirstOrDefault();
                    driverName = outputDevice?.FriendlyName;
                }
            }
            else
            {
                outputDevice = renderDevices.FirstOrDefault(d => d.FriendlyName == driverName);
            }

            //find input device
            var isLoopback = false;
            if (wasapiRecordingName.StartsWith(WasapiLoopbackPrefix))
            {
                isLoopback = true;
                wasapiRecordingName = wasapiRecordingName.Replace(WasapiLoopbackPrefix, "");
            }

            if (wasapiRecordingName == WasapiSystemDevice)
            {
                var dataFlow = isLoopback ? DataFlow.Render : DataFlow.Capture;
                if (mmDeviceEnumerator.HasDefaultAudioEndpoint(dataFlow, Role.Multimedia))
                {
                    inputDevice = mmDeviceEnumerator.GetDefaultAudioEndpoint(dataFlow, Role.Multimedia);
                    wasapiRecordingName = inputDevice.FriendlyName;
                }
                else
                {
                    inputDevice = (isLoopback ? renderDevices : captureDevices).FirstOrDefault();
                    wasapiRecordingName = inputDevice?.FriendlyName;
                }
            }
            else
            {
                inputDevice = (isLoopback ? renderDevices : captureDevices).FirstOrDefault(d => d.FriendlyName == wasapiRecordingName);
            }

            if (WasapiDevice == null || WasapiDevice.MMOutDevice?.FriendlyName != driverName
                || WasapiDevice.MMInDevice?.FriendlyName != wasapiRecordingName
                || MasterWaveProvider.WaveFormat.SampleRate != sampleRate)
            {
                //dispose device if necessary
                Cleanup();

                //create new driver
                this.WasapiDevice = new WasapiInOut(outputDevice, inputDevice, isLoopback);

                //set channel offset
                //WasapiOut.ChannelOffset = outputChannelOffset;
                //WasapiOut.InputChannelOffset = inputChannelOffset;

                //init driver
                MasterWaveProvider.WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, outputChannels);
                this.WasapiDevice.InitRecordAndPlayback(MasterWaveProvider, inputChannels, sampleRate);

                //get actual samplerate
                sampleRate = WasapiDevice.Output.OutputWaveFormat.SampleRate;
                MasterWaveProvider.WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, outputChannels);

                //register for recording
                FRecordBuffers = new float[WasapiDevice.DriverInputChannelCount][];
                FWasapiInputBuffers.Clear();
                for (int i = 0; i < FRecordBuffers.Length; i++)
                {
                    FRecordBuffers[i] = new float[1];
                    FWasapiInputBuffers.Add(new CircularBuffer(3));

                }
                WasapiDevice.Input.DataAvailable += WasapiAudioAvailable;

                Settings.SampleRate = sampleRate;
                Timer.SampleRate = sampleRate;

                Settings.SampleRate = sampleRate;

                NeedsReset = false;

                CurrentDevice = WasapiDevice.Output;

                SettingsChanged.OnNext(driverName);
            }
        }


        //audio input
        protected float[][] FRecordBuffers;
        protected void AsioAudioAvailable(object sender, AsioAudioAvailableEventArgs e)
        {
            //create buffers if neccessary
            if (FRecordBuffers[0].Length != e.SamplesPerBuffer)
            {
                for (int i = 0; i < FRecordBuffers.Length; i++)
                {
                    FRecordBuffers[i] = new float[e.SamplesPerBuffer];
                }
            }
            
            //fill and convert buffers
            GetInputBuffersAsio(FRecordBuffers, e);
        }

        public List<CircularBuffer> FWasapiInputBuffers = new List<CircularBuffer>();
        private void WasapiAudioAvailable(object sender, WaveInEventArgs e)
        {
            var bytes = e.BytesRecorded;
            var bytesPerSample = WasapiDevice.Input.WaveFormat.BitsPerSample / 8;
            var channels = WasapiDevice.Input.WaveFormat.Channels;
            var samples = bytes / (channels * bytesPerSample);

            if (FRecordBuffers[0].Length < samples)
            {
                for (int i = 0; i < FRecordBuffers.Length; i++)
                {
                    FRecordBuffers[i] = new float[samples];
                    FWasapiInputBuffers[i] = new CircularBuffer(samples);
                }
            }

            //fill and convert buffers
            GetInputBuffersWasapi(FRecordBuffers, samples, e);

            for (int i = 0; i < FRecordBuffers.Length; i++)
            {
                FWasapiInputBuffers[i].Write(FRecordBuffers[i], 0, samples);
            }

        }

        //close
        public void Dispose()
        {
            Cleanup();
        }
        
        //close ASIO
        private void Cleanup()
        {
            if (this.AsioDevice != null)
            {
                this.AsioDevice.DriverResetRequest -= AsioOut_DriverResetRequest;
                this.AsioDevice.AudioAvailable -= AsioAudioAvailable;
                this.AsioDevice.Dispose();
                this.AsioDevice = null;
            }
            if (this.WasapiDevice != null)
            {
                this.WasapiDevice.Input.DataAvailable -= WasapiAudioAvailable;
                this.WasapiDevice.Dispose();
                this.WasapiDevice = null;
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
        public int GetInputBuffersAsio(float[][] samples, AsioAudioAvailableEventArgs e)
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
                    throw new NotImplementedException(string.Format("ASIO Sample Type {0} not supported", e.AsioSampleType));
                }
            }
            return e.SamplesPerBuffer*channels;
        }


        public int GetInputBuffersWasapi(float[][] samples, int sampleCount, WaveInEventArgs e)
        {
            int channels = WasapiDevice.DriverInputChannelCount;
            unsafe
            {
                //if (e.AsioSampleType == AsioSampleType.Int32LSB)
                //{
                //    for (int ch = 0; ch < channels; ch++)
                //    {
                //        for (int n = 0; n < e.SamplesPerBuffer; n++)
                //        {
                //            samples[ch][n] = *((int*)e.InputBuffers[ch] + n) / (float)Int32.MaxValue;
                //        }
                //    }
                //}
                //else if (e.AsioSampleType == AsioSampleType.Int16LSB)
                //{
                //    for (int ch = 0; ch < channels; ch++)
                //    {
                //        for (int n = 0; n < e.SamplesPerBuffer; n++)
                //        {
                //            samples[ch][n] = *((short*)e.InputBuffers[ch] + n) / (float)Int16.MaxValue;
                //        }
                //    }
                //}
                //else if (e.AsioSampleType == AsioSampleType.Int24LSB)
                //{
                //    for (int ch = 0; ch < channels; ch++)
                //    {
                //        for (int n = 0; n < e.SamplesPerBuffer; n++)
                //        {
                //            byte* pSample = ((byte*)e.InputBuffers[ch] + n * 3);

                //            //int sample = *pSample + *(pSample+1) << 8 + (sbyte)*(pSample+2) << 16;
                //            int sample = pSample[0] | (pSample[1] << 8) | ((sbyte)pSample[2] << 16);
                //            samples[ch][n] = sample / 8388608.0f;
                //        }
                //    }
                //}
                //else
                if (WasapiDevice.Input.WaveFormat.BitsPerSample == 32 && WasapiDevice.Input.WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
                {
                    var floatBuffer = new WaveBuffer(e.Buffer).FloatBuffer;
                    for (int ch = 0; ch < channels; ch++)
                    {
                        for (int n = 0; n < sampleCount; n++)
                        {
                            samples[ch][n] = floatBuffer[n * channels + ch];
                        }
                    }
                }
                else
                {
                    throw new NotImplementedException("Input Format Not Supported: " + WasapiDevice.Input.WaveFormat);
                }
            }
            return sampleCount * channels;
        }

        #endregion asio

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

            BufferSet?.Invoke(this, new BufferEventArgs { Buffer = this[key], BufferName = key });
        }
        
        public void RemoveBuffer(string key)
        {
            var buffer = this[key];
            this.Remove(key);

            BufferRemoved?.Invoke(this, new BufferEventArgs { Buffer = buffer, BufferName = key });
        }
        
        public event EventHandler<BufferEventArgs> BufferSet;
        public event EventHandler<BufferEventArgs> BufferRemoved;
    }
}
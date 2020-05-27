#region usings
using System;

using NAudio.Wave;
using NAudio.CoreAudioApi;


#endregion usings

namespace VVVV.Audio
{
    public class WasapiInOut : IDisposable
    {
        public WasapiOut Output;
        public WasapiCapture Input;
        public MMDevice MMOutDevice;
        public MMDevice MMInDevice;

        private bool IsLoopback;

        public bool DeviceAvailable { get; }
        public bool OutputInitialized { get; private set; }
        public bool InputInitialized { get; private set; }

        public int DriverInputChannelCount { get; internal set; } = 2;
        public ISampleProvider InputSampleProvider { get; private set; }

        public WasapiInOut(MMDevice outputDevice, MMDevice inputDevice, bool isLoopback)
        {
            MMOutDevice = outputDevice;
            MMInDevice = inputDevice;
            IsLoopback = isLoopback;
        }

        internal void InitRecordAndPlayback(MasterWaveProvider masterWaveProvider, int inputChannels, int sampleRate)
        {
            var minPeriod = (int)Math.Ceiling(MMInDevice.AudioClient.MinimumDevicePeriod / 10000.0);
            Input = IsLoopback ? new VAudioWasapiLoopbackCapture(MMInDevice, true, minPeriod) : new WasapiCapture(MMInDevice, true, minPeriod);

            minPeriod = (int)Math.Ceiling(MMOutDevice.AudioClient.MinimumDevicePeriod / 10000.0);
            Output = new WasapiOut(MMOutDevice, AudioClientShareMode.Shared, true, minPeriod);

            Output.Init(masterWaveProvider);
            Input.StartRecording();

            OutputInitialized = Output != null;
            InputInitialized = Input != null;

            if (InputInitialized)
            {
                DriverInputChannelCount = Input.WaveFormat.Channels;
            }
        }
        public void Dispose()
        {
            Output?.Dispose();
            Input?.Dispose();
        }
    }
}
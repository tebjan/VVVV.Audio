#region usings
using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;


#endregion usings

namespace VVVV.Audio
{
    /// <summary>
    /// Static and naive access to the AudioEngine
    /// TODO: find better life time management
    /// </summary>
    public static class AudioService
    {
        private static Lazy<AudioEngine> FAudioEngine = new Lazy<AudioEngine>(() => new AudioEngine());

        public static AudioEngine Engine => FAudioEngine.Value;

        public static void DisposeEngine()
        {
            Engine.Dispose();
            FAudioEngine = new Lazy<AudioEngine>(() => new AudioEngine());
        }

        public static void AddSink(IAudioSink sink)
        {
            Engine.AddSink(sink);
        }

        public static void RemoveSink(IAudioSink sink)
        {
            Engine.RemoveSink(sink);
        }

        public static BufferDictionary BufferStorage = new BufferDictionary();

        //device infos


        public static IReadOnlyList<string> OutputDrivers
        {
            get
            {
                EnumerateDevicesIfNecessary();
                return outputDrivers;
            }
            private set => outputDrivers = value;
        }
        public static int OutputDriversDefaultIndex
        {
            get
            {
                EnumerateDevicesIfNecessary();
                return outputDriversDefaultIndex;
            }
            private set => outputDriversDefaultIndex = value;
        }
        public static IReadOnlyList<string> WasapiInputDevices
        {
            get
            {
                EnumerateDevicesIfNecessary();
                return wasapiInputDevices;
            }
            private set => wasapiInputDevices = value;
        }
        public static int WasapiInputDevicesDefaultIndex
        {
            get
            {
                EnumerateDevicesIfNecessary();
                return wasapiInputDevicesDefaultIndex;
            }
            private set => wasapiInputDevicesDefaultIndex = value;
        }
        public static IReadOnlyList<string> WasapiDeviceInfos
        {
            get
            {
                EnumerateDevicesIfNecessary();
                return wasapiDeviceInfos;
            }
            private set => wasapiDeviceInfos = value;
        }

        private static bool DevicesEnumerated;
        private static IReadOnlyList<string> outputDrivers;
        private static int outputDriversDefaultIndex;
        private static IReadOnlyList<string> wasapiInputDevices;
        private static int wasapiInputDevicesDefaultIndex;
        private static IReadOnlyList<string> wasapiDeviceInfos;

        private static void EnumerateDevicesIfNecessary()
        {
            if (DevicesEnumerated)
                return;

            // output drivers
            var asioDrivers = AsioOut.GetDriverNames();
            var mmDeviceEnumerator = new MMDeviceEnumerator();
            var allEndpoints = mmDeviceEnumerator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.Active);
            var renderDevices = allEndpoints.Where(d => d.DataFlow == DataFlow.Render);
            var captureDevices = allEndpoints.Where(d => d.DataFlow == DataFlow.Capture);

            var driverNames = new List<string>();
            foreach (var asioDriverName in asioDrivers)
            {
                driverNames.Add(AudioEngine.ASIOPrefix + asioDriverName);
            }
            OutputDriversDefaultIndex = driverNames.Count; // set to next entry, which is the current system default
            driverNames.Add(AudioEngine.WasapiPrefix + AudioEngine.WasapiSystemDevice);
            foreach (var mmDevice in renderDevices)
            {
                driverNames.Add(AudioEngine.WasapiPrefix + mmDevice.FriendlyName);
            }

            OutputDrivers = driverNames;

            //wasapi inputs
            var wasapiInputDriverNames = new List<string>();
            WasapiInputDevicesDefaultIndex = wasapiInputDriverNames.Count; // set to next entry, which is the current system default
            wasapiInputDriverNames.Add(AudioEngine.WasapiSystemDevice);
            foreach (var mmDevice in captureDevices)
            {
                wasapiInputDriverNames.Add(mmDevice.FriendlyName);
            }

            wasapiInputDriverNames.Add(AudioEngine.WasapiLoopbackPrefix + AudioEngine.WasapiSystemDevice);

            foreach (var mmDevice in renderDevices)
            {
                wasapiInputDriverNames.Add(AudioEngine.WasapiLoopbackPrefix + mmDevice.FriendlyName);
            }

            WasapiInputDevices = wasapiInputDriverNames;

            //build all device infos
            var defaultRender = "";
            if (mmDeviceEnumerator.HasDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia))
                defaultRender = mmDeviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia).FriendlyName;

            var defaultCapture = "";
            if (mmDeviceEnumerator.HasDefaultAudioEndpoint(DataFlow.Capture, Role.Multimedia))
                defaultCapture = mmDeviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Multimedia).FriendlyName;

            var deviceInfos = new List<string>();
            foreach (var ep in allEndpoints)
            {
                var df = ep.DataFlow.ToString();
                var sr = ep.AudioClient.MixFormat.SampleRate / 1000.0f;
                var defaultName = ep.DataFlow == DataFlow.Render ? defaultRender : defaultCapture;
                var def = ep.FriendlyName == defaultName ? " (System Default)" : "";
                deviceInfos.Add(df + " " + sr + ": " + ep.FriendlyName + def);
            }

            WasapiDeviceInfos = deviceInfos;

            DevicesEnumerated = true;
        }

        /// <summary>
        /// Forces a new device enumeration on next acces of any device info property.
        /// </summary>
        public static void InvalidateDeviceEnums()
        {
            DevicesEnumerated = false;
        }
    }
}
#region usings
using System;

using NAudio.Wave;
using NAudio.CoreAudioApi;


#endregion usings

namespace VVVV.Audio
{
    /// <summary>
    /// WASAPI Loopback Capture
    /// based on a contribution from "Pygmy" - http://naudio.codeplex.com/discussions/203605
    /// </summary>
    public class VAudioWasapiLoopbackCapture : WasapiCapture
    {
        /// <summary>
        /// Initialises a new instance of the WASAPI capture class
        /// </summary>
        /// <param name="captureDevice">Capture device to use</param>
        /// <param name="useEventSync">true if sync is done with event. false use sleep.</param>
        /// <param name="audioBufferMillisecondsLength">Length of the audio buffer in milliseconds. A lower value means lower latency but increased CPU usage.</param>
        public VAudioWasapiLoopbackCapture(MMDevice captureDevice, bool useEventSync, int audioBufferMillisecondsLength) :
            base(captureDevice, useEventSync, audioBufferMillisecondsLength)
        {
        }

        /// <summary>
        /// Capturing wave format
        /// </summary>
        public override WaveFormat WaveFormat
        {
            get { return base.WaveFormat; }
            set { throw new InvalidOperationException("WaveFormat cannot be set for WASAPI Loopback Capture"); }
        }

        /// <summary>
        /// Specify loopback
        /// </summary>
        protected override AudioClientStreamFlags GetAudioClientStreamFlags()
        {
            return AudioClientStreamFlags.Loopback;
        }
    }
}
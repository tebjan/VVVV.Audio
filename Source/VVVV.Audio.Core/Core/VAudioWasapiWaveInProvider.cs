#region usings
using NAudio.Wave;


#endregion usings

namespace VVVV.Audio
{
    /// <summary>
    /// Buffered WaveProvider taking source data from WaveIn
    /// </summary>
    public class VAudioWasapiWaveInProvider : IWaveProvider
    {
        private readonly IWaveIn waveIn;

        /// <summary>
        /// Creates a new WaveInProvider
        /// n.b. Should make sure the WaveFormat is set correctly on IWaveIn before calling
        /// </summary>
        /// <param name="waveIn">The source of wave data</param>
        public VAudioWasapiWaveInProvider(IWaveIn waveIn)
        {
            this.waveIn = waveIn;
            waveIn.DataAvailable += OnDataAvailable;
            BufferedWaveProvider = new BufferedWaveProvider(WaveFormat);
        }

        private void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            BufferedWaveProvider.AddSamples(e.Buffer, 0, e.BytesRecorded);
        }

        /// <summary>
        /// Reads data from the WaveInProvider
        /// </summary>
        public int Read(byte[] buffer, int offset, int count)
        {
            return BufferedWaveProvider.Read(buffer, offset, count);
        }

        /// <summary>
        /// The WaveFormat
        /// </summary>
        public WaveFormat WaveFormat => waveIn.WaveFormat;

        public BufferedWaveProvider BufferedWaveProvider { get; }
    }
}
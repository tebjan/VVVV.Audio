using System;
using System.Collections.Generic;
using System.Text;
using NAudio;
using NAudio.Utils;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.Streams;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

namespace VVVV.Nodes
{
    /// <summary>
    /// A sample provider mixer, allowing inputs to be added and removed
    /// </summary>
	
	public struct MixerChannel
	{
		public ISampleProvider sampleProvider;
		public int index;
	}
    public class GainMixingSampleProvider : ISampleProvider
    {
        private List<MixerChannel> sources;
        private WaveFormat waveFormat;
        private float[] sourceBuffer;
        public const int maxInputs = 1024; // protect ourselves against doing something silly
    	
    	
    	public float[] gain = new float[maxInputs];
    	public int gainCount = 1;
        	

        /// <summary>
        /// Creates a new MixingSampleProvider, with no inputs, but a specified WaveFormat
        /// </summary>
        /// <param name="waveFormat">The WaveFormat of this mixer. All inputs must be in this format</param>
        public GainMixingSampleProvider(WaveFormat waveFormat)
        {
            if (waveFormat.Encoding != WaveFormatEncoding.IeeeFloat)
            {
                throw new ArgumentException("Mixer wave format must be IEEE float");
            }
            this.sources = new List<MixerChannel>();
            this.waveFormat = waveFormat;
        	
        	//gainSetup();
        	
        }

    	private void gainSetup()
    	{
    		for(int i = 0; i < maxInputs; i++)
    		{
    			gain[i] = 1.0f;
    		}
    	}

        /// <summary>
        /// When set to true, the Read method always returns the number
        /// of samples requested, even if there are no inputs, or if the
        /// current inputs reach their end. Setting this to true effectively
        /// makes this a never-ending sample provider, so take care if you plan
        /// to write it out to a file.
        /// </summary>

        public bool ReadFully { get; set; }

        /// <summary>
        /// Adds a WaveProvider as a Mixer input.
        /// Must be PCM or IEEE float already
        /// </summary>
        /// <param name="mixerInput">IWaveProvider mixer input</param>
        /*public void AddMixerInput(IWaveProvider mixerInput)
        {
            AddMixerInput(SampleProviderConverters.ConvertWaveProviderIntoSampleProvider(mixerInput));
        }*/

        /// <summary>
        /// Adds a new mixer input
        /// </summary>
        /// <param name="mixerInput">Mixer input</param>
        public void AddMixerInput(MixerChannel mixerInput)
        {
            // we'll just call the lock around add since we are protecting against an AddMixerInput at
            // the same time as a Read, rather than two AddMixerInput calls at the same time
            lock (sources)
            {
                if (this.sources.Count >= maxInputs)
                {
                    throw new InvalidOperationException("Too many mixer inputs");
                }
                this.sources.Add(mixerInput);
            }
            if (this.waveFormat == null)
            {
                this.waveFormat = mixerInput.sampleProvider.WaveFormat;
            }
            else
            {
                if (this.WaveFormat.SampleRate != mixerInput.sampleProvider.WaveFormat.SampleRate ||
                    this.WaveFormat.Channels != mixerInput.sampleProvider.WaveFormat.Channels)
                {
                    throw new ArgumentException("All mixer inputs must have the same WaveFormat");
                }
            }
        }

        /// <summary>
        /// Removes a mixer input
        /// </summary>
        /// <param name="mixerInput">Mixer input to remove</param>
        /*public void RemoveMixerInput(ISampleProvider mixerInput)
        {
            lock (sources)
            {
                this.sources.Remove(mixerInput);
            }
        }*/

        /// <summary>
        /// Removes all mixer inputs
        /// </summary>
        public void RemoveAllMixerInputs()
        {
            lock (sources)
            {
                this.sources.Clear();
            }
        }

        /// <summary>
        /// The output WaveFormat of this sample provider
        /// </summary>
        public WaveFormat WaveFormat
        {
            get { return this.waveFormat; }
        }

        /// <summary>
        /// Reads samples from this sample provider
        /// </summary>
        /// <param name="buffer">Sample buffer</param>
        /// <param name="offset">Offset into sample buffer</param>
        /// <param name="count">Number of samples required</param>
        /// <returns>Number of samples read</returns>
        public int Read(float[] buffer, int offset, int count)
        {
            int outputSamples = 0;
        	float channelGain = 1.0f;
            this.sourceBuffer = BufferHelpers.Ensure(this.sourceBuffer, count);
            lock (sources)
            {
                int index = sources.Count - 1;
                while (index >= 0)
                {
                    var source = sources[index];
                    int samplesRead = source.sampleProvider.Read(this.sourceBuffer, 0, count);
                    int outIndex = offset;
               		channelGain = gain[source.index % gainCount];

                    for (int n = 0; n < samplesRead; n++)
                    {
                        if (n >= outputSamples)
                        {
                            buffer[outIndex++] = this.sourceBuffer[n] * channelGain;
                        }
                        else
                        {
                            buffer[outIndex++] += this.sourceBuffer[n] * channelGain;
                        }
                    }
                    outputSamples = Math.Max(samplesRead, outputSamples);
                    if (samplesRead == 0)
                    {
                        sources.RemoveAt(index);
                    	//we want to loop
                    }
                    index--;
                }
            }
            // optionally ensure we return a full buffer
            if (ReadFully && outputSamples < count)
            {
                int outputIndex = offset + outputSamples;
                while (outputIndex < offset + count)
                {
                    buffer[outputIndex++] = 0;
                }
                outputSamples = count;
            }
            return outputSamples;
        }
    }
}
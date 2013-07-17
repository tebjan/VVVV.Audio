/*
* Created by SharpDevelop.
* User: Tebjan Halm
* Date: 08.04.2012
* Time: 21:53
*
*/

using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using VVVV.Utils.VMath;
using NAudio.Wave;
using NAudio.FileFormats.Wav;

namespace VVVV.Nodes
{

	
	
	/// <summary>
	/// This class supports the reading of WAV files
    /// </summary>
    public class GrainWaveProvider : WaveStream
    {
        private WaveFormat waveFormat;
        private Stream waveStream;
        private bool ownInput;
        private long dataPosition;
        private long dataChunkLength;
        private List<RiffChunk> chunks = new List<RiffChunk>();
    	private float[] FSample;
    	public Grain FGrain;

        /// <summary>Supports opening a WAV file</summary>
        /// <remarks>The WAV file format is a real mess, but we will only
        /// support the basic WAV file format which actually covers the vast
        /// majority of WAV files out there. For more WAV file format information
        /// visit www.wotsit.org. If you have a WAV file that can't be read by
        /// this class, email it to the NAudio project and we will probably
        /// fix this reader to support it
        /// </remarks>
        public GrainWaveProvider(String waveFile) :
            this(File.OpenRead(waveFile))
        {
            ownInput = true;
        }

        /// <summary>
        /// Creates a Wave File Reader based on an input stream
        /// </summary>
        /// <param name="inputStream">The input stream containing a WAV file including header</param>
        public GrainWaveProvider(Stream inputStream)
        {
            this.waveStream = inputStream;
            var chunkReader = new WaveFileChunkReader();
            chunkReader.ReadWaveHeader(inputStream);
            this.waveFormat = chunkReader.WaveFormat;
            this.dataPosition = chunkReader.DataChunkPosition;
            this.dataChunkLength = chunkReader.DataChunkLength;
            this.chunks = chunkReader.RiffChunks;            
        	
            waveStream.Position = dataPosition;
            
            var samples = (dataChunkLength / BlockAlign) * waveFormat.Channels;
        	FSample = new float[samples];
        	
        	for(int i=0; i<samples; i++)
        	{
        		TryReadFloat(out FSample[i]);
        	}
        	
        	waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(waveFormat.SampleRate, waveFormat.Channels);
        	
        	//grain
        	FGrain = new Grain();
        	
        	FGrain.SampleRate = waveFormat.SampleRate;
        	FGrain.Start = 20000;
        	
        	FGrain.Length = 1024;
        	FGrain.Freq = 440;
        	FGrain.Index = 0;
        	
        }


        /// <summary>
        /// Cleans up the resources associated with this WaveFileReader
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Release managed resources.
                if (waveStream != null)
                {
                    // only dispose our source if we created it
                    if (ownInput)
                    {
                        waveStream.Close();
                    }
                    waveStream = null;
                }
            }
            else
            {
                System.Diagnostics.Debug.Assert(false, "WaveFileReader was not disposed");
            }
            // Release unmanaged resources.
            // Set large fields to null.
            // Call Dispose on your base class.
            base.Dispose(disposing);
        }

        /// <summary>
        /// <see cref="WaveStream.WaveFormat"/>
        /// </summary>
        public override WaveFormat WaveFormat
        {
            get
            {
                return waveFormat;
            }
        }

        /// <summary>
        /// This is the length of audio data contained in this WAV file, in bytes
        /// (i.e. the byte length of the data chunk, not the length of the WAV file itself)
        /// <see cref="WaveStream.WaveFormat"/>
        /// </summary>
        public override long Length
        {
            get
            {
                return FSample.Length;
            }
        }

        /// <summary>
        /// Number of Samples (if possible to calculate)
        /// This currently does not take into account number of channels, so
        /// divide again by number of channels if you want the number of 
        /// audio 'frames'
        /// </summary>
        public long SampleCount
        {
            get
            {
                if (waveFormat.Encoding == WaveFormatEncoding.Pcm ||
                    waveFormat.Encoding == WaveFormatEncoding.Extensible ||
                    waveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
                {
                    return dataChunkLength / BlockAlign;
                }
                else
                {
                    // n.b. if there is a fact chunk, you can use that to get the number of samples
                    throw new InvalidOperationException("Sample count is calculated only for the standard encodings");
                }
            }
        }

        /// <summary>
        /// Position in the WAV data chunk.
        /// <see cref="Stream.Position"/>
        /// </summary>
        public override long Position
        {
            get
            {
                return waveStream.Position;
            }
            set
            {
                lock (this)
                {
                    value = Math.Min(value, Length);
                    // make sure we don't get out of sync
                    value -= (value % waveFormat.BlockAlign);
                    waveStream.Position = value + dataPosition;
                }
            }
        }

        
        int FIndex = 0;
        public unsafe override int Read(byte[] array, int offset, int count)
        {
            
            var length = FSample.Length;
            
            count = count / 4;
        	
            fixed(float* sample = FSample)
			{
				fixed(byte* outBuff = array)
				{
					var buf = (float*)outBuff;
					for (int n = 0; n < count; n += waveFormat.Channels)
					{
						FIndex = (int)(FGrain.Start + FGrain.Index * waveFormat.Channels);
						
						buf[n+offset] = sample[FIndex] * FGrain.Window[(int)FGrain.Index];
						buf[n+offset+1] = sample[FIndex+1] * FGrain.Window[(int)FGrain.Index];
						
						FGrain.Inc();
						//FIndex = (FIndex + 2)%length;
					}
				}
			}
        	
            return count;
        }
    	
       
        /// <summary>
        /// Attempts to read a sample into a float. n.b. only applicable for uncompressed formats
        /// Will normalise the value read into the range -1.0f to 1.0f if it comes from a PCM encoding
        /// </summary>
        /// <returns>False if the end of the WAV data chunk was reached</returns>
        public bool TryReadFloat(out float sampleValue)
        {
            sampleValue = 0.0f;
            // 16 bit PCM data
            if (waveFormat.BitsPerSample == 16)
            {
                byte[] value = new byte[2];
                int read = waveStream.Read(value, 0, 2);
                if (read < 2)
                    return false;
                sampleValue = (float)BitConverter.ToInt16(value, 0) / 32768f;
                return true;
            }
            // 24 bit PCM data
            else if (waveFormat.BitsPerSample == 24)
            {
                byte[] value = new byte[3];
                int read = waveStream.Read(value, 0, 3);
                if (read < 3)
                    return false;
                if (value[2] > 0x7f)
                {
                    value[3] = 0xff;
                }
                else
                {
                    value[3] = 0x00;
                }
                sampleValue = (float)BitConverter.ToInt32(value, 0) / (float)(0x800000);
                return true;
            }
            // 32 bit PCM data
            if (waveFormat.BitsPerSample == 32 && waveFormat.Encoding == WaveFormatEncoding.Extensible)
            {
                byte[] value = new byte[4];
                int read = waveStream.Read(value, 0, 4);
                if (read < 4)
                    return false;
                sampleValue = (float)BitConverter.ToInt32(value, 0) / ((float)(Int32.MaxValue) + 1f);
                return true;
            }
            // IEEE float data
            if (waveFormat.BitsPerSample == 32 && waveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
            {
                byte[] value = new byte[4];
                int read = waveStream.Read(value, 0, 4);
                if (read < 4)
                    return false;
                sampleValue = BitConverter.ToSingle(value, 0);
                return true;
            }
            else
            {
                throw new ApplicationException("Only 16, 24 or 32 bit PCM or IEEE float audio data supported");
            }
        }
    }
	
		public static class Windowing
	{
		public static float[] Hamming(int size)
		{
			var ret = new float[size];
			
			for(int i=0; i<size; i++)
			{
				ret[i] = (float)(0.54 - 0.46 * Math.Cos((2*Math.PI*i)/(double)(size-1)));
			}
			
			return ret;
		}
		
		public static float[] Hann(int size)
		{
			var ret = new float[size];
			
			for(int i=0; i<size; i++)
			{
				ret[i] = (float)(0.5 * ( 1 - Math.Cos((2*Math.PI*i)/(double)(size-1))));
			}
			
			return ret;
		}
	}
}

using System;
using NAudio.Wave;
using NAudio.Wave.Compression;
using NAudio.Wave.SampleProviders;

namespace VVVV.Audio
{
    /// <summary>
    /// AudioFileReader simplifies opening an audio file in NAudio
    /// Simply pass in the filename, and it will attempt to open the
    /// file and set up a conversion path that turns into PCM IEEE float.
    /// ACM codecs will be used for conversion.
    /// It provides a volume property and implements both WaveStream and
    /// ISampleProvider, making it possibly the only stage in your audio
    /// pipeline necessary for simple playback scenarios
    /// </summary>
    public class AudioFileReaderVVVV : WaveStream, ISampleProvider
    {
        public string FileName;
        private WaveStream FReaderStream; // the waveStream which we will use for all positioning
        private VolumeSampleProvider FSampleChannel; // sample provider that gives us most stuff we need
        private readonly int FDestBytesPerSample;
        private readonly int FSourceBytesPerSample;
        private readonly long FLength;
        private readonly object FLockObject;
        
        bool FCacheFile;
        public bool CacheFile 
        {
        	get 
        	{ 
        		return FCacheFile; 
        	}
        	set
        	{
        		if(FCacheFile != value)
        		{
        			FCacheFile = value;
        			DoCacheFile();
        		}
        	}
        }
        
        public float[][] Cache;
		void DoCacheFile()
		{
			if(FCacheFile)
			{
                var channels = FSampleChannel.WaveFormat.Channels;
                var cacheSize = FReaderStream.Length / (4 * channels);

                Cache = new float[channels][];
				
				for (int i = 0; i < Cache.Length; i++) 
				{
					Cache[i] = new float[cacheSize];
				}
				
				long totalFloatsRead = 0;
                var buffer = new float[FSampleChannel.WaveFormat.AverageBytesPerSecond * 4 * channels];
                var cacheIndex = 0;
                FReaderStream.Position = 0;

                while (true)
                {
                    int floatsRead = FSampleChannel.Read(buffer, 0, buffer.Length);

                    totalFloatsRead += floatsRead;

                    bool finished = false;
                    for (int i = 0; i < floatsRead; i += channels)
                    {
                        for (int channel = 0; channel < channels; channel++)
                        {
                            Cache[channel][cacheIndex] = buffer[i + channel];
                        }

                        cacheIndex++;
                        if (cacheIndex >= cacheSize)
                        {
                            finished = true;
                            break;
                        }
                    }


                    if (finished)
                    {
                        //end of source
                        break; 
                    }


                    if (totalFloatsRead > Int32.MaxValue)
                    {
                        throw new InvalidOperationException("WAV File cannot be greater than 2GB. Check that sourceProvider is not an endless stream.");
                    }
                }
			}
		}
        
        /// <summary>
        /// Initializes a new instance of AudioFileReader
        /// </summary>
        /// <param name="fileName">The file to open</param>
        public AudioFileReaderVVVV(string fileName)
        {
            FLockObject = new object();
            this.FileName = fileName;
            CreateReaderStream(fileName);
            FSourceBytesPerSample = (FReaderStream.WaveFormat.BitsPerSample / 8) * FReaderStream.WaveFormat.Channels;
            
            this.FSampleChannel = new VolumeSampleProvider(FReaderStream.ToSampleProvider());
            FDestBytesPerSample = 4*FSampleChannel.WaveFormat.Channels;
            FLength = SourceToDest(FReaderStream.Length);
        }

        /// <summary>
        /// Creates the reader stream, supporting all filetypes in the core NAudio library,
        /// and ensuring we are in PCM format
        /// </summary>
        /// <param name="fileName">File Name</param>
        private void CreateReaderStream(string fileName, bool alwaysUseMediaFoundationReader = false)
        {
        	if(alwaysUseMediaFoundationReader)
        	{
        		FReaderStream = new MediaFoundationReader(fileName);
        	}
        	else
        	{
        		if (fileName.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
        		{
        			FReaderStream = new WaveFileReader(fileName);
        			if (FReaderStream.WaveFormat.Encoding != WaveFormatEncoding.Pcm
        			    && FReaderStream.WaveFormat.Encoding != WaveFormatEncoding.IeeeFloat
        			    && FReaderStream.WaveFormat.Encoding != WaveFormatEncoding.Extensible)
        			{
        				FReaderStream = WaveFormatConversionStream.CreatePcmStream(FReaderStream);
        				FReaderStream = new BlockAlignReductionStream(FReaderStream);
        			}

                    OriginalFileFormat = FReaderStream.WaveFormat;
        		}
        		else if (fileName.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
        		{
        			FReaderStream = new Mp3FileReader(fileName);

                    OriginalFileFormat = (FReaderStream as Mp3FileReader).Mp3WaveFormat;
        		}
        		else if (fileName.EndsWith(".aiff", StringComparison.OrdinalIgnoreCase) || fileName.EndsWith(".aif", StringComparison.OrdinalIgnoreCase))
        		{
        			FReaderStream = new AiffFileReader(fileName);

                    OriginalFileFormat = FReaderStream.WaveFormat;
        		}
        		else
        		{
        			// fall back to media foundation reader, see if that can play it
                    FReaderStream = new MediaFoundationReader(fileName, new MediaFoundationReader.MediaFoundationReaderSettings { RepositionInRead = true, RequestFloatOutput = true });
                    OriginalFileFormat = FReaderStream.WaveFormat;
        		}
        	}
        	
            ////needs resampling?
            //if(FReaderStream.WaveFormat.SampleRate != desiredSamplerate)
            //{
            //    var wf = FReaderStream.WaveFormat;
            //    //var targetFormat = WaveFormat.CreateCustomFormat(wf.Encoding, desiredSamplerate, wf.Channels, wf.AverageBytesPerSecond, wf.BlockAlign, 16);
            //    var targetFormat = new WaveFormat(desiredSamplerate, wf.BitsPerSample, wf.Channels);
            //    FReaderStream = new WaveProviderToWaveStream(new MediaFoundationResampler(FReaderStream, targetFormat), FReaderStream);
            //}

            
        }

        public WaveFormat OriginalFileFormat
        {
            get;
            protected set;
        }

        /// <summary>
        /// WaveFormat of this stream
        /// </summary>
        public override WaveFormat WaveFormat
        {
            get { return FSampleChannel.WaveFormat; }
        }

        /// <summary>
        /// Length of this stream (in bytes)
        /// </summary>
        public override long Length
        {
            get { return FLength; }
        }

        /// <summary>
        /// Position of this stream (in bytes)
        /// </summary>
        public override long Position
        {
            get { return SourceToDest(FReaderStream.Position); }
            set 
            { 
            	//lock (FLockObject) 
            	{ 
            		FReaderStream.Position = DestToSource(value); 
            	}  
            }
        }

        /// <summary>
        /// Reads from this wave stream
        /// </summary>
        /// <param name="buffer">Audio buffer</param>
        /// <param name="offset">Offset into buffer</param>
        /// <param name="count">Number of bytes required</param>
        /// <returns>Number of bytes read</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            var waveBuffer = new WaveBuffer(buffer);
            int samplesRequired = count / 4;
            int samplesRead = Read(waveBuffer.FloatBuffer, offset / 4, samplesRequired);
            return samplesRead * 4;
        }

        /// <summary>
        /// Reads audio from this sample provider
        /// </summary>
        /// <param name="buffer">Sample buffer</param>
        /// <param name="offset">Offset into sample buffer</param>
        /// <param name="count">Number of samples required</param>
        /// <returns>Number of samples read</returns>
        public int Read(float[] buffer, int offset, int count)
        {
            //lock (FLockObject)
            {
                return FSampleChannel.Read(buffer, offset, count);
            }
        }

        /// <summary>
        /// Gets or Sets the Volume of this AudioFileReader. 1.0f is full volume
        /// </summary>
        public float Volume
        {
            get { return FSampleChannel.Volume; }
            set { FSampleChannel.Volume = value; } 
        }

        /// <summary>
        /// Helper to convert source to dest bytes
        /// </summary>
        private long SourceToDest(long sourceBytes)
        {
            return FDestBytesPerSample * (sourceBytes / FSourceBytesPerSample);
        }

        /// <summary>
        /// Helper to convert dest to source bytes
        /// </summary>
        private long DestToSource(long destBytes)
        {
            return FSourceBytesPerSample * (destBytes / FDestBytesPerSample);
        }

        /// <summary>
        /// Disposes this AudioFileReader
        /// </summary>
        /// <param name="disposing">True if called from Dispose</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
            	if(FReaderStream != null)
                	FReaderStream.Dispose();
                FReaderStream = null;
            }
            base.Dispose(disposing);
        }
    }
}

using System;
using NAudio.Wave;

namespace VVVV.Audio
{

    public class WaveProviderToWaveStream : WaveStream
    {
        private readonly IWaveProvider FSource;
        private readonly WaveStream FReferenceStream;
        private long FPosition;
        
        public WaveProviderToWaveStream(IWaveProvider source, WaveStream referenceStream)
        {
            this.FSource = source;
            this.FReferenceStream = referenceStream;
        }

        public WaveProviderToWaveStream(IWaveProvider source)
            : this(source, null)
        {
        }

        public override WaveFormat WaveFormat
        {
            get { return FSource.WaveFormat;  }
        }

        /// <summary>
        /// Don't know the real length of the source, just return a big number
        /// </summary>
        public override long Length
        {
            get { return FReferenceStream.Length; }
        }

        public override long Position
        {
            get
            {
//                if(referenceStream != null)
//                    return referenceStream.Position;
//                else
                    // we'll just return the number of bytes read so far
                    return FPosition;
            }
            set
            {
                if(FReferenceStream != null)
                    FReferenceStream.Position = value;
                else
                    FPosition = value;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int read = FSource.Read(buffer, offset, count);
            FPosition += read;
            return read;
        }
    }
}
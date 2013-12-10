using System;
using NAudio.Wave;

namespace VVVV.Audio
{

	public class WaveProviderToWaveStream : WaveStream
	{
		private readonly IWaveProvider source;
		private readonly WaveStream referenceStream;
		private long position;
		
		public WaveProviderToWaveStream(IWaveProvider source, WaveStream referenceStream)
		{
			this.source = source;
			this.referenceStream = referenceStream;
		}

		public WaveProviderToWaveStream(IWaveProvider source)
			: this(source, null)
		{
		}

		public override WaveFormat WaveFormat
		{
			get { return source.WaveFormat;  }
		}

		/// <summary>
		/// Don't know the real length of the source, just return a big number
		/// </summary>
		public override long Length
		{
			get { return Int32.MaxValue; }
		}

		public override long Position
		{
			get
			{
				if(referenceStream != null)
					return referenceStream.Position;
				else
					// we'll just return the number of bytes read so far
					return position;
			}
			set
			{
				if(referenceStream != null)
					referenceStream.Position = value;
				else
					position = value;
			}
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			int read = source.Read(buffer, offset, count);
			position += read;
			return read;
		}
	}
}
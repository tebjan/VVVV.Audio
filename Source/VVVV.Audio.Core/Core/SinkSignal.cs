#region usings
using System;
using System.Linq;
using NAudio.Utils;
#endregion usings

namespace VVVV.Audio
{
	/// <summary>
	/// Interface which the audio engine will call for each buffer after its registered
	/// </summary>
	public interface IAudioSink
	{
		void Read(int offset, int count);
	}
	
	/// <summary>
	/// Base class for all sink signals which have no audio output
	/// </summary>
	public class SinkSignal : AudioSignalInput, IAudioSink
	{
		public SinkSignal()
		{
			AudioService.AddSink(this);
		}
		
		protected float[] FInternalBuffer;
		public void Read(int offset, int count)
		{
			FInternalBuffer = BufferHelpers.Ensure(FInternalBuffer, count);
			base.Read(FInternalBuffer, offset, count);
		}
		
		public override void Dispose()
		{
			AudioService.RemoveSink(this);
			base.Dispose();
		}
	}
}

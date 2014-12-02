#region usings
using System;
using System.Linq;
using NAudio.Utils;
#endregion
namespace VVVV.Audio
{
	public class BufferWriterSignal : BufferAudioSignal, IAudioSink
	{
		public BufferWriterSignal(AudioSignal input, string bufferKey, int previewSize) : base(bufferKey)
		{
			AudioService.AddSink(this);
			InputSignal.Value = input;
			PreviewSize = previewSize;
			if (input == null)
				throw new ArgumentNullException("Input of BufferWriterSignal construcor is null");
		}

		public int WritePosition;

		public float[] Preview = new float[1];

		public int PreviewSize;

		public volatile bool DoRead;

		protected override void FillBuffer(float[] buffer, int offset, int count)
		{
			if (DoRead) {
				InputSignal.Read(buffer, offset, count);
				if (WritePosition >= FBufferSize)
					WritePosition %= FBufferSize;
				var copyCount = Math.Min(FBufferSize - WritePosition, count);
				Array.Copy(buffer, 0, FBuffer, WritePosition, copyCount);
				if (copyCount < count)//copy rest to front
				 {
					Array.Copy(buffer, 0, FBuffer, 0, count - copyCount);
				}
				WritePosition += count;
			}
			//do preview
			if (PreviewSize > 0) {
				if (Preview.Length != PreviewSize)
					Preview = new float[PreviewSize];
				var stepsize = (FBufferSize / PreviewSize) + 1;
				var index = 0;
				for (int i = stepsize / 2; i < FBufferSize; i += stepsize) {
					Preview[index] = FBuffer[i];
					index++;
				}
			}
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





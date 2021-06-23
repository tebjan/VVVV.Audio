#region usings
using System;
using System.Linq;
#endregion
namespace VVVV.Audio
{
    public class BufferReaderSignal : BufferAudioSignal
    {
        public BufferReaderSignal(string bufferKey) : base(bufferKey)
        {
        }

        public bool DoRead;

        public int ReadPosition;

        public int PreviewSize;

        protected override void FillBuffer(float[] buffer, int offset, int count)
        {
            if (DoRead) {
                if (ReadPosition >= FBufferSize)
                    ReadPosition %= FBufferSize;
                var copyCount = Math.Min(FBufferSize - ReadPosition, count);
                Array.Copy(FBuffer, ReadPosition, buffer, 0, copyCount);
                if (copyCount < count)//copy rest from front
                 {
                    Array.Copy(FBuffer, 0, buffer, copyCount, count - copyCount);
                }
                ReadPosition += count;
            }
            else {
                buffer.ReadSilence(offset, count);
            }
        }
    }
}





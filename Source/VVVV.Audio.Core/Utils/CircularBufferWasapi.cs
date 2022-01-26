/*
 * Created by SharpDevelop.
 * User: Tebjan Halm
 * Date: 08.10.2013
 * Time: 10:59
 * 
 * 
 */
using System;
using System.Diagnostics;

using NAudio.Wave;
using NAudio;

namespace VVVV.Audio
{
    
    /// <summary>
    /// Simple wrapper around a float array
    /// </summary>
    public class CircularBufferWasapi
    {
        int FSize;
        float[] FBuffer;
        
        public CircularBufferWasapi(int size)
        {
            Size = size;
        }
        
        public int Size 
        {
            get 
            {
                return FSize; 
            }
            set 
            { 
                if (FSize != value)
                {
                    FBuffer = new float[value];
                    FSize = value;
                    FWritePosition = 0;
                    FirstRead = true;
                }
            }
        }

        /// <summary>
        /// Occurs when the internal buffer was filled completely, e.g. write position has wrapped around.
        /// </summary>
        public Action<float[]> BufferFilled;
        
        int FWritePosition;
        int FReadPosition;
        int FFloatCount;
        public bool FirstRead = true;

        /// <summary>
        /// Writes new data after the latest ones
        /// </summary>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public void Write(float[] data, int offset, int count)
        {
            count = Math.Min(count, FBuffer.Length);
            var samplesWritten = 0;

            // write to end
            int writeToEnd = Math.Min(FBuffer.Length - FWritePosition, count);
            Array.Copy(data, offset, FBuffer, FWritePosition, writeToEnd);
            FWritePosition += writeToEnd;
            FWritePosition %= FBuffer.Length;
            samplesWritten += writeToEnd;
            if (samplesWritten < count)
            {
                BufferFilled?.Invoke(FBuffer);
                Debug.Assert(FWritePosition == 0);
                // must have wrapped round. Write to start
                Array.Copy(data, offset + samplesWritten, FBuffer, FWritePosition, count - samplesWritten);
                FWritePosition += (count - samplesWritten);
                samplesWritten = count;
            }
            FFloatCount = Math.Min(FFloatCount + samplesWritten, FBuffer.Length);
        }
        
        /// <summary>
        /// Starts reading right after the last write position, which is the oldest value
        /// </summary>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public void Read(float[] data, int offset, int count)
        {
            var readPos = FWritePosition;
            int samplesRead = 0;
            int readToEnd = Math.Min(FBuffer.Length - readPos, count);
            Array.Copy(FBuffer, readPos, data, offset, readToEnd);
            samplesRead += readToEnd;
            readPos += readToEnd;
            readPos %= FBuffer.Length;

            if (samplesRead < count)
            {
                // must have wrapped round. Read from start
                Debug.Assert(readPos == 0);
                Array.Copy(FBuffer, readPos, data, offset + samplesRead, count - samplesRead);
                readPos += (count - samplesRead);
                samplesRead = count;
            }

            FFloatCount -= samplesRead;
            Debug.Assert(FFloatCount >= 0);

        }

        /// <summary>
        /// Starts reading where the last Read call left off, but will not read further than the most recent sample.
        /// Will pad with 0 if there are not enough samples available.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public void ReadFromLastPosition(float[] data, int offset, int count)
        {
            var readPosition = FReadPosition;
            //if (FirstRead)
            //    readPosition = AudioUtils.Zmod(FWritePosition - Math.Min(FFloatCount, count), FBuffer.Length);

            var samplesRequested = count;
            count = Math.Min(samplesRequested, FFloatCount);

            int samplesRead = 0;
            int readToEnd = Math.Min(FBuffer.Length - readPosition, count);
            Array.Copy(FBuffer, readPosition, data, offset, readToEnd);
            samplesRead += readToEnd;
            readPosition += readToEnd;
            readPosition %= FBuffer.Length;

            if (samplesRead < count)
            {
                // must have wrapped round. Read from start
                Debug.Assert(readPosition == 0);
                Array.Copy(FBuffer, readPosition, data, offset + samplesRead, count - samplesRead);
                readPosition += (count - samplesRead);
                samplesRead = count;
            }

            if (samplesRequested > samplesRead)
            {
                data.ReadSilence(samplesRead, samplesRequested - samplesRead);
            }
            else
            {
                FReadPosition = readPosition;
            }

            FFloatCount -= samplesRead;
            FirstRead = false;
            Debug.Assert(FFloatCount >= 0);
        }

        /// <summary>
        /// Starts reading right after the last write position, which is the oldest value
        /// </summary>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public void ReadDouble(double[] data, int offset, int count)
        {
            var readPos = FWritePosition;
            for (int i = 0; i < count; i++) 
            {
                readPos++;
                if(readPos >= FSize)
                    readPos = 0;
                
                data[i+offset] = FBuffer[readPos];
            }
        }
        
        /// <summary>
        /// Starts reading right after the last write position, which is the oldest value
        /// </summary>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public void ReadDoubleWindowed(double[] data, double[] window, int offset, int count)
        {
            var readPos = FWritePosition;
            for (int i = 0; i < count; i++) 
            {
                readPos++;
                if(readPos >= FSize)
                    readPos = 0;
                
                data[i+offset] = FBuffer[readPos] * window[i+offset];
            }
        }
    }
}

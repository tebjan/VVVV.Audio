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
    public class CircularBuffer
    {
        int FSize;
        float[] Buffer;
        
        public CircularBuffer(int size)
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
                if(FSize != value)
                {
                    FSize = value;
                    Buffer = new float[value];
                    FWritePos = -1;
                }
            }
        }
        
        
        int FWritePos = -1;
        /// <summary>
        /// Writes new data after the latest ones
        /// </summary>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public void Write(float[] data, int offset, int count)
	    {
            for (int i = 0; i < count; i++) 
            {
                FWritePos++;
                if(FWritePos >= FSize)
                    FWritePos = 0;
                
                Buffer[FWritePos] = data[i+offset];
            }
        }
        
        /// <summary>
        /// Starts reading right after the last write position, which is the oldest value
        /// </summary>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public void Read(float[] data, int offset, int count)
        {
            var readPos = FWritePos;
            for (int i = 0; i < count; i++) 
            {
                readPos++;
                if(readPos >= FSize)
                    readPos = 0;
                
                data[i+offset] = Buffer[readPos];
            }
        }
        
        /// <summary>
        /// Starts reading right after the last write position, which is the oldest value
        /// </summary>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public void ReadDouble(double[] data, int offset, int count)
        {
            var readPos = FWritePos;
            for (int i = 0; i < count; i++) 
            {
                readPos++;
                if(readPos >= FSize)
                    readPos = 0;
                
                data[i+offset] = Buffer[readPos];
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
            var readPos = FWritePos;
            for (int i = 0; i < count; i++) 
            {
                readPos++;
                if(readPos >= FSize)
                    readPos = 0;
                
                data[i+offset] = Buffer[readPos] * window[i+offset];
            }
        }
    }
    
	/// <summary>
	/// A very basic circular buffer implementation
	/// </summary>
	public class CircularPullBuffer : IDisposable
	{
	    private readonly float[] FBuffer;
	    private int FWritePosition;
	    private int FReadPosition;
	    private int FFloatCount;
	
	    /// <summary>
	    /// Create a new circular buffer
	    /// </summary>
	    /// <param name="size">Max buffer size in samples</param>
	    public CircularPullBuffer(int size)
	    {
	    	FBuffer = new float[size];
	    	PullCount = 1024;
	    }
	    
	    public ISampleProvider Input
	    {
	    	get;
	    	set;
	    }
	    
	    /// <summary>
	    /// Write data to the buffer
	    /// </summary>
	    /// <param name="data">Data to write</param>
	    /// <param name="offset">Offset into data</param>
	    /// <param name="count">Number of bytes to write</param>
	    /// <returns>number of bytes written</returns>
	    public int Write(float[] data, int offset, int count)
	    {
	    	var samplesWritten = 0;
	    	if (count > FBuffer.Length - FFloatCount)
	    	{
	    		count = FBuffer.Length - FFloatCount;
	    	}
	    	// write to end
	    	int writeToEnd = Math.Min(FBuffer.Length - FWritePosition, count);
	    	Array.Copy(data, offset, FBuffer, FWritePosition, writeToEnd);
	    	FWritePosition += writeToEnd;
	    	FWritePosition %= FBuffer.Length;
	    	samplesWritten += writeToEnd;
	    	if (samplesWritten < count)
	    	{
	    		Debug.Assert(FWritePosition == 0);
	    		// must have wrapped round. Write to start
	    		Array.Copy(data, offset + samplesWritten, FBuffer, FWritePosition, count - samplesWritten);
	    		FWritePosition += (count - samplesWritten);
	    		samplesWritten = count;
	    	}
	    	FFloatCount += samplesWritten;
	    	return samplesWritten;
	    }
	    
	    /// <summary>
	    /// The amount of data to be pulled
	    /// </summary>
	    public int PullCount { get; set; }
	    
	    protected float[] FTmpBuffer = new float[1];
	    /// <summary>
	    /// Pulls a specified amount of samples from the input.
	    /// </summary>
	    /// <param name="count"></param>
	    public virtual void Pull(int count)
	    {
	    	if(FTmpBuffer.Length != count)
	    		FTmpBuffer = new float[count];
	    	
	    	if(Input != null)
	    	{
	    		Input.Read(FTmpBuffer, 0, count);
	    	}
	    	else
	    	{
	    		FTmpBuffer.ReadSilence(0, count);
	    	}
	    	
	    	Write(FTmpBuffer, 0, count);
	    }
	
	    /// <summary>
	    /// Read from the buffer
	    /// </summary>
	    /// <param name="data">Buffer to read into</param>
	    /// <param name="offset">Offset into read buffer</param>
	    /// <param name="count">Bytes to read</param>
	    /// <returns>Number of bytes actually read</returns>
	    public int Read(float[] data, int offset, int count)
	    {
			//pull in enough samples
	    	while (count > FFloatCount)
	    	{
	    		//count = FFloatCount;
	    		Pull(PullCount);
	    	}
	    	
	    	int samplesRead = 0;
	    	int readToEnd = Math.Min(FBuffer.Length - FReadPosition, count);
	    	Array.Copy(FBuffer, FReadPosition, data, offset, readToEnd);
	    	samplesRead += readToEnd;
	    	FReadPosition += readToEnd;
	    	FReadPosition %= FBuffer.Length;
	    	
	    	if (samplesRead < count)
	    	{
	    		// must have wrapped round. Read from start
	    		Debug.Assert(FReadPosition == 0);
	    		Array.Copy(FBuffer, FReadPosition, data, offset + samplesRead, count - samplesRead);
	    		FReadPosition += (count - samplesRead);
	    		samplesRead = count;
	    	}
	    	
	    	FFloatCount -= samplesRead;
	    	Debug.Assert(FFloatCount >= 0);
	    	return samplesRead;
	    }
	    
	    /// <summary>
	    /// Maximum length of this circular buffer
	    /// </summary>
	    public int MaxLength
	    {
	    	get { return FBuffer.Length; }
	    }
	
	    /// <summary>
	    /// Number of bytes currently stored in the circular buffer
	    /// </summary>
	    public int Count
	    {
	        get { return FFloatCount; }
	    }
	
	    /// <summary>
	    /// Resets the buffer
	    /// </summary>
	    public void Reset()
	    {
	        FFloatCount = 0;
	        FReadPosition = 0;
	        FWritePosition = 0;
	    }
	
	    /// <summary>
	    /// Advances the buffer, discarding bytes
	    /// </summary>
	    /// <param name="count">Bytes to advance</param>
	    public void Advance(int count)
	    {
	        if (count >= FFloatCount)
	        {
	            Reset();
	        }
	        else
	        {
	            FFloatCount -= count;
	            FReadPosition += count;
	            FReadPosition %= MaxLength;
	        }
	    }
	    
	    		
		public virtual void Dispose()
		{
			Input = null;
		}
	}
}

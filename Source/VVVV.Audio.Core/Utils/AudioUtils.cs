/*
 * Created by SharpDevelop.
 * User: Tebjan Halm
 * Date: 08.10.2013
 * Time: 10:59
 * 
 * 
 */
using System;
using System.Collections.Generic;

namespace VVVV.Audio
{
    public enum WindowFunction
	{
		Block,
		Hamming,
		Hann,
		BlackmannHarris
	}
    
	/// <summary>
	/// Audio helpers, all audio samples are considered float32 in the range [-1..1]
	/// </summary>
	public static class AudioUtils
	{
		public static int ReadSilence(this float[] buffer, int offset, int count)
		{
			for (int i = 0; i < count; i++) 
			{
				buffer[i+offset] = 0;
			}
            return count;
		}
		
		public static void ReadDouble(this float[] buffer, double[] dest, int offset, int count)
		{
			for (int i = 0; i < count; i++) 
			{
				dest[i+offset] = buffer[i+offset];
			}
		}
		
		public static void WriteDouble(this float[] buffer, double[] source, int offset, int count)
		{
			for (int i = 0; i < count; i++) 
			{
				buffer[i+offset] = (float)(source[i+offset]);
			}
		}
		
		public static void ResampleMax(float[] source, float[] dest, int outCount)
		{
		    
		    var samples = source.Length;
		    
		    if(samples > outCount)
		    {
    			int blockSize = (int)(samples / outCount);
    			
    			for (int slice = 0; slice < outCount; slice++)
    			{
    			    //do the min/max
    			    var maxValue = 0.0f;
    			    var minValue = 0.0f;
    			    var offset = slice * blockSize;
    			    for (int i = 0; i < blockSize; i++)
    			    {  
    			        maxValue = Math.Max(maxValue, source[i+offset]);
    			        minValue = Math.Min(minValue, source[i+offset]);
    			    }
    			    
    			    dest[slice] = maxValue > -minValue ? maxValue : minValue;
    			}
		    }
		    else if(samples == outCount)
		    {
		        Array.Copy(source, dest, outCount);
		    }
		}
		
		public static IEnumerable<T> Circular<T>(this IEnumerable<T> coll)
		{
		    while(true)
		    {
		        foreach(T t in coll)
		            yield return t;
		    }
		}
		
		/// <summary>
		/// Modulo function with the property, that the remainder of a division z / d
		/// and z &lt; 0 is positive. For example: zmod(-2, 30) = 28.
		/// </summary>
		/// <param name="z"></param>
		/// <param name="d"></param>
		/// <returns>Remainder of division z / d.</returns>
		public static int Zmod(int z, int d)
		{
            if (z >= d)
				return z % d;
			
            if (z < 0)
			{
				int remainder = z % d;
				return remainder == 0 ? 0 : remainder + d;
			}
			
            return z;
		}
		
		public static float Wrap(float x, float min = -1.0f, float max = 1.0f)
		{
		    var range = max - min;

		    if (x > max)
		        return x - range;

		    if (x < min)
		        return x + range;

		    return x;
		}
		
	    /// <summary>
		/// Calcualtes an asymmetric triangle wave
		/// </summary>
		/// <param name="phase">Position in wave, 0..1</param>
		/// <param name="slope">Slope, 0..1, 0.5 is symmetric triangle</param>
		/// <returns></returns>
        public static float Triangle(float phase, float slope = 0.5f)
        {
        	return phase < slope ? (2/slope) * phase - 1 : 1 - (2/(1-slope)) * (phase-slope);
        }

        public static float[] CreateWindowFloat(int size, WindowFunction windowType)
        {
            var ret = new float[size];
            switch (windowType)
            {
                case WindowFunction.Block:
                    for (int i = 0; i < size; i++)
                    {
                        ret[i] = 1;
                    }
                    break;
                case WindowFunction.Hamming:
                    for (int i = 0; i < size; i++)
                    {
                        ret[i] = (float)HammingWindow(i, size);
                    }
                    break;
                case WindowFunction.Hann:
                    for (int i = 0; i < size; i++)
                    {
                        ret[i] = (float)HannWindow(i, size);
                    }
                    break;
                case WindowFunction.BlackmannHarris:
                    for (int i = 0; i < size; i++)
                    {
                        ret[i] = (float)BlackmannHarrisWindow(i, size);
                    }
                    break;
            }

            return ret;
        }

		public static double[] CreateWindowDouble(int size, WindowFunction windowType)
		{
            var ret = new double[size];
            switch (windowType)
            {
                case WindowFunction.Block:
                    for (int i = 0; i < size; i++)
                    {
                        ret[i] = 1;
                    }
                    break;
                case WindowFunction.Hamming:
                    for (int i = 0; i < size; i++)
                    {
                        ret[i] = HammingWindow(i, size);
                    }
                    break;
                case WindowFunction.Hann:
                    for (int i = 0; i < size; i++)
                    {
                        ret[i] = HannWindow(i, size);
                    }
                    break;
                case WindowFunction.BlackmannHarris:
                    for (int i = 0; i < size; i++)
                    {
                        ret[i] = BlackmannHarrisWindow(i, size);
                    }
                    break;
            }

            return ret;
		}
		
		/// <summary>
        /// Applies a Hamming Window
        /// </summary>
        /// <param name="n">Index into frame</param>
        /// <param name="frameSize">Frame size (e.g. 1024)</param>
        /// <returns>Multiplier for Hamming window</returns>
        public static double HammingWindow(int n, int frameSize)
        {
            return 0.54 - 0.46 * Math.Cos((2 * Math.PI * n) / (frameSize - 1));
        }

        /// <summary>
        /// Applies a Hann Window
        /// </summary>
        /// <param name="n">Index into frame</param>
        /// <param name="frameSize">Frame size (e.g. 1024)</param>
        /// <returns>Multiplier for Hann window</returns>
        public static double HannWindow(int n, int frameSize)
        {
            return 0.5 * (1 - Math.Cos((2 * Math.PI * n) / (frameSize - 1)));
        }

        /// <summary>
        /// Applies a Blackman-Harris Window
        /// </summary>
        /// <param name="n">Index into frame</param>
        /// <param name="frameSize">Frame size (e.g. 1024)</param>
        /// <returns>Multiplier for Blackmann-Harris window</returns>
        public static double BlackmannHarrisWindow(int n, int frameSize)
        {
            return 0.35875 - (0.48829 * Math.Cos((2 * Math.PI * n) / (frameSize - 1))) + (0.14128 * Math.Cos((4 * Math.PI * n) / (frameSize - 1))) - (0.01168 * Math.Cos((6 * Math.PI * n) / (frameSize - 1)));
        }
	}
	

}

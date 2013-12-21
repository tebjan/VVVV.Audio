/*
 * Created by SharpDevelop.
 * User: Tebjan Halm
 * Date: 08.10.2013
 * Time: 10:59
 * 
 * 
 */
using System;

namespace VVVV.Audio
{
	/// <summary>
	/// Audio helpers, all audio samples are considered float32 in the range [-1..1]
	/// </summary>
	public static class AudioUtils
	{
		public static double SampleTodBs(double sample, double mindB = -90.0)
		{
			return  Math.Max(20.0 * Math.Log10(Math.Abs(sample)), mindB);
		}
		
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

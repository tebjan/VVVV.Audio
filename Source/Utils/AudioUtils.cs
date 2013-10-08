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
	public sealed class AudioUtils
	{
		public static double SampleTodBs(double sample, double mindB = -90.0)
		{
			return  Math.Max(20.0 * Math.Log10(Math.Abs(sample)), mindB);
		}
	}
}

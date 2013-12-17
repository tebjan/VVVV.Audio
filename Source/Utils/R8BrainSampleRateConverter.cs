/*
 * Created by SharpDevelop.
 * User: Tebjan Halm
 * Date: 17.12.2013
 * Time: 11:19
 * 
 * 
 * Sample rate converter designed by Aleksey Vaneev of Voxengo
 * 
 */
using System;
using System.Runtime.InteropServices;

namespace VVVV.Audio
{
	public enum R8BrainResamplerResolution
	{
		/// <summary>
		/// 16-bit precision resampler
		/// </summary>
		r8brr16 = 0,

		/// <summary>
		/// 16-bit precision resampler for impulse responses
		/// </summary>
		r8brr16IR = 1,
		
		/// <summary>
		/// 24-bit precision resampler (including 32-bit floating point).
		/// </summary>
		r8brr24 = 2
	}
	
	/// <summary>
	/// Managed wrapper class for the high quality sample rate converter r8brain from Aleksey Vaneev of Voxengo
	/// </summary>
	public class R8BrainSampleRateConverter : IDisposable
	{
		/// <summary>
		/// Reference to the actal unmanaged resampler
		/// </summary>
		IntPtr FUnmanagedInstance;
		
		/// <summary>
		/// Function creates a new linear-phase resampler object
		/// </summary>
		/// 
		/// <param name="srcSampleRate">SrcSampleRate Source signal sample rate. Both sample rates can be 
		/// specified as a ratio, e.g. SrcSampleRate = 1.0, DstSampleRate = 2.0.</param>
		/// 
		/// <param name="dstSampleRate">DstSampleRate Destination signal sample rate</param>
		/// 
		/// <param name="maxInputBufferLength">MaxInLen The maximal planned length of the input buffer (in samples) 
		/// that will be passed to the resampler. The resampler relies on this value as
		/// it allocates intermediate buffers. Input buffers longer than this value
		/// should never be supplied to the resampler. Note that the resampler may use
		/// the input buffer itself for intermediate sample data storage.</param>
		/// 
		/// <param name="reqTransBand"></param>
		/// 
		/// <param name="resolution">Resampler's required resolution</param>
		public R8BrainSampleRateConverter(double srcSampleRate,
		                                  double dstSampleRate,
		                                  int maxInputBufferLength,
		                                  double reqTransBand,
		                                  R8BrainResamplerResolution resolution)
		{
			FUnmanagedInstance = R8BrainDLLWrapper.Create(srcSampleRate, dstSampleRate, maxInputBufferLength, reqTransBand, resolution);
		}
		
		/// <summary>
		/// Returns the number of samples that should be passed to the resampler object before the actual output starts
		/// </summary>
		public int Latency
		{
			get
			{
				return R8BrainDLLWrapper.GetLatency(FUnmanagedInstance);
			}
		}
		
		/// <summary>
		/// Function clears (resets) the state of the resampler object and returns it 
		/// to the state after construction. All input data accumulated in the
		/// internal buffer of this resampler object so far will be discarded.
		/// </summary>
		public void Clear()
		{
			R8BrainDLLWrapper.Clear(FUnmanagedInstance);
		}
		
		/// <summary>
		/// Function performs sample rate conversion.
		/// If the source and destination sample rates are equal, the resampler will do
		/// nothing and will simply return the input buffer unchanged.
		/// 
		/// You do not need to allocate an intermediate output buffer for use with this
		/// function. If required, the resampler will allocate a suitable intermediate
		/// output buffer itself.
		/// </summary>
		/// 
		/// <param name="input">Input buffer. This buffer may be used as output buffer by this function.</param>
		/// 
		/// <param name="length">The number of samples available in the input buffer</param>
		/// 
		/// <param name="output">This variable receives the pointer to the resampled data.
		/// This pointer may point to the address within the "ip0" input buffer, or to
		/// *this object's internal buffer. In real-time applications it is suggested
		/// to pass this pointer to the next output audio block and consume any data
		/// left from the previous output audio block first before calling the
		/// process() function again. The buffer pointed to by the "op0" on return may
		/// be owned by the resampler, so it should not be freed by the caller.</param>
		/// 
		/// <returns>The number of samples available in the output buffer. If the
		/// data from the output buffer is going to be written to a bigger output
		/// buffer, it is suggested to check the returned number of samples so that no
		/// overflow of the bigger output buffer happens.</returns>
		public int Process(double[] input, int length, out double[] output)
		{
			return R8BrainDLLWrapper.Process(FUnmanagedInstance, input, length, out output);
		}
		
		#region Dispose pattern with unmanaged resources
		private bool disposed = false;
		
		// Use C# destructor syntax for finalization code.
		~R8BrainSampleRateConverter()
		{
			// Simply call Dispose(false).
			Dispose (false);
		}

		//Implement IDisposable.
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!disposed)
			{
				if (disposing)
				{
					// Free other state (managed objects).
				}
				
				// Free your own state (unmanaged objects).
				// Set large fields to null.
				R8BrainDLLWrapper.Delete(FUnmanagedInstance);
				disposed = true;
			}
		}
		#endregion Dispose pattern with unmanaged resources
	}
	
	/// <summary>
	/// Static wrapper for the r8bsrc.dll
	/// </summary>
	internal static class R8BrainDLLWrapper
	{
		[DllImport("r8bsrc.dll", EntryPoint="r8b_create", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr Create(double SrcSampleRate,
		                                   double DstSampleRate,
		                                   int MaxInLen,
		                                   double ReqTransBand,
		                                   R8BrainResamplerResolution Resolution);
		
		[DllImport("r8bsrc.dll", EntryPoint="r8b_delete", CallingConvention = CallingConvention.Cdecl)]
		public static extern void Delete(IntPtr instance);
		
		[DllImport("r8bsrc.dll", EntryPoint="r8b_get_latency", CallingConvention = CallingConvention.Cdecl)]
		public static extern int GetLatency(IntPtr instance);
		
		[DllImport("r8bsrc.dll", EntryPoint="r8b_clear", CallingConvention = CallingConvention.Cdecl)]
		public static extern void Clear(IntPtr instance);
		
		[DllImport("r8bsrc.dll", EntryPoint="r8b_process", CallingConvention = CallingConvention.Cdecl)]
		public static extern int Process(IntPtr instance,
		                                 IntPtr ip0,
		                                 int length,
		                                 out IntPtr op0);
		
		[DllImport("r8bsrc.dll", EntryPoint="r8b_process", CallingConvention = CallingConvention.Cdecl)]
		public static extern int Process(IntPtr instance,
		                                 double[] ip0,
		                                 int length,
		                                 out double[] op0);
	}
	
}

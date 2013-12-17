/*
 * Created by SharpDevelop.
 * User: Tebjan Halm
 * Date: 17.12.2013
 * Time: 11:19
 * 
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
		IntPtr FUnmanagedInstance;
		
		/// <summary>
		/// Function creates a new linear-phase resampler object
		/// </summary>
		/// <param name="srcSampleRate">SrcSampleRate Source signal sample rate. Both sample rates can be 
		/// specified as a ratio, e.g. SrcSampleRate = 1.0, DstSampleRate = 2.0.</param>
		/// <param name="dstSampleRate">DstSampleRate Destination signal sample rate</param>
		/// <param name="maxInputBufferLength">MaxInLen The maximal planned length of the input buffer (in samples) 
		/// that will be passed to the resampler. The resampler relies on this value as
		/// it allocates intermediate buffers. Input buffers longer than this value
		/// should never be supplied to the resampler. Note that the resampler may use
		/// the input buffer itself for intermediate sample data storage.</param>
		/// <param name="reqTransBand"></param>
		/// <param name="resolution"></param>
		public R8BrainSampleRateConverter(double srcSampleRate,
		                                  double dstSampleRate,
		                                  int maxInputBufferLength,
		                                  double reqTransBand,
		                                  R8BrainResamplerResolution resolution)
		{
			FUnmanagedInstance = R8BrainResamplerDLLWrapper.Create(srcSampleRate, dstSampleRate, maxInputBufferLength, reqTransBand, resolution);
		}
		
		/// <summary>
		/// Returns the number of samples that should be passed to the resampler object before the actual output starts
		/// </summary>
		public int Latency
		{
			get
			{
				return R8BrainResamplerDLLWrapper.GetLatency(FUnmanagedInstance);
			}
		}
		
		/// <summary>
		/// Function clears (resets) the state of the resampler object and returns it 
		/// to the state after construction. All input data accumulated in the
		/// internal buffer of this resampler object so far will be discarded.
		/// </summary>
		public void Clear()
		{
			R8BrainResamplerDLLWrapper.Clear(FUnmanagedInstance);
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
		/// <param name="input">Input buffer. This buffer may be used as output buffer by this function.</param>
		/// <param name="length">The number of samples available in the input buffer</param>
		/// <param name="output">This variable receives the pointer to the resampled data.
		/// This pointer may point to the address within the "ip0" input buffer, or to
		/// *this object's internal buffer. In real-time applications it is suggested
		/// to pass this pointer to the next output audio block and consume any data
		/// left from the previous output audio block first before calling the
		/// process() function again. The buffer pointed to by the "op0" on return may
		/// be owned by the resampler, so it should not be freed by the caller.</param>
		/// <returns>The number of samples available in the output buffer. If the
		/// data from the output buffer is going to be written to a bigger output
		/// buffer, it is suggested to check the returned number of samples so that no
		/// overflow of the bigger output buffer happens.</returns>
		public int Process(double[] input, int length, out double[] output)
		{
			return R8BrainResamplerDLLWrapper.Process(FUnmanagedInstance, input, length, out output);
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
				R8BrainResamplerDLLWrapper.Delete(FUnmanagedInstance);
				disposed = true;
			}
		}
		#endregion Dispose pattern with unmanaged resources
	}
	
	#region DLL documentation

	/*
	 * @file r8bsrc.h
	 *
	 * @brief Inclusion file for use with the "r8bsrc.dll".
	 *
	 * This is the inclusion file for the "r8bsrc.dll" dynamic link library
	 * (the "r8bsrc.lib" library should be included into the project). This DLL
	 * is designed to be used on Windows, on a processor with SSE2 support.
	 * On non-Windows systems it is preferrable to use the C++ library directly.
	 *
	 * Before using this DLL library please read the description of the
	 * r8b::CDSPResampler class and its functions.
	 *
	 * Note that the "int" and "enum" types have 32-bit size on both 32-bit and
	 * 64-bit systems. Pointer types, including the CR8BResampler type, have
	 * 32-bit size on 32-bit system and 64-bit size on 64-bit system.
	 *
	 * r8brain-free-src Copyright (c) 2013 Aleksey Vaneev
	 * See the "License.txt" file for license.
	 *

	#ifndef R8BSRC_INCLUDED
	#define R8BSRC_INCLUDED

	/**
	 * Resampler object handle.
	 *

	typedef void* CR8BResampler;

	/**
	 * Possible resampler object resolutions.
	 *

	enum ER8BResamplerRes
	{
		r8brr16 = 0, ///< 16-bit precision resampler.
		///<
		r8brr16IR = 1, ///< 16-bit precision resampler for impulse responses.
		///<
		r8brr24 = 2 ///< 24-bit precision resampler (including 32-bit floating
			///< point).
			///<
	};

	extern "C" {

		/**
		 * Function creates a new linear-phase resampler object.
		 *
		 * @param SrcSampleRate Source signal sample rate. Both sample rates can
		 * be specified as a ratio, e.g. SrcSampleRate = 1.0, DstSampleRate = 2.0.
		 * @param DstSampleRate Destination signal sample rate.
		 * @param MaxInLen The maximal planned length of the input buffer (in samples)
		 * that will be passed to the resampler. The resampler relies on this value as
		 * it allocates intermediate buffers. Input buffers longer than this value
		 * should never be supplied to the resampler. Note that the resampler may use
		 * the input buffer itself for intermediate sample data storage.
		 * @param Res Resampler's required resolution.
		 *

		CR8BResampler _cdecl r8b_create( const double SrcSampleRate,
		                                const double DstSampleRate, const int MaxInLen,
		                                const double ReqTransBand, const ER8BResamplerRes Res );

		/**
		 * Function deletes a resampler previously created via the r8b_create()
		 * function.
		 *
		 * @param rs Resampler object to delete.
		 *

		void _cdecl r8b_delete( CR8BResampler const rs );

		/**
		 * Function returns the number of samples that should be passed to the
		 * resampler object before the actual output starts.
		 *
		 * @param rs Resampler object.
		 *

		int _cdecl r8b_get_latency( CR8BResampler const rs );

		/**
		 * Function clears (resets) the state of the resampler object and returns it
		 * to the state after construction. All input data accumulated in the
		 * internal buffer of this resampler object so far will be discarded.
		 *
		 * @param rs Resampler object to clear.
		 *

		void _cdecl r8b_clear( CR8BResampler const rs );

		/**
		 * Function performs sample rate conversion.
		 *
		 * If the source and destination sample rates are equal, the resampler will do
		 * nothing and will simply return the input buffer unchanged.
		 *
		 * You do not need to allocate an intermediate output buffer for use with this
		 * function. If required, the resampler will allocate a suitable intermediate
		 * output buffer itself.
		 *
		 * @param ip0 Input buffer. This buffer may be used as output buffer by this
		 * function.
		 * @param l The number of samples available in the input buffer.
		 * @param[out] op0 This variable receives the pointer to the resampled data.
		 * This pointer may point to the address within the "ip0" input buffer, or to
		 * *this object's internal buffer. In real-time applications it is suggested
		 * to pass this pointer to the next output audio block and consume any data
		 * left from the previous output audio block first before calling the
		 * process() function again. The buffer pointed to by the "op0" on return may
		 * be owned by the resampler, so it should not be freed by the caller.
		 * @return The number of samples available in the "op0" output buffer. If the
		 * data from the output buffer "op0" is going to be written to a bigger output
		 * buffer, it is suggested to check the returned number of samples so that no
		 * overflow of the bigger output buffer happens.
		 *

		int _cdecl r8b_process( CR8BResampler const rs, double* const ip0, int l,
		                       double*& op0 );

	} // extern "C"

	#endif // R8BSRC_INCLUDED
		 */
	#endregion
		
	internal static class R8BrainResamplerDLLWrapper
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

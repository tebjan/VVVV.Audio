#region usings
using System;
using System.ComponentModel.Composition;
using System.Collections.Generic;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using NAudio.Wave;
using NAudio.Wave.Asio;
using NAudio.CoreAudioApi;
using NAudio.Wave.SampleProviders;
using NAudio.Utils;


using VVVV.Core.Logging;
#endregion usings

namespace VVVV.Audio
{
	public class AudioSignal : ISampleProvider, IDisposable
	{
		
		public AudioSignal(int sampleRate)
	    {
	        this.WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 1);
	    	AudioService.Engine.FinishedReading += EngineFinishedReading;
	    }
		
		protected void EngineFinishedReading(object sender, EventArgs e)
		{
			FNeedsRead = true;
		}
		
		public virtual void Dispose()
		{
			AudioService.Engine.FinishedReading -= EngineFinishedReading;
		}
		
		
		public AudioSignal Source
		{
			set
			{
				lock(FUpstreamLock)
				{
					FUpstreamProvider = value;
				}
			}
		}
		
		
		protected AudioSignal FUpstreamProvider;
		protected object FUpstreamLock = new object();
		protected float[] FReadBuffer = new float[1];
		
		private bool FNeedsRead = true;
		
		
	    public int Read(float[] buffer, int offset, int count)
	    {
	    	//ensure buffer size
	    	FReadBuffer = BufferHelpers.Ensure(FReadBuffer, count);
	    
	    	//first call per frame
	    	if(FNeedsRead) 
	    	{
	    		this.FillBuffer(FReadBuffer, offset, count);
	    		
	    		FNeedsRead = false;
	    	}
	    	
	    	//every call
	        Array.Copy(FReadBuffer, offset, buffer, offset, count);
	    	
	        return count;
	    }
		
	    /// <summary>
	    /// This method should be overwritten in the sub class to do the actual processing work
	    /// </summary>
	    /// <param name="buffer">The buffer to fill</param>
	    /// <param name="offset">Write offset for the buffer</param>
	    /// <param name="count">Count of samples need</param>
		protected virtual void FillBuffer(float[] buffer, int offset, int count)
		{
			lock(FUpstreamLock)
			{
				if(FUpstreamProvider != null)
					FUpstreamProvider.Read(buffer, offset, count);
			}
		}
	
	    public WaveFormat WaveFormat
	    {
	        get;
	    	protected set;
	    	
	    }
	}
}



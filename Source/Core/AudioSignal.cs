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
		
		public void Dispose()
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
	    	FReadBuffer = BufferHelpers.Ensure(FReadBuffer, count);
	    	if(FNeedsRead)
	    	{
	    		this.FillBuffer(FReadBuffer, offset, count);
	    		
	    		FNeedsRead = false;
	    	}
	    	
	        for (int i = 0; i < count; i++)
	        {
	            buffer[offset + i] = FReadBuffer[i];
	        }
	    	
	        return count;
	    }
		
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



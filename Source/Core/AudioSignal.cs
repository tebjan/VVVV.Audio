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
	public interface ICanCopyBuffer
	{
		bool NeedsBufferCopy
		{
			get;
			set;
		}
	}
	
	public class AudioSignalBase : IDisposable
	{
		public AudioSignalBase()
		{
			AudioService.Engine.FinishedReading += EngineFinishedReading;
			System.Diagnostics.Debug.WriteLine("Signal Created: " + this.GetType());
		}
		
		~AudioSignalBase()
		{
			Dispose();
		}
		
		protected bool FNeedsRead = true;
		protected void EngineFinishedReading(object sender, EventArgs e)
		{
			FNeedsRead = true;
		}
		
		public virtual void Dispose()
		{
			AudioService.Engine.FinishedReading -= EngineFinishedReading;
            System.Diagnostics.Debug.WriteLine("Signal Deleted: " + this.GetType());
		}
	}
	
	public class AudioSignal : AudioSignalBase, ISampleProvider, ICanCopyBuffer
	{
		
		public AudioSignal()
	    {
			AudioService.Engine.Settings.SampleRateChanged += Engine_SampleRateChanged;
			Engine_SampleRateChanged(null, null);
		}

		//set new sample rate
		protected void Engine_SampleRateChanged(object sender, EventArgs e)
		{
			this.SampleRate = AudioService.Engine.Settings.SampleRate;
			this.WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(this.SampleRate, 1);
		}
		
	    public WaveFormat WaveFormat
	    {
	        get;
	    	protected set;
	    	
	    }
	    
	    /// <summary>
	    /// Current sample rate as set by the engine
	    /// </summary>
	    protected int SampleRate;


		protected AudioSignal FSource;
		protected float[] FReadBuffer = new float[1];
		
		public bool NeedsBufferCopy
		{
			get;
			set;
		}

	    public int Read(float[] buffer, int offset, int count)
	    {
	    	//TODO: find solid way to decide whether buffer copy is needed
	    	if(true || NeedsBufferCopy)
	    	{
	    		//ensure buffer size
	    		FReadBuffer = BufferHelpers.Ensure(FReadBuffer, count);
	    		if(FReadBuffer.Length > count)
	    			FReadBuffer = new float[count];
	    		
	    		//first call per frame
	    		if(FNeedsRead)
	    		{
	    			this.FillBuffer(FReadBuffer, offset, count);
	    			FNeedsRead = false;
	    		}
	    		
	    		//every call
	    		Array.Copy(FReadBuffer, offset, buffer, offset, count);
	    	}
	    	else
	    	{
	    		this.FillBuffer(buffer, offset, count);
	    	}
	    	
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
	    	if(FSource != null)
	    		FSource.Read(buffer, offset, count);
	    }
	    
		public override void Dispose()
		{
			AudioService.Engine.Settings.SampleRateChanged -= Engine_SampleRateChanged;
			base.Dispose();
		}
	}
	
	public class AudioSignalSpread : Spread<AudioSignal>, ICanCopyBuffer
	{
		
		public AudioSignalSpread(int count)
			: base(count)
		{
		}
		
		bool FNeedsBufferCopy;
		public bool NeedsBufferCopy
		{
			get 
			{
				return FNeedsBufferCopy;
			}
			set 
			{
				FNeedsBufferCopy = value;
				foreach (var element in this) 
				{
					element.NeedsBufferCopy = FNeedsBufferCopy;
				}
			}
		}
	}
}



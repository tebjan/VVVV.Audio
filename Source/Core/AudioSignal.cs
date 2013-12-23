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
	/// <summary>
	/// Interface which provides buffer copy of the output.
	/// This is needed when multiple signals have the output as input,
	/// so the stream does not advance during frames.
	/// </summary>
	public interface ICanCopyBuffer
	{
		bool NeedsBufferCopy
		{
			get;
			set;
		}
	}
	
	/// <summary>
	/// General base class for all audio signals
	/// </summary>
	public class AudioSignalBase : IDisposable
	{
        //this object is allowed to call Reset, usually the engine itself
        private object FSyncOwner = null;

		public AudioSignalBase()
		{
			AudioService.Engine.FinishedReading += EngineFinishedReading;
            FSyncOwner = AudioService.Engine;
			System.Diagnostics.Debug.WriteLine("Signal Created: " + this.GetType());
		}

        /// <summary>
        /// If a class wants to call buffers not synced to the engine,
        /// it can register itself with this method
        /// </summary>
        /// <param name="newOwner"></param>
        public void TakeOwnership(object newOwner)
        {
            FSyncOwner = newOwner;
        }

        /// <summary>
        /// Sets the sync owner back to the engine
        /// </summary>
        /// <param name="currentOwner">The owner which wants to be released</param>
        public void ReleaseOwnership(object currentOwner)
        {
            if (FSyncOwner == currentOwner)
                FSyncOwner = AudioService.Engine;
        }
		
		protected bool FNeedsRead = true;
		protected void EngineFinishedReading(object sender, EventArgs e)
		{
            Reset(sender);
		}

        /// <summary>
        /// Tells the signal that this frame is over and it should calculate a new buffer
        /// </summary>
        /// <param name="owner"></param>
        public void Reset(object owner)
        {
            if(owner == FSyncOwner)
                FNeedsRead = true;
        }

		public virtual void Dispose()
		{
			AudioService.Engine.FinishedReading -= EngineFinishedReading;
            System.Diagnostics.Debug.WriteLine("Signal Deleted: " + this.GetType());
		}
	}
	
	/// <summary>
	/// Base class for signals which just generate audio
	/// </summary>
	public class AudioSignal : AudioSignalBase, ISampleProvider, ICanCopyBuffer
	{
		
		public AudioSignal()
	    {
			AudioService.Engine.Settings.SampleRateChanged += Engine_SampleRateChanged;
            AudioService.Engine.Settings.BufferSizeChanged += Engine_BufferSizeChanged;
			Engine_SampleRateChanged(null, null);
		}

		//set new sample rate
		protected virtual void Engine_SampleRateChanged(object sender, EventArgs e)
		{
			this.SampleRate = AudioService.Engine.Settings.SampleRate;
			this.WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(this.SampleRate, 1);
		}

        protected virtual void Engine_BufferSizeChanged(object sender, EventArgs e)
        {
            this.BufferSize = AudioService.Engine.Settings.BufferSize;
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
        protected int BufferSize;
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
                //ensure internal buffer size and shrink size if too large
                if (FReadBuffer.Length < count || FReadBuffer.Length > (count * 2))
                {
                    FReadBuffer = new float[count];
                }
	    		
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
	    	buffer.ReadSilence(offset, count);
	    }
	    
		public override void Dispose()
		{
			AudioService.Engine.Settings.SampleRateChanged -= Engine_SampleRateChanged;
            AudioService.Engine.Settings.BufferSizeChanged -= Engine_BufferSizeChanged;
			base.Dispose();
		}

    }
	
	/// <summary>
	/// Base class for audio signals with input
	/// </summary>
	public class AudioSignalInput : AudioSignal
	{
		/// <summary>
		/// The input signal
		/// </summary>
		public AudioSignal Input
		{
			get
			{
				return FInput;
			}
			set
			{
				if(FInput != value)
				{
					FInput = value;
					InputWasSet(value);
				}
			}
		}

		/// <summary>
		/// Override in sub class to know when the input has changed
		/// </summary>
		/// <param name="newInput"></param>
		protected virtual void InputWasSet(AudioSignal newInput)
		{	
		}
		
		protected AudioSignal FInput;

        protected int ReadInput(float[] buffer, int offset, int count)
        {
            if (FInput != null)
            {
                return FInput.Read(buffer, offset, count);
            }
            else
            {
                return buffer.ReadSilence(offset, count);
            }
        }
		
	}
	
	/// <summary>
	/// A generic spread of audio signals
	/// </summary>
	public class AudioSignalSpread<TAudioSignal> : Spread<TAudioSignal>, ICanCopyBuffer where TAudioSignal : AudioSignal
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
	
	/// <summary>
	/// A spread of audio signals
	/// </summary>
	public class AudioSignalSpread : AudioSignalSpread<AudioSignal>
	{
		public AudioSignalSpread(int count)
			: base(count)
		{
		}
	}
}



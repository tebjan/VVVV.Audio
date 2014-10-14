#region usings
using System;
using System.Collections.Generic;
using System.Linq;
using VVVV.PluginInterfaces.V2;

#endregion usings

namespace VVVV.Audio
{
	public class SingleSignal : AudioSignal
	{
		//the read method from the MultiChannelSignal
		protected Action<int, int> FRequestBufferFill;
		public SingleSignal(Action<int, int> read)
		{
			FRequestBufferFill = read;
		}

		public void SetBuffer(float[] buffer)
		{
			FBuffer = buffer;
		}
		
		float[] FBuffer;
		
		protected override void FillBuffer(float[] buffer, int offset, int count)
		{
			FRequestBufferFill(offset, count);
	        Array.Copy(FBuffer, offset, buffer, offset, count);
		}
		
		public override void Dispose()
		{
			FRequestBufferFill = null;
			base.Dispose();
		}
	}
	
	/// <summary>
	/// Processes multiple audio signals
	/// </summary>
	public class MultiChannelSignal : AudioSignal
	{
		protected int FOutputCount;
		public MultiChannelSignal()
		{
			Outputs = new List<AudioSignal>();
			SetOutputCount(2);
		}
		
		protected void SetOutputCount(int newCount)
		{
			//recreate output signals?
			if(FOutputCount != newCount)
			{
				FOutputCount = newCount;
				
				Outputs.ResizeAndDispose(newCount, () => new SingleSignal(Read));

				FReadBuffers = new float[FOutputCount][];
			}
			
			//make sure new buffers get assigned by the manage buffers method
			if(FOutputCount > 0)
			{
				FReadBuffers[0] = new float[0];
			}
		}
		
		public List<AudioSignal> Outputs
		{
			get;
			protected set;
		}
		
		protected float[][] FReadBuffers;
		protected void ManageBuffers(int count)
		{
			if(FReadBuffers[0].Length < count)
			{
				FReadBuffers = new float[FOutputCount][];
				for (int i = 0; i < FOutputCount; i++)
				{
					FReadBuffers[i] = new float[count];
					(Outputs[i] as SingleSignal).SetBuffer(FReadBuffers[i]);
				}
			}
		}
		
		protected void Read(int offset, int count)
		{
			if(FNeedsRead && FOutputCount > 0)
			{
				ManageBuffers(count);
				FillBuffers(FReadBuffers, offset, count);
				FNeedsRead = false;
			}
			
			//since the buffers are already assigned to the SingleSignals nothing more to do
		}
		
		/// <summary>
		/// Does the actual work
		/// </summary>
		/// <param name="buffers"></param>
		/// <param name="offset"></param>
		/// <param name="count"></param>
		protected virtual void FillBuffers(float[][] buffers, int offset, int count)
		{
			
		}
	}
	
	public class MultiChannelInputSignal : MultiChannelSignal
	{
		/// <summary>
		/// The input signal
		/// </summary>
		public ISpread<AudioSignal> Input
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
		protected virtual void InputWasSet(ISpread<AudioSignal> newInput)
		{
		}
		
		protected ISpread<AudioSignal> FInput;
	}
	
	public static class ListExtra
	{
	    public static void ResizeAndDispose<T>(this List<T> list, int newSize, Func<T> create)
	    {
	        int count = list.Count;
	        if(newSize < count)
	        {
	            var itemCount = count - newSize;
	            var toRemove = list.GetRange(newSize, itemCount);
	            toRemove.Reverse();
	            foreach(var item in toRemove)
	            {
	                list.Remove(item);
	                var disposable = item as IDisposable;
	                if(item != null)
	                    disposable.Dispose();
	            }
	        }
	        else if(newSize > count)
	        {
	            var itemCount = newSize - count;
	            
	            for(int i=0; i < itemCount; i++)
	            {
	                list.Add(create());
	            }
	        }
	    }
	}
}



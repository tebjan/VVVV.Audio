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
	}
	
	/// <summary>
	/// Processes multiple audio signals
	/// </summary>
	public class MultiChannelSignal : AudioSignal
	{
		protected int FOutputCount;
		public MultiChannelSignal(int outputCount)
		{
			Outputs = new Spread<AudioSignal>();
			SetOutputCount(outputCount);
		}
		
		protected void SetOutputCount(int newCount)
		{
			//recreate output signals?
			if(FOutputCount != newCount)
			{
				FOutputCount = newCount;
				
				Outputs.ResizeAndDispose(newCount, () => { return new SingleSignal(Read); });

				FReadBuffers = new float[FOutputCount][];
			}
			
			//make sure new buffers get assigned by the manage buffers method
			FReadBuffers[0] = new float[0];
		}
		
		public ISpread<AudioSignal> Outputs
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
			if(FNeedsRead)
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
		protected ISpread<AudioSignal> FInput;
		
		public MultiChannelInputSignal(ISpread<AudioSignal> inputs, int outputCount)
			: base(outputCount)
		{
			FInput = inputs;
		}
	}
}



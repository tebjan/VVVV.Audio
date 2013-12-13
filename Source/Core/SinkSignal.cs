#region usings
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

using NAudio.CoreAudioApi;
using NAudio.Utils;
using NAudio.Wave;
using NAudio.Wave.Asio;
using NAudio.Wave.SampleProviders;
using VVVV.Core.Logging;
using VVVV.PluginInterfaces.V2;

#endregion usings

namespace VVVV.Audio
{
	/// <summary>
	/// Interface which the audio engine will call for each buffer after its registered
	/// </summary>
	public interface IAudioSink
	{
		void Read(int offset, int count);
	}
	
	/// <summary>
	/// Base class for all sink signals which have no audio output
	/// </summary>
	public class SinkSignal<TValue> : AudioSignal, IAudioSink
	{
		public SinkSignal(int sampleRate)
		{
			AudioService.AddSink(this);
		}
		
		private volatile bool FReading;
		private volatile bool FWriting;
		private TValue FValueToPass;
		private TValue FLastValue;
		
		public bool GetLatestValue(out TValue value)
		{
			var success = false;
			FReading = true;
			if (!FWriting)
			{
				FLastValue = FValueToPass;
				success = true;
			}
			else
			{
				System.Diagnostics.Debug.WriteLine("Could not read");
			}
			
			value = FLastValue;
			FReading = false;
			return success;
		}
		
		protected bool SetLatestValue(TValue newValue)
		{
			var success = false;
			FWriting = true;
			if (!FReading)
			{
				FValueToPass = newValue;
				success = true;
			}
			else
			{
				System.Diagnostics.Debug.WriteLine("Could not write");
			}
			FWriting = false;
			return success;
		}
		
		protected float[] FInternalBuffer;
		public void Read(int offset, int count)
		{
			FInternalBuffer = BufferHelpers.Ensure(FInternalBuffer, count);
			base.Read(FInternalBuffer, offset, count);
		}
		
		public override void Dispose()
		{
			AudioService.RemoveSink(this);
			base.Dispose();
		}
	}
}

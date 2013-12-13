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
	public interface IAudioSink
	{
		void Read(int offset, int count);
	}
	
	public class SinkSignal<TValue> : AudioSignal, IAudioSink
	{
		public SinkSignal(int sampleRate)
			: base(sampleRate)
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
				value = FValueToPass;
				success = true;
			}
			else
			{
				value = FLastValue;
				System.Diagnostics.Debug.WriteLine("Could not read");
			}
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
			base.Dispose();
			AudioService.RemoveSink(this);
		}
	}
}

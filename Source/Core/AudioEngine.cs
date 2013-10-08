#region usings
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

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
	
	public enum AudioOutType
	{
		Asio,
		Wasapi,
		DirectSound,
		Wave
	}
	
	public interface IAudioSink
	{
		void Read(int offset, int count);
	}
	
	public class SinkSignal<TStack> : AudioSignal, IAudioSink, IDisposable
	{
		protected ConcurrentStack<TStack> FStack = new ConcurrentStack<TStack>();

		public SinkSignal(int sampleRate)
			: base(sampleRate)
		{
			AudioService.AddSink(this);
		}
		
		private TStack FLastValue;
		public bool GetLatestValue(out TStack value)
		{
			if(FStack.IsEmpty)
			{
				value = FLastValue;
				return true;
			}
			
			var ret = FStack.TryPeek(out value);
			FLastValue = value;
			FStack.Clear();
			return ret;
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
	
	public class AudioEngine: IDisposable
	{
		//this mixes multiple sample providers from the graph to a waveprovider which is set to
		MasterWaveProvider MasterWaveProvider;
		
		//the driver wrapper
		public AsioOut AsioOut;
		
		public AudioEngine()
		{
			Settings = new AudioEngineSettings { SampleRate = 44100, BufferSize = 512 };
			Timer = new AudioEngineTimer(Settings.SampleRate);
			var format = WaveFormat.CreateIeeeFloatWaveFormat(Settings.SampleRate, 1);
			MasterWaveProvider = new MasterWaveProvider(format, samples => OnFinishedReading(samples));
		}
		
		public AudioEngineSettings Settings
		{
			get;
			private set;
		}
		
		private object FTimerLock = new Object();
		public AudioEngineTimer Timer
		{
			get;
			private set;
		}
		
		private bool FPlay;
		public bool Play
		{
			set
			{
				FPlay = value;
				if(FPlay) AsioOut.Play();
				else AsioOut.Pause();
			}
			
			get
			{
				return FPlay;
			}
			
		}
		
		public void Stop()
		{
			AsioOut.Stop();
		}

		//tells the subscribers to prepare for the next frame
		public event EventHandler FinishedReading;
		
		protected void OnFinishedReading(int calledSamples)
		{
			var handle = FinishedReading;
			if(handle != null)
				handle(this, new EventArgs());
			
			//lock(FTimerLock) //needed?
			{
				Timer.Progress(calledSamples);
			}
		}
		
		//add/remove outputs
		public void AddOutput(IEnumerable<MasterChannel> provider)
		{
			if(provider != null)
				foreach(var p in provider)
					MasterWaveProvider.Add(p);
		}
		
		public void RemoveOutput(IEnumerable<MasterChannel> provider)
		{
			if(provider != null)
				foreach(var p in provider)
					MasterWaveProvider.Remove(p);
		}
		
		//add/remove sinks
		public void AddSink(IAudioSink sink)
		{
			if (sink != null)
				MasterWaveProvider.AddSink(sink);
		}
		
		public void RemoveSink(IAudioSink sink)
		{
			if (sink != null)
				MasterWaveProvider.RemoveSink(sink);
		}
		
		#region asio
		
		/// <summary>
		/// Set the Driver name, this initializes the output driver
		/// </summary>
		public string DriverName
		{
			get
			{
				return AsioOut.DriverName;
			}
			
			set
			{
				if(AsioOut == null || AsioOut.DriverName != value)
				{ 
					CreateAsio(value);
				}
			}
		}
		
		//init driver
		private void CreateAsio(string driverName)
		{
			//dispose device if necessary
			if (this.AsioOut != null)
			{
				Cleanup();
			}
			
			this.AsioOut = new AsioOut(driverName);
			MasterWaveProvider.WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, AsioOut.DriverOutputChannelCount);
			this.AsioOut.Init(MasterWaveProvider);
		}
		
		//close
		public void Dispose()
		{
			Cleanup();
		}
		
		//close ASIO
		private void Cleanup()
		{
			if (this.AsioOut != null)
			{
				this.AsioOut.Dispose();
				this.AsioOut = null;
			}
		}
		
		public string[] AsioDriverNames
		{
			get
			{
				return AsioOut.GetDriverNames();
			}
		}
		
		#endregion asio
		
	}
	

	
	public class AudioEngineSettings
	{
		private int FBufferSize;
		public int BufferSize
		{
			get
			{
				return FBufferSize;
			}
			
			set
			{
				FBufferSize = value;
				OnBufferSizeChanged();
			}
		}
		
		public event EventHandler BufferSizeChanged;
		
		void OnBufferSizeChanged()
		{
			var handler = BufferSizeChanged;
			if(handler != null)
			{
				handler(this, new EventArgs());
			}
		}
		
		private int FSampleRate;
		public int SampleRate
		{
			get
			{
				return FSampleRate;
			}
			
			set
			{
				FSampleRate = value;
				OnSampleRateChanged();
			}
		}
		
		public event EventHandler SampleRateChanged;
		
		void OnSampleRateChanged()
		{
			var handler = SampleRateChanged;
			if(handler != null)
			{
				handler(this, new EventArgs());
			}
		}
	}
	
	/// <summary>
	/// Static and naive access to the AudioEngine
	/// TODO: find better life time management
	/// </summary>
	public static class AudioService
	{
		private static AudioEngine FAudioEngine;
		
		public static AudioEngine Engine
		{
			get
			{
				if(FAudioEngine == null)
				{
					FAudioEngine = new AudioEngine();
				}
				
				return FAudioEngine;
			}
		}
		
		public static void DisposeEngine()
		{
			FAudioEngine.Dispose();
		}
		
		public static void AddSink(IAudioSink sink)
		{
			FAudioEngine.AddSink(sink);
		}
		
		public static void RemoveSink(IAudioSink sink)
		{
			FAudioEngine.RemoveSink(sink);
		}
	}
}
#region usings
using System;
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
	
	
	public class AudioEngine: IDisposable
	{
		public AudioEngine()
		{
			Settings = new AudioEngineSettings { SampleRate = 44100, BufferSize = 512 };
			var format = WaveFormat.CreateIeeeFloatWaveFormat(Settings.SampleRate, 1);
			MasterWaveProvider = new MasterWaveProvider(format, () => OnFinishedReading());
		}
		
		public AudioEngineSettings Settings
		{
			get;
			private set;
		}
		
		MasterWaveProvider MasterWaveProvider;
		public AsioOut AsioOut;
		
		public event EventHandler FinishedReading;
		
		protected void OnFinishedReading()
		{
			var handle = FinishedReading;
			if(handle != null)
				handle(this, new EventArgs());
		}
		
		public void AddOutput(IEnumerable<AudioSignal> provider)
		{
			if(provider == null) return;
			foreach(var p in provider)
				MasterWaveProvider.Add(p);
		}
		
		public void RemoveOutput(IEnumerable<AudioSignal> provider)
		{
			if(provider == null) return;
			foreach(var p in provider)
				MasterWaveProvider.Remove(p);
		}
		
		#region asio
		
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
	}
}



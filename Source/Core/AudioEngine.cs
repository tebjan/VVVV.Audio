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
		
		public int SampleRate
		{
			get;
			set;
		}
	}
	
	
	public class AudioEngine: IDisposable, IStartable
	{
		public AudioEngine()
		{
			var format = WaveFormat.CreateIeeeFloatWaveFormat(44100, 1);
			MultiInputProvider = new MultipleSampleToWaveProvider(format, () => OnFinishedReading());
		}
		
		MultipleSampleToWaveProvider MultiInputProvider;
		public AsioOut AsioOut;
		
		public event EventHandler FinishedReading;
		
		protected void OnFinishedReading()
		{
			var handle = FinishedReading;
			if(handle != null)
				handle(this, new EventArgs());
		}
		
		#region asio
		
		public string DriverName
		{
			get;
			set;
		}
		
		//init driver
		public void CreateAsio()
		{
			//recreate device if necessary
			if (this.AsioOut != null)
			{
				Cleanup();
			}
			
			this.AsioOut = new AsioOut(DriverName);
			
			this.AsioOut.Init(MultiInputProvider);
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
		
		#region IStatable
		//Needed for IStartable. Do nothing right now...
		public void Start()	{}
		
		//Shutdown engine when vvvv ends so vvvv process isn't left running
		public void Shutdown()
		{ Cleanup(); }
		#endregion IStatable
		
		#endregion asio
		
		public void AddOutput(IEnumerable<AudioSignal> provider)
		{
			if(provider == null) return;
			foreach(var p in provider)
				MultiInputProvider.Add(p);
		}
		
		public void RemoveOutput(IEnumerable<AudioSignal> provider)
		{
			if(provider == null) return;
			foreach(var p in provider)
				MultiInputProvider.Remove(p);
		}
	}
	
	public static class AudioService
	{
		private static AudioEngine FAudioEngine;
		
		public static AudioEngine Engine
		{
			get
			{
				return FAudioEngine;
			}
		}
		
		static AudioService()
		{
			FAudioEngine = new AudioEngine();
		}
	}
}



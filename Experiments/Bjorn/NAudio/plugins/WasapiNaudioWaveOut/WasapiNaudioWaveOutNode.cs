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

namespace VVVV.Nodes
{
	#region effect/generator
	
	public interface IAudioGenerator
	{
		WaveFormat WaveFormat
		{
			get;
		}
		
		unsafe void Process(float* sample);
		
	}
	
	public interface IAudioStereoEffect
	{
		WaveFormat WaveFormat
		{
			get;
		}
	   
		unsafe void Process(float* sample);
		
	}
	
	
	public class VolumeProcessor : IAudioStereoEffect
	{
		public WaveFormat WaveFormat
		{
			get;
			set;
		}
		
		public float Volume
		{
			get;
			set;
		}
		
		public unsafe void Process(float* sample)
		{
			for(int i=0; i<WaveFormat.Channels; i++)
			{
				sample[i] = sample[i] * Volume;
			}
		}
	}

	public class AudioEffectProvider : ISampleProvider
	{

		
		public AudioEffectProvider(WaveFormat format)
	    {
	        this.WaveFormat = format;
	    }

		protected IAudioStereoEffect Effect;
		protected ISampleProvider Input;
		
	    public unsafe int Read(float[] buffer, int offset, int count)
	    {
	    	
	    	Input.Read(buffer, offset, count);
	    	
	    	var channels = this.WaveFormat.Channels;
	    	
	        for (int i = 0; i < (count/channels); i++)
	        {
	     		fixed(float* sample = &buffer[i*channels])
	        	{
	        		Effect.Process(sample);
	        	}
	        }
	    	
	        return count;
	    }
	
	    public WaveFormat WaveFormat
	    {
	        get;
	    	protected set;
	    	
	    }
		
	}
	
	#endregion effect/generator
	
	#region node and links
	public class VAudioEngineSettings
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

	public class SineSignal : AudioSignal
	{
		public SineSignal(double frequency)
			: base(44100)
		{
			Frequency = frequency;
		}
		
		private double Frequency;
		public double Gain = 0.1;
		private double TwoPi = Math.PI * 2;
		private int nSample = 0;
		
		protected override void FillBuffer(float[] buffer, int offset, int count)
		{
			
			var sampleRate = this.WaveFormat.SampleRate;
			var multiple = TwoPi*Frequency/sampleRate;
			for (int i = 0; i < count; i++)
			{
				// Sinus Generator
				
				buffer[i] = (float)(Gain*Math.Sin(nSample*multiple));
				
				unchecked
				{
					nSample++;
				}
			}
			
		}
		
	}
	
	[PluginInfo(Name = "Sine", Category = "Naudio", Version = "Source", Help = "Creates a sine wave", AutoEvaluate = true, Tags = "Wave")]
	public class AudioSourceNodeBase : IPluginEvaluate
	{
		[Input("Frequency", DefaultValue = 440)]
		IDiffSpread<double> Frequency;
		
		[Input("Gain", DefaultValue = 0.1)]
		IDiffSpread<double> Gain;
		
		[Output("Audio Out")]
		ISpread<AudioSignal> OutBuffer;

		public void Evaluate(int SpreadMax)
		{
			//OutBuffer.ResizeAndDispose(SpreadMax, index =>  new SineSignal(Frequency[index]));
			
			if(Frequency.IsChanged)
			{
				OutBuffer.SliceCount = SpreadMax;
				for(int i=0; i<SpreadMax; i++)
				{
					OutBuffer[i] = new SineSignal(Frequency[i]);
				}
			}
			
			if(Gain.IsChanged)
			{
				for(int i=0; i<SpreadMax; i++)
				{
					(OutBuffer[i] as SineSignal).Gain  = Gain[i];
				}
			}
		}
	}
	
		
	#endregion node and links
	
	public enum AudioOutType
	{
		Asio,
		Wasapi,
		DirectSound,
		Wave
	}
	
	public class AudioEngine: IDisposable
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
		
		#endregion asio
		
		
		public void AddOutput(ISpread<AudioSignal> provider)
		{
			if(provider == null) return;
			foreach(var p in provider)
				MultiInputProvider.Add(p);
		}
		
		public void RemoveOutput(ISpread<AudioSignal> provider)
		{
			if(provider == null) return;
			foreach(var p in provider)
				MultiInputProvider.Remove(p);
		}
	}
	
	public static class AudioService
	{
		static AudioService()
		{
			FAudioEngine = new AudioEngine();
		}
		
		private static AudioEngine FAudioEngine;
		
		public static AudioEngine Engine
		{
			get
			{
				return FAudioEngine;
			}
		}
		
	}
	
	/// <summary>
    /// Helper class for when you need to convert back to an IWaveProvider from
    /// an ISampleProvider. Keeps it as IEEE float
    /// </summary>
    public class MultipleSampleToWaveProvider : IWaveProvider
    {
        private List<ISampleProvider> source = new List<ISampleProvider>();
		private Action FReadingFinished;
    	
        /// <summary>
        /// Initializes a new instance of the WaveProviderFloatToWaveProvider class
        /// </summary>
        /// <param name="source">Source wave provider</param>
        public MultipleSampleToWaveProvider(WaveFormat format, Action readingFinished)
        {
        	this.WaveFormat = format;
        	this.FReadingFinished = readingFinished;
        }
    	
    	public void Add(ISampleProvider provider)
    	{
    		lock(source)
    		{
    			source.Add(provider);
    		}
    	}
    	
    	public void Remove(ISampleProvider provider)
    	{
    		lock(source)
    		{
    			source.Remove(provider);
    		}
    	}
    	
        /// <summary>
        /// Reads from this provider
        /// </summary>
    	float[] FMixerBuffer = new float[1];
    	
    	//this is called from the soundcard
        public int Read(byte[] buffer, int offset, int count)
        {
            int samplesNeeded = count / 4;
            WaveBuffer wb = new WaveBuffer(buffer);
        	
        	//fix buffer size
        	FMixerBuffer = BufferHelpers.Ensure(FMixerBuffer, samplesNeeded);
        	
        	//empty buffer
        	wb.Clear();
        	
        	lock(source)
        	{
        		var inputCount = source.Count;
        		//var invCount = 1.0f/inputCount;
        		for(int i=0; i<inputCount; i++)
        		{
        			if(source[i] != null)
        			{
        				//starts the calculation of the audio graph
        				source[i].Read(FMixerBuffer, offset / 4, samplesNeeded);
        				
        				//add to output buffer
        				for(int j=0; j<samplesNeeded; j++)
        				{
        					wb.FloatBuffer[j] += FMixerBuffer[j];
        					FMixerBuffer[j] = 0;
        				}
        			}
        		}
        		
        		//tell  the engine that reading has finished
				FReadingFinished();
        	}
            return count; //always run 
        }

        /// <summary>
        /// The waveformat of this WaveProvider (same as the source)
        /// </summary>
        public WaveFormat WaveFormat
        {
            get;
        	protected set;
        }
    }
	
	#region PluginInfo
	[PluginInfo(Name = "AudioEngine", Category = "Naudio", Help = "Configures the audio engine", AutoEvaluate = true, Tags = "Asio, Wasapi, DirectSound, Wave")]
	#endregion PluginInfo
	public class AudioEngineNode : IPluginEvaluate
	{
		#region fields & pins
		[Input("Play", DefaultValue = 0)]
		IDiffSpread<bool> FPlayIn;
		
		[Input("Driver", EnumName = "NAudioASIO")]
		IDiffSpread<EnumEntry> FDriverIn;
		
		[Input("Control Panel", IsBang = true)]
		IDiffSpread<bool> FShowPanelIn;

		[Import()]
		ILogger FLogger;
		AudioEngine FEngine;
		#endregion fields & pins	
		
		[ImportingConstructor]
		public AudioEngineNode()
		{
			FEngine = AudioService.Engine;
			
			var drivers = AsioOut.GetDriverNames();
			
			if (drivers.Length > 0)
			{
				EnumManager.UpdateEnum("NAudioASIO", drivers[0], drivers);
			}
			else
			{
				drivers = new string[]{"No ASIO!? -> go download ASIO4All"};
				EnumManager.UpdateEnum("NAudioASIO", drivers[0], drivers);
			}
		}
		
		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			if(FDriverIn.IsChanged)
			{
				FEngine.DriverName = FDriverIn[0].Name;
				FEngine.CreateAsio();
				if(FPlayIn[0]) FEngine.AsioOut.Play();
			}
			
			if(FShowPanelIn[0])
			{
				FEngine.AsioOut.ShowControlPanel();
			}
			
			if(FPlayIn.IsChanged)
			{
				if(FPlayIn[0]) FEngine.AsioOut.Play();
				else FEngine.AsioOut.Stop();
			}
		}

	}
	
	#region PluginInfo
	[PluginInfo(Name = "AudioOut", Category = "Naudio", Help = "Audio Out", AutoEvaluate = true, Tags = "Asio, Wasapi, DirectSound, Wave")]
	#endregion PluginInfo
	public class AudioOutNode : IPluginEvaluate, IDisposable
	{
		#region fields & pins
		[Input("Input")]
		IDiffSpread<AudioSignal> FInput;

		//[Output("Output")]
		//ISpread<double> FOutput;
		
		[Import()]
		ILogger FLogger;
		#endregion fields & pins

		private ISpread<AudioSignal> LastProvider;
		public AudioOutNode()
		{
			LastProvider = new Spread<AudioSignal>();
		}
		
		public void Dispose()
		{
			AudioService.Engine.RemoveOutput(LastProvider);
		}
		
		private void RestartAudio()
		{
			AudioService.Engine.RemoveOutput(LastProvider);
			AudioService.Engine.AddOutput(FInput);
			LastProvider.AssignFrom(FInput);
		}		
		
		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			//FOutput.SliceCount = SpreadMax;

			if(FInput.IsChanged)
			{
				RestartAudio();
			}

			//FLogger.Log(LogType.Debug, "hi tty!");
		}
	}
	
	#region PluginInfo
	[PluginInfo(Name = "WaveOut", Category = "Naudio", Version = "Wasapi", Help = "Wasapi Audio Out", AutoEvaluate = true, Tags = "")]
	#endregion PluginInfo
	public class WasapiNaudioWaveOutNode : IPluginEvaluate, IDisposable
	{
		#region fields & pins
		[Input("Input")]
		IDiffSpread<ISampleProvider> FInput;

		//[Output("Output")]
		//ISpread<double> FOutput;

		WasapiOut FWaveOut;
		SampleToWaveProvider FWaveProvider;
		
		[Import()]
		ILogger FLogger;
		#endregion fields & pins

		public WasapiNaudioWaveOutNode()
		{
		
		}
		
		public void Dispose()
		{
			FWaveOut.Dispose();
		}
		
		private void RestartAudio()
		{
			if(FWaveOut != null)
			{
				Dispose();
			}
			
			if(FInput[0] != null)
			{
				FWaveOut = new WasapiOut(AudioClientShareMode.Shared, 4);
	
				FWaveProvider = new SampleToWaveProvider(FInput[0]);
				FWaveOut.Init(FWaveProvider);
				FWaveOut.Play();
			}

		}		
		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			//FOutput.SliceCount = SpreadMax;

			if(FInput.IsChanged || FWaveProvider == null)
			{
				RestartAudio();
			}

			//FLogger.Log(LogType.Debug, "hi tty!");
		}
	}
}



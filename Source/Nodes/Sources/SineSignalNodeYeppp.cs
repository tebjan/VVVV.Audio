#region usings
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq.Expressions;

using NAudio.CoreAudioApi;
using NAudio.Utils;
using NAudio.Wave;
using NAudio.Wave.Asio;
using NAudio.Wave.SampleProviders;
using VVVV.Audio;
using VVVV.Core.Logging;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

#endregion usings

namespace VVVV.Nodes
{
	public static class PerfLogger
	{
		public static ILogger Logger;
		
		private static int Counter = 0;
		
		public static void Log(string msg)
		{
			if(Counter == 0 && Logger != null)
			{
				Logger.Log(LogType.Message, msg);
			}
			
			Counter++;
			Counter%=61;
		}
	}
	
	public static class PerfCounter
	{
		private static Dictionary<string, Stopwatch> Watches = new Dictionary<string, Stopwatch>();
		
		public static void Start(string key)
		{
			if(!Watches.ContainsKey(key))
				Watches[key] = new Stopwatch();
			else
				Watches[key].Reset();
			
			Watches[key].Start();
				
		}
		
		public static void Stop(string key)
		{
			Watches[key].Stop();
			PerfLogger.Log(key + ":" + Watches[key].ElapsedTicks);
		}
	}
	
	public class MultiSineSignalYeppp : AudioSignal
	{
		SineTable SineTable = new SineTable(2205);
		public MultiSineSignalYeppp(ISpread<float> frequency, ISpread<float> gain)
			: base(44100)
		{
			Frequencies = frequency;
			Gains = gain;
			Phases = new Spread<double>();
		}
		
		public ISpread<float> Frequencies;
		public ISpread<float> Gains;
		private readonly float TwoPi = (float)(Math.PI * 2);
		private ISpread<double> Phases;
		
		private double[] phases = new double[1];
		private double[] sines = new double[1];
		
		protected override void FillBuffer(float[] buffer, int offset, int count)
		{
			PerfCounter.Start("MultiSineYeppp");
			var sampleRate = this.WaveFormat.SampleRate;
			var spreadMax = Frequencies.CombineWith(Gains);
			Phases.Resize(spreadMax, () => default(float), f => f = 0);
			
			var oneDArraySize = spreadMax * count;
			
			//resize arrays
			if(phases.Length != oneDArraySize) phases = new double[oneDArraySize];
			if(sines.Length != oneDArraySize) sines = new double[oneDArraySize];
			
			//prepare phase array
			for (int slice = 0; slice < spreadMax; slice++)
			{
				var increment = TwoPi*Frequencies[slice]/sampleRate;
				var phase = Phases[slice];
				
				for (int i = 0; i < count; i++)
				{
					// Sinus Generator
					phases[i + slice*count] = phase;
					
					phase += increment;
					if(phase > TwoPi)
						phase -= TwoPi;
					else if(phase < 0)
						phase += TwoPi;
				}
				
				Phases[slice] = phase;
			}
			
			//calc sines
			Yeppp.Math.Sin_V64f_V64f(phases, 0, sines, 0, oneDArraySize);
			
			//write to output
			for (int slice = 0; slice < spreadMax; slice++) 
			{
			 	var gain = Gains[slice];
			 	
			 	if(slice == 0)
			 	{
			 		for (int i = 0; i < count; i++)
			 		{
			 			// Sinus Generator
			 			buffer[i] = gain*(float)sines[i + slice*count];
			 		}
			 	}
			 	else
			 	{
			 		for (int i = 0; i < count; i++)
			 		{
			 			// Sinus Generator
			 			buffer[i] += gain*(float)sines[i + slice*count];

			 		}
			 	}
			}
			
			PerfCounter.Stop("MultiSineYeppp");
		}
			
	}
		
	public class SineSignalYeppp : AudioSignal
	{
		public SineSignalYeppp(float frequency, float gain)
			: base(44100)
		{
			Frequency = frequency;
			Gain = gain;
		}
		
		public float Frequency;
		public float Gain = 0.1f;
		private float TwoPi = (float)(Math.PI * 2);
		private double phase = 0;
		private double[] phases = new double[1];
		private double[] sines = new double[1];
		
		protected override void FillBuffer(float[] buffer, int offset, int count)
		{
			PerfCounter.Start("SineYeppp");
			//new array?
			if(phases.Length != count) phases = new double[count];
			if(sines.Length != count) sines = new double[count];
			
			var sampleRate = this.WaveFormat.SampleRate;
			var increment = TwoPi*Frequency/sampleRate;
			
			//calc phases
			for (int i = 0; i < count; i++)
			{
				phases[i] = phase;
				phase += increment;
				if(phase > TwoPi)
					phase -= TwoPi;
				else if(phase < 0)
					phase += TwoPi;
			}
			
			//calc sines
			Yeppp.Math.Sin_V64f_V64f(phases, 0, sines, 0, count);
			
			//write to output
			for (int i = 0; i < count; i++)
			{
				// Sinus Generator
				buffer[i] = Gain*(float)sines[i];
			}
			
			PerfCounter.Stop("SineYeppp");
		}
	}
	
	[PluginInfo(Name = "Sine", Category = "Audio", Version = "Source Yeppp", Help = "Creates a sine wave", AutoEvaluate = true, Tags = "Wave")]
	public class SineSignalNodeYeppp : AudioNodeBase
	{
		[Input("Frequency", DefaultValue = 440)]
		IDiffSpread<float> Frequency;
		
		[Input("Gain", DefaultValue = 0.1)]
		IDiffSpread<float> Gain;
		
		[ImportingConstructor]
		public SineSignalNodeYeppp([Import] ILogger logger)
		{
			PerfLogger.Logger = logger;
		}
		
		public override void Evaluate(int SpreadMax)
		{
			OutBuffer.ResizeAndDispose(SpreadMax, index => new SineSignalYeppp(Frequency[index], Gain[index]));
			
			if(Frequency.IsChanged)
			{
				for(int i=0; i<SpreadMax; i++)
				{
					if(OutBuffer[i] == null) OutBuffer[i] = new SineSignalYeppp(Frequency[i], Gain[i]); 
					
					(OutBuffer[i] as SineSignalYeppp).Frequency = Frequency[i];
				}
			}
			
			if(Gain.IsChanged)
			{
				for(int i=0; i<SpreadMax; i++)
				{
					(OutBuffer[i] as SineSignalYeppp).Gain  = Gain[i];
				}
			}
		}
	}
	
	[PluginInfo(Name = "MultiSine", Category = "Audio", Version = "Source Yeppp", Help = "Creates a spread of sine waves", AutoEvaluate = true, Tags = "LFO, additive, synthesis")]
	public class MultiSineSignalNodeYeppp : AudioNodeBase
	{
		[Input("Frequency", DefaultValue = 440)]
		IDiffSpread<ISpread<float>> Frequency;
		
		[Input("Gain", DefaultValue = 0.1)]
		IDiffSpread<ISpread<float>> Gain;
		
		[ImportingConstructor]
		public MultiSineSignalNodeYeppp([Import] ILogger logger)
		{
			PerfLogger.Logger = logger;
		}
		
		public override void Evaluate(int SpreadMax)
		{
			SpreadMax = Frequency.CombineWith(Gain);
			OutBuffer.ResizeAndDispose(SpreadMax, index => new MultiSineSignalYeppp(Frequency[index], Gain[index]));
			
			if(Frequency.IsChanged)
			{
				for(int i=0; i<SpreadMax; i++)
				{
					if(OutBuffer[i] == null) OutBuffer[i] = new MultiSineSignalYeppp(Frequency[i], Gain[i]); 
					
					(OutBuffer[i] as MultiSineSignalYeppp).Frequencies = Frequency[i];
				}
			}
			
			if(Gain.IsChanged)
			{
				for(int i=0; i<SpreadMax; i++)
				{
					(OutBuffer[i] as MultiSineSignalYeppp).Gains = Gain[i];
				}
			}
		}
	}
}



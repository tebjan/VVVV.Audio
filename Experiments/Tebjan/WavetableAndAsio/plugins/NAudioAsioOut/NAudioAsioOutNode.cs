
#region usings
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;
using VVVV.Core.Logging;

using NAudio.Wave;
using NAudio.FileFormats.Wav;

#endregion usings

namespace VVVV.Nodes
{
	public struct Grain
	{
		public int SampleRate;
		public int Start;
		private int FLength;
		public double Index;
		private double delta;
		public float[] Window;
		
		public int Length
		{
			get
			{
				return FLength;
			}
			set
			{
				FLength = value;
				Window = Windowing.Hann(value);
			}
		}
		
		public double Freq
		{
			get
			{
				return delta * BaseFreq;
			}
			
			set
			{
				delta = value / BaseFreq;
			}
		}
		
		public double BaseFreq
		{
			get
			{
				return SampleRate / (double)Length;
			}
			
			set
			{
				Length = (int)(SampleRate / value);
			}
		}
		
		public void Inc()
		{
			Index = (Index + delta) % FLength;
		}
	}

	#region PluginInfo
	[PluginInfo(Name = "Granulator", Category = "NAudio", Help = "Plays grains of a sample", Tags = "")]
	#endregion PluginInfo
	public class NAudioGranulatorNode : IPluginEvaluate, IDisposable
	{
		#region fields & pins
		[Input("Sample", StringType = StringType.Filename, FileMask = "Wave Files|*.wav")]
		IDiffSpread<string> FSampleIn;
		
		[Input("Size", DefaultValue = 1024)]
		IDiffSpread<int> FSizeIn;
		
		[Input("Offset", DefaultValue = 0.5)]
		IDiffSpread<double> FOffsetIn;
		
		[Input("Frequency", DefaultValue = 440)]
		IDiffSpread<double> FFreqIn;

		[Output("Output")]
		ISpread<IWaveProvider> FOutput;

		[Import()]
		ILogger FLogger;
		
		protected GrainWaveProvider FSample;
		
		#endregion fields & pins

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			if(FSampleIn.IsChanged)
			{
				FSample = new GrainWaveProvider(FSampleIn[0]);
				FOutput[0] = FSample;
			}
			
			if(FSizeIn.IsChanged)
			{
				FSample.FGrain.Length = FSizeIn[0];
			}
			
			if(FOffsetIn.IsChanged)
			{
				FSample.FGrain.Start = (int)(FOffsetIn[0] * FSample.Length);
			}
			
			if(FFreqIn.IsChanged)
			{
				FSample.FGrain.Freq = FFreqIn[0];
			}
		}
		
		public void Dispose()
		{
			if(FSample != null)
				FSample.Dispose();
		}
	}
	
	#region PluginInfo
	[PluginInfo(Name = "LUT", Category = "NAudio", Help = "Basic template with one value in/out", Tags = "")]
	#endregion PluginInfo
	public class NAudioLUTNode : IPluginEvaluate
	{
		#region fields & pins
		[Input("Table")]
		IDiffSpread<float> FTableIn;
		
		[Input("Frequency", DefaultValue = 1, IsSingle = true)]
		IDiffSpread<float> FFreqIn;
		
		[Input("Delay Amount", DefaultValue = 0, IsSingle = true)]
		IDiffSpread<float> FDelayAmountIn;
		
		[Input("Delay Time", DefaultValue = 0.5, IsSingle = true)]
		IDiffSpread<float> FDelayTimeIn;

		[Output("Output")]
		ISpread<IWaveProvider> FOutput;

		[Import()]
		ILogger FLogger;
		
		protected LUTWaveProvider FLUT = new LUTWaveProvider();
		
		#endregion fields & pins

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			if(FFreqIn.IsChanged)
			{
				FLUT.Delta = FFreqIn[0] * 1024 / 44100;
			}
			
			if(FDelayAmountIn.IsChanged)
			{
				FLUT.DelayAmount = FDelayAmountIn[0];
			}
			
			if(FDelayTimeIn.IsChanged)
			{
				FLUT.DelayTime = FDelayTimeIn[0];
			}
			
			if(FTableIn.IsChanged)
			{
				//FLogger.Log(LogType.Debug, "LUT");
				for(int i=0; i<FLUT.LUT.Length; i++)
				{
					FLUT.LUTBuffer[i] = FTableIn[i];
				}
				
				FLUT.SwapBuffers();
				
				FOutput[0] = FLUT;
			}
		}
	}
	
	#region PluginInfo
	[PluginInfo(Name = "AsioOut", Category = "NAudio", Help = "Basic template with one value in/out", Tags = "", AutoEvaluate = true)]
	#endregion PluginInfo
	public class NAudioAsioOutNode : IPluginEvaluate, IDisposable
	{
		#region fields & pins
		[Input("Wave")]
		IDiffSpread<IWaveProvider> FWaveIn;
		
		[Input("Play", DefaultValue = 0)]
		IDiffSpread<bool> FPlayIn;
		
		[Input("Driver", EnumName = "NAudioASIO")]
		IDiffSpread<EnumEntry> FDriverIn;
		
		[Input("Control Panel", IsBang = true)]
		IDiffSpread<bool> FShowPanelIn;

		[Output("Output")]
		ISpread<double> FOutput;

		[Import()]
		ILogger FLogger;
		
		protected AsioOut FAsioOut;
		protected IWaveProvider FWave;
		
		#endregion fields & pins
		
		[ImportingConstructor]
		public NAudioAsioOutNode()
		{
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
			FOutput.SliceCount = SpreadMax;
			
			if(FDriverIn.IsChanged)
			{
				CreateAsio();
				if(FPlayIn[0]) FAsioOut.Play();
			}
			
			if(FShowPanelIn[0])
			{
				FAsioOut.ShowControlPanel();
			}
			
			if(FWave != FWaveIn[0])
			{
				FWave = FWaveIn[0];
				CreateAsio();
				if(FPlayIn[0]) FAsioOut.Play();
			}
			
			if(FPlayIn.IsChanged)
			{
				if(FPlayIn[0]) FAsioOut.Play();
				else FAsioOut.Stop();
			}
			
		}
		
		#region asio
		
		//init driver
        private void CreateAsio()
        {
        	//recreate device if necessary
        	if (this.FAsioOut != null)
        	{
        		Cleanup();
        	}
        	
        	this.FAsioOut = new AsioOut(FDriverIn[0].Name);
        	
        	this.FAsioOut.Init(FWave);
        }
        
		//close
		public void Dispose()
		{
			Cleanup();
		}
		
		//close ASIO
		private void Cleanup()
		{
			if (this.FAsioOut != null)
			{
				this.FAsioOut.Dispose();
				this.FAsioOut = null;
			}
		}
		
		#endregion asio
		
	}
	
	
	
	
}

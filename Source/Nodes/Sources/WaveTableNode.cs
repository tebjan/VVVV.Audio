#region usings
using System;
using System.ComponentModel.Composition;
using System.Collections.Generic;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;
using VVVV.Audio;

using NAudio.Wave;
using NAudio.Wave.Asio;
using NAudio.CoreAudioApi;
using NAudio.Wave.SampleProviders;
using NAudio.Utils;


using VVVV.Core.Logging;
#endregion usings

namespace VVVV.Nodes
{
	/// <summary>
	/// Class for LUT playback
	/// </summary>
	public class WaveTableSignal : AudioSignal
	{
		
		//constructor
		public WaveTableSignal()
		{
			LUT = new float[1024];
			LUTBuffer = new float[1024];
			DlyBufferSize = 2*MaxDlyTime;
			DlyBuffer = new float[DlyBufferSize];
		}

		double FFrequency;
		public double Frequency 
		{
			get 
			{ 
				return FFrequency; 
			}
			set 
			{ 
				FFrequency = value; 
			}
		}
		
		private float Delta;
		
		private float FIndex;
		
		private float[] LUT;
		
		public float[] LUTBuffer
		{
			get;
			private set;
		}
		
		public void SwapBuffers()
		{
			var tmp = LUT;
			LUT = LUTBuffer;
			LUTBuffer = tmp;
		}
		
		int i = 0;
		int j = 0;
		
		float fDlyTime = 0.25f;
		
		public float DelayTime
		{
			get { return fDlyTime; }
			set { fDlyTime = (float)VMath.Clamp(value, 0.0, 1.0); }
		}
		
		int MaxDlyTime = 44100*2;
		int DlyBufferSize;
		float[] DlyBuffer;
		public float DelayAmount;
		
		
		protected unsafe override void FillBuffer(float[] buffer, int offset, int count)
		{
			var luts = LUT.Length;
			var Delta = (float)(FFrequency * luts / WaveFormat.SampleRate);
			
			fixed(float* lut = LUT)
			{
				fixed(float* outBuff = buffer)
				{
					for (int n = 0; n < count; n++)
					{
						
						if( i >= DlyBufferSize ) i = 0;
						
						j = i - (int)(fDlyTime * MaxDlyTime);
						
						if( j < 0 ) j += DlyBufferSize;
						
						var round = (int)FIndex;
						var index = (int)Math.Floor(FIndex);
						var s1 = LUT[index%luts];
						//var s2 = LUT[(index + 1)%luts];
						
						outBuff[n+offset] = DlyBuffer[i] = s1 + DlyBuffer[j] * DelayAmount;
						FIndex = (FIndex + Delta) % luts;
						i++;
					}
				}
			}
		}
	}
	
	public enum WavetableWindowFunction
	{
		None,
		Hamming,
		Hann,
		BlackmannHarris
	}
	
	#region PluginInfo
	[PluginInfo(Name = "WaveTable", Category = "Audio", Version = "Source", Help = "Generates an audio signal from a wave table", Tags = "LUT, Synthesis", AutoEvaluate = true)]
	#endregion PluginInfo
	public class WaveTableNode : GenericAudioSourceNode<WaveTableSignal>
	{
		#region fields & pins
		[Input("Table")]
		IDiffSpread<ISpread<float>> FTableIn;
		
		[Input("Frequency", DefaultValue = 440)]
		IDiffSpread<float> FFreqIn;
		
		[Input("Window Function")]
		IDiffSpread<WavetableWindowFunction> FWindowFuncIn;
		
		[Input("Delay Amount", DefaultValue = 0)]
		IDiffSpread<float> FDelayAmountIn;
		
		[Input("Delay Time", DefaultValue = 0.5)]
		IDiffSpread<float> FDelayTimeIn;
	
		[Import()]
		ILogger FLogger;
		
		float[] FWindow;
		
		#endregion fields & pins
		
		protected override int GetSpreadMax(int originalSpreadMax)
		{
			if(originalSpreadMax == 0) return 0;
			var max = Math.Max(FTableIn.SliceCount, FFreqIn.SliceCount);
			max = Math.Max(max, FWindowFuncIn.SliceCount);
			max = Math.Max(max, FDelayAmountIn.SliceCount);
			return Math.Max(max, FDelayTimeIn.SliceCount);
		}
		
		WavetableWindowFunction FlastWindowFunc;
		
		protected override void SetParameters(int i, WaveTableSignal instance)
		{
			SetParameters(i, instance, false);
		}
		
		protected void SetParameters(int i, WaveTableSignal instance, bool created)
		{
			
			instance.Frequency = FFreqIn[i];
			instance.DelayAmount = FDelayAmountIn[i];
			instance.DelayTime = FDelayTimeIn[i];
			
			if(FTableIn.IsChanged || FWindowFuncIn.IsChanged || created)
			{	
				//setup new window
				if(FWindowFuncIn.IsChanged || created)
				{
					Func<int, int, double> window;
					
					switch (FWindowFuncIn[i])
					{
						case WavetableWindowFunction.Hamming:
							window = AudioUtils.HammingWindow;
							break;
						case WavetableWindowFunction.Hann:
							window = AudioUtils.HannWindow;
							break;
						case WavetableWindowFunction.BlackmannHarris:
							window = AudioUtils.BlackmannHarrisWindow;
							break;
						default:
							window = (j, k) => { return 1; };
							break;
					}
					
					FWindow = new float[instance.LUTBuffer.Length];
					for (int j = 0; j < FWindow.Length; j++) 
					{
						FWindow[j] = (float)window(j, FWindow.Length);
					}
				}
				
				//FLogger.Log(LogType.Debug, "LUT");
				for(int j=0; j<instance.LUTBuffer.Length; j++)
				{
					instance.LUTBuffer[j] = FTableIn[i][j] * FWindow[j];
				}
				
				instance.SwapBuffers();
			}
		}
		
		protected override WaveTableSignal GetInstance(int i)
		{
			var instance = new WaveTableSignal();
			SetParameters(i, instance, true);
			return instance;
		}
	}
}



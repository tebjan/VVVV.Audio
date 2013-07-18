#region usings
using System;
using System.ComponentModel.Composition;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using NAudio.Wave;
using NAudio.CoreAudioApi;
using NAudio.Wave.SampleProviders;


using VVVV.Core.Logging;
#endregion usings

namespace VVVV.Nodes
{
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

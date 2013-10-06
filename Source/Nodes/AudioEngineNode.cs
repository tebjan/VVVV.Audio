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
	[PluginInfo(Name = "AudioEngine", Category = "Audio", Help = "Configures the audio engine", AutoEvaluate = true, Tags = "Asio")]
	public class AudioEngineNode : IPluginEvaluate, IDisposable
	{
		#region fields & pins
		[Input("Play", DefaultValue = 0)]
		IDiffSpread<bool> FPlayIn;
		
		[Input("Driver", EnumName = "NAudioASIO")]
		IDiffSpread<EnumEntry> FDriverIn;
		
		[Input("Control Panel", IsBang = true)]
		IDiffSpread<bool> FShowPanelIn;
		
		[Output("Time")]
		ISpread<double> FTime;
		
		[Output("Input Chanels")]
		ISpread<int> FInputChannels;
		
		[Output("Output Chanels")]
		ISpread<int> FOutputChannels;
	
		[Import()]
		ILogger FLogger;
		AudioEngine FEngine;
		#endregion fields & pins	
		
		[ImportingConstructor]
		public AudioEngineNode()
		{
			FEngine = AudioService.Engine;
			
			var drivers = FEngine.AsioDriverNames;
			
			if (drivers.Length > 0)
			{
				EnumManager.UpdateEnum("NAudioASIO", drivers[0], drivers);
			}
			else
			{
				drivers = new string[]{"No ASIO!? -> go download ASIO4All"};
				EnumManager.UpdateEnum("NAudioASIO", drivers[0], drivers);
			}
			
			//FTime[0] = FEngine.Timer.Time;
		}
		
		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			if(FDriverIn.IsChanged)
			{
				FEngine.DriverName = FDriverIn[0].Name;
				FEngine.Play = FPlayIn[0];
				FInputChannels[0] = FEngine.AsioOut.DriverInputChannelCount;
				FOutputChannels[0] = FEngine.AsioOut.DriverOutputChannelCount;
			}
			
			if(FShowPanelIn[0])
			{
				FEngine.AsioOut.ShowControlPanel();
			}
			
			if(FPlayIn.IsChanged)
			{
				FEngine.Play = FPlayIn[0];
			}
		}
		
		//HACK: coupled lifetime of engine to this node
		public void Dispose()
		{
			AudioService.DisposeEngine();
		}
	
	}
}



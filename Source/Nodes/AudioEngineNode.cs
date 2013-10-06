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
}



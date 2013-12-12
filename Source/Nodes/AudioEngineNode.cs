#region usings
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

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
	public enum AudioSampleRate
	{
		Hz8000 = 8000,
		Hz11025 = 11025,
		Hz16000 = 16000,
		Hz22050 = 22050,
		Hz32000 = 32000,
		Hz44056	= 44056,
		Hz44100 = 44100,
		Hz48000 = 48000,
		Hz88200 = 88200,
		Hz96000 = 96000,
		Hz176400 = 176400,
		Hz192000 = 192000,
		Hz352800 = 352800
		
	}
	
	[PluginInfo(Name = "AudioEngine", Category = "Audio", Help = "Configures the audio engine", AutoEvaluate = true, Tags = "Asio")]
	public class AudioEngineNode : IPluginEvaluate, IDisposable
	{
		#region fields & pins
		#pragma warning disable 0649
		[Input("Play", DefaultValue = 0)]
		IDiffSpread<bool> FPlayIn;
		
		[Input("BPM", DefaultValue = 120)]
		IDiffSpread<double> FBPMIn;
		
		[Input("Driver", EnumName = "NAudioASIO")]
		IDiffSpread<EnumEntry> FDriverIn;
		
		[Input("Sample Rate", DefaultEnumEntry = "Hz44100")]
		IDiffSpread<AudioSampleRate> FSamplingRateIn;
		
		[Input("Desired Input Channels", DefaultValue = 2)]
		IDiffSpread<int> FInputChannelsIn;
		
		[Input("Input Channel Offset", DefaultValue = 0, Visibility = PinVisibility.OnlyInspector)]
		IDiffSpread<int> FInputChannelOffsetIn;
		
		[Input("Desired Output Channels", DefaultValue = 2)]
		IDiffSpread<int> FOutputChannelsIn;
		
		[Input("Output Channel Offset", DefaultValue = 0, Visibility = PinVisibility.OnlyInspector)]
		IDiffSpread<int> FOutputChannelOffsetIn;
		
		[Input("Control Panel", IsBang = true)]
		IDiffSpread<bool> FShowPanelIn;
		
		[Output("Time")]
		ISpread<double> FTime;
		
		[Output("Beat")]
		ISpread<double> FBeat;
		
		[Output("Buffer Size")]
		ISpread<int> FBufferSizeOut;
		
		[Output("Driver Input Chanels")]
		ISpread<int> FInputChannelsOut;
		
		[Output("Driver Output Chanels")]
		ISpread<int> FOutputChannelsOut;
		
		[Output("Open Input Chanels")]
		ISpread<int> FOpenInputChannelsOut;
		
		[Output("Open Output Chanels")]
		ISpread<int> FOpenOutputChannelsOut;
	
		[Import()]
		ILogger FLogger;
		AudioEngine FEngine;
		#pragma warning restore
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
			if(FDriverIn.IsChanged || FSamplingRateIn.IsChanged ||
			   FInputChannelsIn.IsChanged || FInputChannelOffsetIn.IsChanged ||
			   FOutputChannelsIn.IsChanged || FOutputChannelOffsetIn.IsChanged)
			{
				FEngine.ChangeDriverSettings(FDriverIn[0].Name, 
				                             (int)FSamplingRateIn[0], 
				                             FInputChannelsIn[0], 
				                             FInputChannelOffsetIn[0], 
				                             FOutputChannelsIn[0], 
				                             FOutputChannelOffsetIn[0]);
				
				FEngine.Play = FPlayIn[0];
				FInputChannelsOut[0] = FEngine.AsioOut.DriverInputChannelCount;
				FOutputChannelsOut[0] = FEngine.AsioOut.DriverOutputChannelCount;
				FOpenInputChannelsOut[0] = FEngine.AsioOut.NumberOfInputChannels;
				FOpenOutputChannelsOut[0] = FEngine.AsioOut.NumberOfOutputChannels;
				
				FBufferSizeOut[0] = FEngine.Settings.BufferSize;
			}
			
			if(FShowPanelIn[0])
			{
				FEngine.AsioOut.ShowControlPanel();
			}
			
			if(FPlayIn.IsChanged)
			{
				FEngine.Play = FPlayIn[0];
			}
			
			if(FBPMIn.IsChanged)
			{
				FEngine.Timer.BPM =FBPMIn[0];
			}
			
			FTime[0] = FEngine.Timer.Time;
			FBeat[0] = FEngine.Timer.Beat;
		}
		
		//HACK: coupled lifetime of engine to this node
		public void Dispose()
		{
			AudioService.DisposeEngine();
		}
	
	}
}



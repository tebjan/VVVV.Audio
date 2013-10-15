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
	[PluginInfo(Name = "CreateBuffer", Category = "Audio", Help = "Creates a buffer which can be used to write and read samples", AutoEvaluate = true, Tags = "record")]
	public class CreateBufferNode : IPluginEvaluate, IDisposable
	{
		#region fields & pins
		#pragma warning disable 0649
		[Input("Name", DefaultString = "")]
		IDiffSpread<string> FNameIn;
		
		[Input("Size", DefaultValue = 1024)]
		IDiffSpread<int> FSizeIn;
	
		#pragma warning restore
		#endregion fields & pins	
		
		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			if(FNameIn.IsChanged ||FSizeIn.IsChanged)
			{
				var storage = AudioService.BufferStorage;
				for (int i = 0; i < FNameIn.SliceCount; i++)
				{
					var key = FNameIn[i];
					if (!string.IsNullOrEmpty(key))
					{
						if(storage.ContainsKey(key))
						{
							if(storage[key].Length != FSizeIn[i])
							{
								storage[key] = new float[FSizeIn[i]];
							}
						}
						else
						{
							storage[key] = new float[FSizeIn[i]];
						}
					}
				}
				
				UpdateEnum();
			}
		}
		
		void UpdateEnum()
		{
			var bufferKeys = AudioService.BufferStorage.Keys.ToArray();
			
			if (bufferKeys.Length > 0)
			{
				EnumManager.UpdateEnum("AudioBufferStorageKeys", bufferKeys[0], bufferKeys);
			}
			else
			{
				bufferKeys = new string[]{"No Buffers Created yet"};
				EnumManager.UpdateEnum("AudioBufferStorageKeys", bufferKeys[0], bufferKeys);
			}
		}
		
		public void Dispose()
		{
			foreach (var key in FNameIn) 
			{
				AudioService.BufferStorage.Remove(key);
			}
			
		}
	}
	
	[PluginInfo(Name = "AudioEngine", Category = "Audio", Help = "Configures the audio engine", AutoEvaluate = true, Tags = "Asio")]
	public class AudioEngineNode : IPluginEvaluate, IDisposable
	{
		#region fields & pins
		#pragma warning disable 0649
		[Input("Play", DefaultValue = 0)]
		IDiffSpread<bool> FNameIn;
		
		[Input("BPM", DefaultValue = 120)]
		IDiffSpread<double>FSizeIn;
		
		[Input("Driver", EnumName = "NAudioASIO")]
		IDiffSpread<EnumEntry> FDriverIn;
		
		[Input("Control Panel", IsBang = true)]
		IDiffSpread<bool> FShowPanelIn;
		
		[Output("Time")]
		ISpread<double> FTime;
		
		[Output("Beat")]
		ISpread<double> FBeat;
		
		[Output("Input Chanels")]
		ISpread<int> FInputChannels;
		
		[Output("Output Chanels")]
		ISpread<int> FOutputChannels;
	
		[Import()]
		ILogger FLogger;
		AudioEngine FEngine;
		#pragma warning restore
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
			
		}
		
		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			if(FDriverIn.IsChanged)
			{
				FEngine.DriverName = FDriverIn[0].Name;
				FEngine.Play = FNameIn[0];
				FInputChannels[0] = FEngine.AsioOut.DriverInputChannelCount;
				FOutputChannels[0] = FEngine.AsioOut.DriverOutputChannelCount;
			}
			
			if(FShowPanelIn[0])
			{
				FEngine.AsioOut.ShowControlPanel();
			}
			
			if(FNameIn.IsChanged)
			{
				FEngine.Play = FNameIn[0];
			}
			
			if(FSizeIn.IsChanged)
			{
				FEngine.Timer.BPM =FSizeIn[0];
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



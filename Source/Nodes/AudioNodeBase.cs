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

	public abstract class AudioNodeBase : IPluginEvaluate, IDisposable, IPartImportsSatisfiedNotification
	{
		[Output("Audio Out")]
		protected Pin<AudioSignal> OutBuffer;
		
		AudioEngine FEngine;
		public virtual void OnImportsSatisfied()
		{
			FEngine = AudioService.Engine;
			
			OutBuffer.Connected += delegate
			{
				CheckOutConnections();
			};
			
			OutBuffer.Disconnected += delegate 
			{
				CheckOutConnections();
			};
		}
		
		private void CheckOutConnections()
		{
			if((OutBuffer.PluginIO as IPin).GetConnectedPins().Length > 1)
			{
				foreach (var element in OutBuffer)
				{
					element.NeedsBufferCopy = true;
				}
			}
			else
			{
				foreach (var element in OutBuffer)
				{
					element.NeedsBufferCopy = false;
				}
			}
		}
		
		//called when data for any output pin is requested
		public abstract void Evaluate(int SpreadMax);
		
		//dispose stuff?
		public virtual void Dispose()
		{
		}
	}
}



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

namespace VVVV.Audio
{
	public class MasterChannel
	{
		public MasterChannel(AudioSignal sig, int channel)
		{
			Signal = sig;
			Channel = channel;
		}
		
		public AudioSignal Signal;
		public int Channel;
	}
	
	/// <summary>
	/// Helper class for when you need to convert back to an IWaveProvider from
	/// an ISampleProvider. Keeps it as IEEE float
	/// </summary>
	public class MasterWaveProvider : IWaveProvider
	{
		private object FSourceLock =  new object();
		private List<MasterChannel> FSources = new List<MasterChannel>();
		private List<IAudioSink> FSinks = new List<IAudioSink>();
		private Action<int> FReadingFinished;
		
		/// <summary>
		/// Initializes a new instance of the WaveProviderFloatToWaveProvider class
		/// </summary>
		/// <param name="source">Source wave provider</param>
		public MasterWaveProvider(WaveFormat format, Action<int> readingFinished)
		{
			this.WaveFormat = format;
			this.FReadingFinished = readingFinished;
		}
		
		//add/remove sample providers
		public void Add(MasterChannel provider)
		{
			lock(FSourceLock)
			{
				if(!FSources.Contains(provider))
					FSources.Add(provider);
			}
		}
		
		public void Remove(MasterChannel provider)
		{
			lock(FSourceLock)
			{
				FSources.Remove(provider);
			}
		}
		
		//add/remove sinks
		public void AddSink(IAudioSink sink)
		{
			lock(FSourceLock)
			{
				if(!FSinks.Contains(sink))
					FSinks.Add(sink);
			}
		}
		
		public void RemoveSink(IAudioSink sink)
		{
			lock(FSourceLock)
			{
				FSinks.Remove(sink);
			}
		}

		
		/// <summary>
		/// Reads from this provider
		/// </summary>
		float[] FMixerBuffer = new float[1];
		
		//this gets called from the soundcard
		public int Read(byte[] buffer, int offset, int count)
		{
			var channels = WaveFormat.Channels;
			int samplesNeeded = count / (4*channels);
			WaveBuffer wb = new WaveBuffer(buffer);
			
			//fix buffer size
			FMixerBuffer = BufferHelpers.Ensure(FMixerBuffer, samplesNeeded);
			
			//empty buffer
			wb.Clear();
			
			lock(FSources)
			{
				var inputCount = FSources.Count;
				for(int i=0; i<inputCount; i++)
				{
					if(FSources[i].Signal != null)
					{
						//starts the calculation of the audio graph
						FSources[i].Signal.Read(FMixerBuffer, offset / 4, samplesNeeded);
						var chan = FSources[i].Channel % channels;
						
						//add to output buffer
						for(int j=0; j<samplesNeeded; j++)
						{
							wb.FloatBuffer[j*channels + chan] += FMixerBuffer[j];
							FMixerBuffer[j] = 0;
						}
					}
				}
				
				//then evaluate the sinks
				for (int i = 0; i < FSinks.Count; i++)
				{
					FSinks[i].Read(offset / 4, samplesNeeded);
				}
				
				//tell  the engine that reading has finished
				FReadingFinished(samplesNeeded);
			}
			return count; //always run
		}
		
		/// <summary>
		/// The waveformat of this WaveProvider (same as the source)
		/// </summary>
		public WaveFormat WaveFormat
		{
			get;
			set;
		}
	}
}



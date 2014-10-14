#region usings
using System;
using System.Collections.Generic;

using NAudio.Wave;
using NAudio.Utils;

#endregion usings

namespace VVVV.Audio
{
	/// <summary>
	/// A combination of audio signal and the driver output channel number
	/// </summary>
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
                System.Diagnostics.Debug.WriteLine("Sink Count: " + FSinks.Count);
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
			
			//lock(FSources)
			{
				//evaluate the sinks,
				//e.g. buffer writers should write first to have the latest data in the buffer storage
				for (int i = 0; i < FSinks.Count; i++)
				{
					try
					{
						FSinks[i].Read(offset / 4, samplesNeeded);
					}
					catch (Exception e)
					{
						System.Diagnostics.Debug.WriteLine(e.Message);
						System.Diagnostics.Debug.WriteLine(e.Source);
						System.Diagnostics.Debug.WriteLine(e.StackTrace);
					}
				}
					
					//evaluate the inputs
                var inputCount = FSources.Count;
                for (int i = 0; i < inputCount; i++)
                {
                    try
                    {
                        if (FSources[i].Signal != null)
                        {
                            //starts the calculation of the audio graph
                            FSources[i].Signal.Read(FMixerBuffer, offset / 4, samplesNeeded);
                            var chan = FSources[i].Channel % channels;

                            //add to output buffer
                            for (int j = 0; j < samplesNeeded; j++)
                            {
                                wb.FloatBuffer[j * channels + chan] += FMixerBuffer[j];
                                FMixerBuffer[j] = 0;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        System.Diagnostics.Debug.WriteLine(e.Message);
                        System.Diagnostics.Debug.WriteLine(e.Source);
                        System.Diagnostics.Debug.WriteLine(e.StackTrace);
                    }
                }
				
				//tell the engine that reading has finished
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



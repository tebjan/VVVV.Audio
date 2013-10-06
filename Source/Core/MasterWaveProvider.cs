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
	/// <summary>
	/// Helper class for when you need to convert back to an IWaveProvider from
	/// an ISampleProvider. Keeps it as IEEE float
	/// </summary>
	public class MasterWaveProvider : IWaveProvider
	{
		private List<ISampleProvider> source = new List<ISampleProvider>();
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
		
		public void Add(ISampleProvider provider)
		{
			lock(source)
			{
				source.Add(provider);
			}
		}
		
		public void Remove(ISampleProvider provider)
		{
			lock(source)
			{
				source.Remove(provider);
			}
		}
		
		/// <summary>
		/// Reads from this provider
		/// </summary>
		float[] FMixerBuffer = new float[1];
		
		//this gets called from the soundcard
		public int Read(byte[] buffer, int offset, int count)
		{
			int samplesNeeded = count / 4;
			WaveBuffer wb = new WaveBuffer(buffer);
			
			//fix buffer size
			FMixerBuffer = BufferHelpers.Ensure(FMixerBuffer, samplesNeeded);
			
			//empty buffer
			wb.Clear();
			
			lock(source)
			{
				var inputCount = source.Count;
				//var invCount = 1.0f/inputCount;
				for(int i=0; i<inputCount; i++)
				{
					if(source[i] != null)
					{
						//starts the calculation of the audio graph
						source[i].Read(FMixerBuffer, offset / 4, samplesNeeded);
						
						//add to output buffer
						for(int j=0; j<samplesNeeded; j++)
						{
							wb.FloatBuffer[j] += FMixerBuffer[j];
							FMixerBuffer[j] = 0;
						}
					}
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
			protected set;
		}
	}
}



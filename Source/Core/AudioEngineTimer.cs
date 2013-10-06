#region usings
using System;
using System.Collections.Generic;
using NAudio.CoreAudioApi;
using NAudio.Utils;
using NAudio.Wave;
using NAudio.Wave.Asio;
using NAudio.Wave.SampleProviders;
using VVVV.Core.Logging;
using VVVV.PluginInterfaces.V2;

#endregion usings

namespace VVVV.Audio
{
	public class AudioEngineTimer
	{
		protected int FSampleRate;
		protected long FSamplePosition = 0;
		public AudioEngineTimer(int sampleRate)
		{
			FSampleRate = sampleRate;
		}
		
		public void Progress(int samplesCount)
		{
			FSamplePosition += samplesCount;
			FTime = FSamplePosition/(double)FSampleRate;
			FBeat = (FTime/60) * BPM;
		}
		
		public long BufferStart
		{
			get
			{
				return FSamplePosition;
			}
		}
		
		double FTime = 0;
		public double Time
		{
			get
			{
				return FTime;
			}
		}
		
		double FBeat = 0;
		public double Beat
		{
			get
			{
				return FBeat;
			}
		}
		
		public double BPM
		{
			get;
			set;
		}
			
	}
}

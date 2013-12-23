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
			BPM = 120;
		}
		
		public void Progress(int samplesCount)
		{
			FSamplePosition += samplesCount;
			var deltaTime = samplesCount/(double)FSampleRate;
			var deltaBeat = deltaTime * FTimeToBPM;
			FBeat += deltaBeat;
			FTime = FBeat * FBPMToTime;
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
		
		double FBPM;
		double FTimeToBPM;
		double FBPMToTime;
		public double BPM
		{
			get
			{
				return FBPM;
			}
			set
			{
				FBPM = value;
				FTimeToBPM = FBPM/60.0;
				FBPMToTime = 60.0/FBPM;
			}
		}

        public int TimeSignatureDenominator = 4;
        public int TimeSignatureNumerator = 4;
			
	}
}

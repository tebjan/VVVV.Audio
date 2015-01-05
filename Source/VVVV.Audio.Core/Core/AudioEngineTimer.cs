#region usings
using System;

#endregion usings

namespace VVVV.Audio
{
	public class AudioEngineTimer
	{
		protected long FSamplePosition = 0;
		public AudioEngineTimer(int sampleRate)
		{
			FSampleRate = sampleRate;
			BPM = 120;
		}
		
		protected double[] FBeatBuffer = new double[1];
		public void Progress(int samplesCount)
		{
			FSamplePosition += samplesCount;
			
			if(Loop && FLoopSampleLength > 0)
			{
				while(FSamplePosition >= (FLoopStartSample + FLoopSampleLength))
				{
					FSamplePosition -= FLoopSampleLength;
				}
			}
			
			FTime = FSamplePosition/(double)FSampleRate;
			FBeat = FTime * FTimeToBPM;
			
			FillBeatBuffer(samplesCount);
			
		}
		
		public void FillBeatBuffer(int samplesCount)
		{
		    //time buffers
			var sampleToBeat = FTimeToBPM/FSampleRate;
			
			if(FBeatBuffer.Length != samplesCount)
			    FBeatBuffer = new double[samplesCount];
			
			for (int i = 0; i < samplesCount; i++)
			{
			    if(Loop && FLoopSampleLength > 0)
			    {
			        FBeatBuffer[i] = ((FSamplePosition + i) % FLoopSampleLength) * sampleToBeat;
			    }
			    else
			    {
			        FBeatBuffer[i] = (FSamplePosition + i) * sampleToBeat;
			    }
			}
		}

		protected int FSampleRate;
        public int SampleRate 
        {
            get
            {
                return FSampleRate;
            }
            set
            {
                FSampleRate = value;
            }
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
		
        //the beat 1ppq
		double FBeat = 0;
		public double Beat
		{
			get
			{
				return FBeat;
			}
			
			set
			{
				FBeat = value;
				FTime = FBeat * FBPMToTime;
				FSamplePosition = (long)Math.Round(FTime * FSampleRate);
			}
		}
		
		public double[] BeatBuffer
		{
		    get
		    {
		        return FBeatBuffer;
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
		
		long FLoopSampleLength;
		public bool Loop
		{
			get;
			set;
		}
		
		double FLoopStartBeat;
		long FLoopStartSample;
		public double LoopStartBeat 
		{
			get { return FLoopStartBeat; }
			set 
			{ 
				FLoopStartBeat = value;
				CalcLoop();
			}
		}
		
		double FLoopEndBeat;
		long FLoopEndSample;
		public double LoopEndBeat 
		{
			get { return FLoopEndBeat; }
			set 
			{ 
				FLoopEndBeat = value;
				CalcLoop();
			}
		}
		
		private void CalcLoop()
		{
			FLoopStartSample = (long)Math.Round(FLoopStartBeat * FBPMToTime * FSampleRate);
			FLoopEndSample = (long)Math.Round(FLoopEndBeat * FBPMToTime * FSampleRate);
			FLoopSampleLength = Math.Max(FLoopEndSample - FLoopStartSample, 0);
		}

        public int TimeSignatureDenominator = 4;
        public int TimeSignatureNumerator = 4;
			
	}
}

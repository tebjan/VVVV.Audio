#region usings
using System;
using System.IO;
using NAudio.Utils;
#endregion
namespace VVVV.Audio
{
	public class FileStreamSignal : MultiChannelSignal
	{
		public bool FLoop;

		public bool FPlay = false;

		public bool FRunToEndBeforeLooping = true;

		public TimeSpan LoopStartTime;

		public TimeSpan LoopEndTime;

		public TimeSpan FSeekTime;

		public AudioFileReaderVVVV FAudioFile;

		public double Speed 
		{
			get;
			set;
		}

		public void OpenFile(string filename)
		{
			if (FAudioFile != null) 
			{
				FAudioFile.Dispose();
				FAudioFile = null;
			}
			if (!string.IsNullOrEmpty(filename) && File.Exists(filename))
			{
				FAudioFile = new AudioFileReaderVVVV(filename);
				SetOutputCount(FAudioFile.WaveFormat.Channels);
			}
			else 
			{
				SetOutputCount(0);
			}
		}

		float[] FFileBuffer = new float[1];

		double FFractionalAccum;

		//gathers frac error
		BufferWiseResampler FResampler = new BufferWiseResampler();

		protected override void FillBuffers(float[][] buffer, int offset, int sampleCount)
		{
		    if(FAudioFile == null) return;
			var channels = FAudioFile.WaveFormat.Channels;
			var blockAlign = FAudioFile.OriginalFileFormat.BlockAlign;
			int samplesToRead;
			if (Speed == 1.0) 
			{
				samplesToRead = sampleCount * channels;
			}
			else 
			{
				var desiredSamples = sampleCount * channels * Speed;
				//ideal value
				samplesToRead = (int)Math.Truncate(desiredSamples);
				//can only read that much
				var rem = samplesToRead % blockAlign;
				samplesToRead -= rem;
				FFractionalAccum += (desiredSamples - samplesToRead);
				//gather error
				//correct error
				if (FFractionalAccum >= blockAlign) 
				{
					samplesToRead += blockAlign;
					FFractionalAccum -= blockAlign;
				}
			}
			FFileBuffer = BufferHelpers.Ensure(FFileBuffer, samplesToRead);
			int samplesRead = 0;
			if (FPlay && samplesToRead > 0) 
			{
				samplesRead = FAudioFile.Read(FFileBuffer, offset * channels, samplesToRead);
				if (samplesRead == 0) 
				{
					if (FLoop)
					{
						FAudioFile.CurrentTime = LoopStartTime;
						FRunToEndBeforeLooping = false;
						samplesRead = FAudioFile.Read(FFileBuffer, offset * channels, samplesToRead);
					}
					else 
					{
						samplesRead = FFileBuffer.ReadSilence(offset * channels, samplesToRead);
					}
				}
					if (FLoop && FAudioFile.CurrentTime >= LoopEndTime)
					{
						FAudioFile.CurrentTime = LoopStartTime;
						FRunToEndBeforeLooping = false;
						//bytesread = FAudioFile.Read(FFileBuffer, offset*channels, samplesToRead);  		
					}
				}
				if (Speed == 1.0) 
				{
					//copy to output buffers
					for (int i = 0; i < channels; i++)
					{
						for (int j = 0; j < sampleCount; j++) 
						{
							buffer[i][j] = FFileBuffer[i + j * channels];
						}
					}
				}
				else//resample
				{
					FResampler.ResampleDeinterleave(FFileBuffer, buffer, samplesToRead / channels, sampleCount, channels);
				}
			}
			else//silence
			 {
				for (int i = 0; i < channels; i++) 
				{
					buffer[i].ReadSilence(offset, sampleCount);
				}
			}
		}

		public override void Dispose()
		{
			base.Dispose();
		}
	}
}



#region usings
using System;
using System.ComponentModel.Composition;
using NAudio;
using NAudio.Utils;
using NAudio.Wave;
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
	public class FileStreamSignal : MultiChannelSignal
	{
		#region fields & pins
		
		public bool FLoop;
		public bool FIsPlaying = false;
		public bool FRunToEndBeforeLooping = true;
		public TimeSpan LoopStartTime;
		public TimeSpan LoopEndTime;
		public TimeSpan FSeekTime;

		#endregion fields & pins
		
		public CachedAudioResampleFileReader FAudioFile;
		public SilenceProvider FSilence;

		public FileStreamSignal()
			: base(2)
		{
			FSilence = new SilenceProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2));
		}
		
		public void OpenFile(string filename)
		{
			if (FAudioFile != null)
			{
				FAudioFile.Dispose();
			}
			
			FAudioFile = new CachedAudioResampleFileReader(filename, 44100);
			FSilence = new SilenceProvider(FAudioFile.WaveFormat);
			SetOutputCount(FAudioFile.WaveFormat.Channels);
		}
		
		float[] FFileBuffer = new float[1];
		protected override void FillBuffer(float[][] buffer, int offset, int sampleCount)
		{
			
			var channels = FAudioFile.WaveFormat.Channels;
			var samplesToRead = sampleCount*channels;
			FFileBuffer = BufferHelpers.Ensure(FFileBuffer, samplesToRead);
            int bytesread = 0;
			if(FIsPlaying)
			{
	            bytesread = FAudioFile.Read(FFileBuffer, offset*channels, samplesToRead);
	
	            if (bytesread == 0)
	            {
	            	if(FLoop)
	            	{
	            		FAudioFile.CurrentTime = LoopStartTime;
	            		FRunToEndBeforeLooping = false;
		                bytesread = FAudioFile.Read(FFileBuffer, offset*channels, samplesToRead);  		
	            	}
	            	else
	            	{
	            		FIsPlaying = false;
	            		bytesread = FSilence.Read(FFileBuffer, offset*channels, samplesToRead);
	            	}
					
	            }
	            else
	            {
	            	if(FLoop && FAudioFile.CurrentTime >= LoopEndTime)
	            	{
	            		FAudioFile.CurrentTime = LoopStartTime;
	            		FRunToEndBeforeLooping = false;
		                //bytesread = FAudioFile.Read(FFileBuffer, offset*channels, samplesToRead);  		
	            	}
	            }
	            
	            //copy to output buffers
				for (int i = 0; i < channels; i++)
				{
					for (int j = 0; j < sampleCount; j++)
					{
						buffer[i][j] = FFileBuffer[i + j*channels];
					}
				}	            
			}
			else //silence
			{
				for (int i = 0; i < channels; i++) 
				{
					buffer[i].ReadSilence(offset, sampleCount);
				}
			}
						
		}

		public override void Dispose()
		{
			FAudioFile.Dispose();
			base.Dispose();
		}		
	}
	
	//SilenceProvider code from vexorum, http://naudio.codeplex.com/workitem/16377
	public class SilenceProvider : ISampleProvider
	{
	    private readonly WaveFormat format;
	
	    public SilenceProvider(WaveFormat format)
	    {
	        this.format = format;
	    }
	
	    public int Read(float[] buffer, int offset, int count)
	    {
	        for (int i = 0; i < count; i++)
	        {
	            buffer[offset + i] = 0;
	        }
	        return count;
	    }
	
	    public WaveFormat WaveFormat
	    {
	        get { return format; }
	    }
	}	
	
	#region PluginInfo
	[PluginInfo(Name = "FileStream", Category = "Audio", Help = "Plays Back sound files", Tags = "wav, mp3, aiff", Author = "beyon")]
	#endregion PluginInfo
	public class FileStreamNode : GenericMultiChannelAudioSourceNodeWithOutputs<FileStreamSignal>
	{
		#region fields & pins
		[Input("Play")]
		IDiffSpread<bool> FPlay;
		
		[Input("Loop")]
		IDiffSpread<bool> FLoop;
		
		[Input("Loop Start Time")]
		IDiffSpread<double> FLoopStart;
		
		[Input("Loop End Time")]
		IDiffSpread<double> FLoopEnd;
		
		[Input("Do Seek", IsBang = true)]
		IDiffSpread<bool> FDoSeek;
		
		[Input("Seek Time")]
		IDiffSpread<double> FSeekPosition;		
		
		[Input("Volume", DefaultValue = 0.25f)] //What's the best default? Good to have something audible but probably good to avoid max to save ears/speakers
		IDiffSpread<float> FVolume;
		
		[Input("Filename", StringType = StringType.Filename, FileMask="Audio File (*.wav, *.mp3, *.aiff)|*.wav;*.mp3;*.aiff")]
		IDiffSpread<string> FFilename;
		
		[Output("Duration")]
		ISpread<double> FDuration;
		
		[Output("Position")]
		ISpread<double> FPosition;
			
		[Output("Can Seek")]
		ISpread<bool> FCanSeek;
		#endregion fields & pins

		protected override void SetParameters(int i, FileStreamSignal instance)
		{
			if(FFilename.IsChanged)
			{
				instance.OpenFile(FFilename[i]);		
			}

			instance.FAudioFile.Volume = FVolume[i];
			
			if(FPlay.IsChanged)
			{
				instance.FIsPlaying = FPlay[i];
				if((instance.FAudioFile.CurrentTime >= instance.FAudioFile.TotalTime) && FPlay[i])
				{
					instance.FAudioFile.Position = 0;
				}
			}
			
			if(FLoop.IsChanged)
			{
				if(FLoop[i] && instance.FAudioFile.CurrentTime <= instance.LoopEndTime)
				{
					instance.FRunToEndBeforeLooping = false;
				}
				else if (FLoop[i])
				{
					instance.FRunToEndBeforeLooping = true;
				}
				instance.FLoop = FLoop[i];
			}
			
			if(FLoopStart.IsChanged)
			{
				instance.LoopStartTime = TimeSpan.FromSeconds(FLoopStart[i]);
			}	
			
			if(FLoopEnd.IsChanged)
			{
				instance.LoopEndTime = TimeSpan.FromSeconds(Math.Min(FLoopEnd[i], instance.FAudioFile.TotalTime.TotalSeconds));
			}
			
			if( FLoop[i] && !instance.FRunToEndBeforeLooping)
			{
				if(instance.FAudioFile.CurrentTime > instance.LoopEndTime)
				{
					instance.FAudioFile.CurrentTime = instance.LoopStartTime;
				}
			}
			
			if(FSeekPosition.IsChanged)
			{
				instance.FSeekTime = TimeSpan.FromSeconds(Math.Min(instance.FAudioFile.TotalTime.TotalSeconds, FSeekPosition[i]));
			}
			
			if(FDoSeek[i] && instance.FAudioFile.CanSeek)
			{
				instance.FIsPlaying = FPlay[i];
				
				if(instance.FSeekTime > instance.LoopEndTime) instance.FRunToEndBeforeLooping = true;
				if(instance.FSeekTime < instance.LoopStartTime) instance.FRunToEndBeforeLooping = false;
				instance.FAudioFile.CurrentTime = instance.FSeekTime;
			}
		}
		
		protected override void SetOutputs(int i, FileStreamSignal instance)
		{
			if(instance.FAudioFile == null)
			{
				FPosition[i] = 0;
				FDuration[i] = 0;
				FCanSeek[i] = false;
			}
			else
			{
				FPosition[i] = instance.FAudioFile.CurrentTime.TotalSeconds;
				FDuration[i] = instance.FAudioFile.TotalTime.TotalSeconds;
				FCanSeek[i] = instance.FAudioFile.CanSeek;
			}
		}
		
		protected override FileStreamSignal GetInstance(int i)
		{
			return new FileStreamSignal();
		}
	}
	
}

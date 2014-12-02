#region usings
using System;
using System.ComponentModel.Composition;
using System.IO;

using VVVV.Audio;
using VVVV.Core.Logging;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

#endregion usings

namespace VVVV.Nodes
{
	
	#region PluginInfo
	[PluginInfo(Name = "FileStream", Category = "VAudio", Help = "Plays Back sound files", Tags = "wav, mp3, aiff", Author = "tonfilm, beyon")]
	#endregion PluginInfo
	public class FileStreamNode : GenericMultiAudioSourceNode<FileStreamSignal>
	{
		#region fields & pins
		[Input("Play")]
		public IDiffSpread<bool> FPlay;
		
		[Input("Loop")]
		public IDiffSpread<bool> FLoop;
		
		[Input("Loop Start Time")]
		public IDiffSpread<double> FLoopStart;
		
		[Input("Loop End Time")]
		public IDiffSpread<double> FLoopEnd;
		
		[Input("Do Seek", IsBang = true)]
		public IDiffSpread<bool> FDoSeek;
		
		[Input("Seek Time")]
		public IDiffSpread<double> FSeekPosition;

		[Input("Speed", DefaultValue = 1)]
		public IDiffSpread<double> FSpeed;
		
		[Input("Volume", DefaultValue = 1f)]
		public IDiffSpread<float> FVolume;
		
		[Input("Filename", StringType = StringType.Filename, FileMask="Audio File (*.wav, *.mp3, *.aiff, *.m4a)|*.wav;*.mp3;*.aiff;*.m4a")]
		public IDiffSpread<string> FFilename;
		
		[Output("Duration")]
		public ISpread<double> FDurationOut;
		
		[Output("Position")]
		public ISpread<double> FPositionOut;
			
		[Output("Can Seek")]
		public ISpread<bool> FCanSeekOut;
		
		[Output("Sample Rate")]
        public ISpread<int> FSampleRateOut;

		[Output("Channels")]
        public ISpread<int> FChannelsOut;
		
        [Output("Uncompressed Format")]
        public ISpread<string> FFileFormatOut;
		#endregion fields & pins
		

		protected override void SetParameters(int i, FileStreamSignal instance)
		{
		    if(FFilename.IsChanged && (instance.FAudioFile == null || instance.FAudioFile.FFileName != FFilename[i]))
			{ 
				instance.OpenFile(FFilename[i]);
                
                if (instance.FAudioFile == null)
                {
                    FDurationOut[i] = 0;
                    FCanSeekOut[i] = false;
                    FSampleRateOut[i] = 0;
                    FChannelsOut[i] = 0;
                    FFileFormatOut[i] = "";
                }
                else
                {
                	instance.FAudioFile.Volume = FVolume[i];
                	instance.FLoop = FLoop[i];
                	instance.LoopStartTime = TimeSpan.FromSeconds(FLoopStart[i]);
                	instance.LoopEndTime = TimeSpan.FromSeconds(FLoopEnd[i]);
                	
                	SetOutputSliceCount(CalculatedSpreadMax);
                	
                    FDurationOut[i] = instance.FAudioFile.TotalTime.TotalSeconds;
                    FCanSeekOut[i] = instance.FAudioFile.CanSeek;
                    FChannelsOut[i] = instance.FAudioFile.OriginalFileFormat.Channels;
                    FSampleRateOut[i] = instance.FAudioFile.OriginalFileFormat.SampleRate;
                    FFileFormatOut[i] = instance.FAudioFile.OriginalFileFormat.ToString();
                }
		
			}
			
			if (instance.FAudioFile == null) return;

            if (FVolume.IsChanged)
            {
            	instance.FAudioFile.Volume = FVolume[i];
            }
            
            if(FPlay.IsChanged)
            {
            	instance.FPlay = FPlay[i];
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
			
			//TODO: write sample based looping
			if(FLoop[i] && !instance.FRunToEndBeforeLooping)
			{
				if(instance.FAudioFile.CurrentTime > instance.LoopEndTime)
				{
					instance.FAudioFile.CurrentTime = instance.LoopStartTime;
				}
			}
			
			if(FDoSeek[i] && instance.FAudioFile.CanSeek)
			{
				if(instance.FSeekTime > instance.LoopEndTime) instance.FRunToEndBeforeLooping = true;
				if(instance.FSeekTime < instance.LoopStartTime) instance.FRunToEndBeforeLooping = false;
				instance.FAudioFile.CurrentTime = TimeSpan.FromSeconds(FSeekPosition[i]);
			}
			
			if(FSpeed.IsChanged)
				instance.Speed = FSpeed[i];
		}
		
		protected override void SetOutputSliceCount(int sliceCount)
		{
			FPositionOut.SliceCount = sliceCount;
			FDurationOut.SliceCount = sliceCount;
			FChannelsOut.SliceCount = sliceCount;
			FSampleRateOut.SliceCount = sliceCount;
			FCanSeekOut.SliceCount = sliceCount;
			FFileFormatOut.SliceCount = sliceCount;
		}
		
		protected override void SetOutputs(int i, FileStreamSignal instance)
		{
			if(instance.FAudioFile == null)
			{
				FPositionOut[i] = 0;
			}
			else
			{
				FPositionOut[i] = instance.FAudioFile.CurrentTime.TotalSeconds;
			}
		}

        protected override FileStreamSignal GetInstance(int i)
		{
			return new FileStreamSignal();
		}
	}
	
}

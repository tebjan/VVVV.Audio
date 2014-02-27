#region usings
using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

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
	public class WaveFormSignal : MultiChannelSignal
	{

		
		
		public AudioFileReaderVVVV FAudioFile;
		
		public void OpenFile(string filename)
		{
			if (FAudioFile != null)
			{
				FAudioFile.Dispose();
			}
			
			if(!string.IsNullOrEmpty(filename))
			{
				FAudioFile = new AudioFileReaderVVVV(filename, 44100);
				SetOutputCount(FAudioFile.WaveFormat.Channels);
			}
		}
		
		int FSpreadCount;
		public int SpreadCount
		{
			get
			{
				return FSpreadCount;
			}
			set
			{
				FSpreadCount = value;
			}
		}
		
		public Spread<ISpread<double>> WaveFormSpread = new Spread<ISpread<double>>();
		
		float[] FFileBuffer = new float[1];
		protected override void FillBuffers(float[][] buffer, int offset, int sampleCount)
		{
			throw new Exception("Fill buffers should not be called on wave form reader");	
		}
		
		public float MinValue;
		public bool ToMono;
		public bool Loop;
		public double StartTime;
		public double EndTime;
		
		public Task ReadIntoSpreadAsync(CancellationToken ct)
		{
			return Task.Factory.StartNew(() => ReadIntoSpread(ct), ct);
		}
		
		public void ReadIntoSpread(CancellationToken ct)
		{
			var channels = FAudioFile.WaveFormat.Channels;
			long samples = (long)Math.Round(FAudioFile.TotalTime.TotalSeconds * FAudioFile.WaveFormat.SampleRate);
			long startSample = 0;
			
			if(Loop)
			{
				startSample = (long)(StartTime * FAudioFile.WaveFormat.SampleRate);
				samples = (long)((EndTime - StartTime) * FAudioFile.WaveFormat.SampleRate);
			}
			
			var localSpreadCount = (int)Math.Min(SpreadCount, samples);
			
			
			if(ToMono)
			{
				WaveFormSpread.SliceCount = 1;
				WaveFormSpread[0] = new Spread<double>(localSpreadCount);
			}
			else
			{
				WaveFormSpread.SliceCount = channels;
				for (int i = 0; i < channels; i++)
				{
					WaveFormSpread[i] = new Spread<double>(localSpreadCount);
				}
			}

			int blockSize = (int)(samples / localSpreadCount);
			
			FAudioFile.Position = startSample * channels * 4;
			var bufferSize = blockSize * channels;
			var buffer = new float[bufferSize];
			var maxValue = 0.0f;
			var outputBuffers = WaveFormSpread.Select(s => s.Stream.Buffer).ToArray();
			for (int slice = 0; slice < localSpreadCount; slice++)
			{
				//read one interleaved block
				var samplesRead = FAudioFile.Read(buffer, 0, bufferSize);
				
				//split into channels and do the max
				for (int channel = 0; channel < channels; channel++) 
				{
					maxValue = MinValue;
					for (int i = 0; i < samplesRead; i += channels) 
					{
						maxValue = Math.Max(maxValue, Math.Abs(buffer[i + channel]));
					}
					
					if(ToMono)
					{
						outputBuffers[0][slice] = Math.Max(maxValue, outputBuffers[0][slice]);
					}
					else
					{
						outputBuffers[channel][slice] = maxValue;
					}
					
					ct.ThrowIfCancellationRequested();
				}
			}
			
		}

		public override void Dispose()
		{
			FAudioFile.Dispose();
			base.Dispose();
		}		
	}
	
	#region PluginInfo
	[PluginInfo(Name = "WaveForm", Category = "Spreads", Help = "Gets a block max representation of an audio file", Tags = "VAudio")]
	#endregion PluginInfo
	public class WaveFormSpreadNode : GenericMultiAudioSourceNodeWithOutputs<WaveFormSignal>
	{
		#region fields & pins
		[Input("Start Time")]
		public IDiffSpread<double> FLoopStart;
		
		[Input("End Time")]
		public IDiffSpread<double> FLoopEnd;

		[Input("Min Value", DefaultValue = 0.01)]
		public IDiffSpread<float> FMinValueIn;
		
		[Input("Convert to Mono")]
		public IDiffSpread<bool> FConvertToMonoIn;
		
		[Input("Spread Count", DefaultValue = 1)]
		public IDiffSpread<int> FSpreadCount;
		
		[Input("Filename", StringType = StringType.Filename, FileMask="Audio File (*.wav, *.mp3, *.aiff, *.m4a)|*.wav;*.mp3;*.aiff;*.m4a")]
		public IDiffSpread<string> FFilename;
		
		[Output("Wave Form")]
		public ISpread<ISpread<double>> FWaveFormOut;
		
		[Output("Duration")]
		public ISpread<double> FDurationOut;
		
		[Output("Sample Rate")]
        public ISpread<int> FSampleRateOut;

		[Output("Channels")]
        public ISpread<int> FChannelsOut;
		
        [Output("Uncompressed Format")]
        public ISpread<string> FFileFormatOut;
		#endregion fields & pins
		

		protected override PinVisibility GetOutputVisiblilty()
		{
			return PinVisibility.False;
		}
		
		private CancellationTokenSource FCtsSource;
		private Task FCurrentTask;
		protected override async void SetParameters(int i, WaveFormSignal instance)
		{
			if(FFilename.IsChanged)
			{
				instance.OpenFile(FFilename[i]);
                
                if (instance.FAudioFile == null)
                {
                    FDurationOut[i] = 0;
                    FFileFormatOut[i] = "";
                }
                else
                {
                	var duration = instance.FAudioFile.TotalTime.TotalSeconds;
                	instance.StartTime = VMath.Clamp(FLoopStart[i], 0, duration);
                	instance.EndTime = VMath.Clamp(FLoopEnd[i], 0, duration);
                	
                	instance.Loop = instance.StartTime < instance.EndTime;
                	
                	SetOutputSliceCount(CalculatedSpreadMax);
                	
                    FDurationOut[i] = duration;
                    FChannelsOut[i] = instance.FAudioFile.OriginalFileFormat.Channels;
                    FSampleRateOut[i] = instance.FAudioFile.OriginalFileFormat.SampleRate;
                    FFileFormatOut[i] = instance.FAudioFile.OriginalFileFormat.ToString();
                }
		
			}
			
			if(FLoopStart.IsChanged || FLoopEnd.IsChanged)
			{
				var duration = instance.FAudioFile.TotalTime.TotalSeconds;
				instance.StartTime = VMath.Clamp(FLoopStart[i], 0, duration);
				instance.EndTime = VMath.Clamp(FLoopEnd[i], 0, duration);
				
				instance.Loop = instance.StartTime < instance.EndTime;
			}
			
			if(FSpreadCount.IsChanged)
			{
				instance.SpreadCount = FSpreadCount[i];
			}
			
			if(FMinValueIn.IsChanged)
			{
				instance.MinValue = FMinValueIn[i];
			}
			
			if(FConvertToMonoIn.IsChanged)
			{
				instance.ToMono = FConvertToMonoIn[i];
			}
			
			//do the calculation
			CancelCurrentTask();
			FCtsSource = new CancellationTokenSource();
			FCurrentTask = instance.ReadIntoSpreadAsync(FCtsSource.Token);
		}
		
		private void CancelCurrentTask()
		{
			if (FCtsSource != null)
			{
				FCtsSource.Cancel();
				try
				{
					FCurrentTask.Wait();
				}
				catch (Exception)
				{
					// Ignore
				}
				finally
				{
					FCtsSource = null;
					FCurrentTask.Dispose();
				}
			}
		}
		
		public override void Dispose()
		{
			CancelCurrentTask();
			base.Dispose();
		}
		
		protected override void SetOutputSliceCount(int sliceCount)
		{
			
			FWaveFormOut.SliceCount = 0;
			
			FDurationOut.SliceCount = sliceCount;
			FChannelsOut.SliceCount = sliceCount;
			FSampleRateOut.SliceCount = sliceCount;
			FFileFormatOut.SliceCount = sliceCount;
		}
		
		protected override void SetOutputs(int i, WaveFormSignal instance)
		{
			if(instance.FAudioFile == null)
			{
				FWaveFormOut[i] = new Spread<double>(0);
			}
			else
			{
				for (int channel = 0; channel < instance.WaveFormSpread.SliceCount; channel++)
				{
					FWaveFormOut.Add(instance.WaveFormSpread[channel]);
				}
			}
		}

        protected override WaveFormSignal GetInstance(int i)
		{
			return new WaveFormSignal();
		}
	}
	
}

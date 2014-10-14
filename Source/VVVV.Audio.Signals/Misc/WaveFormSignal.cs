#region usings
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VVVV.PluginInterfaces.V2;
#endregion
namespace VVVV.Audio
{
	public class WaveFormSignal : MultiChannelSignal
	{
		public AudioFileReaderVVVV FAudioFile;

		public void OpenFile(string filename)
		{
			if (FAudioFile != null) {
				FAudioFile.Dispose();
				FAudioFile = null;
			}
			if (!string.IsNullOrEmpty(filename) && File.Exists(filename)) {
				FAudioFile = new AudioFileReaderVVVV(filename);
				SetOutputCount(FAudioFile.WaveFormat.Channels);
			}
			else {
				SetOutputCount(0);
			}
		}

		int FSpreadCount;

		public int SpreadCount {
			get {
				return FSpreadCount;
			}
			set {
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

		public void ReadIntoSpreadAsync()
		{
			if (FAudioFile == null)
				return;
			CancelCurrentTask();
			FCtsSource = new CancellationTokenSource();
			FCurrentTask = Task.Factory.StartNew(() => ReadIntoSpread(FCtsSource.Token), FCtsSource.Token);
		}

		public void ReadIntoSpread(CancellationToken ct)
		{
			var channels = FAudioFile.WaveFormat.Channels;
			long samples = (long)Math.Round(FAudioFile.TotalTime.TotalSeconds * FAudioFile.WaveFormat.SampleRate);
			long startSample = 0;
			if (Loop) {
				startSample = (long)(StartTime * FAudioFile.WaveFormat.SampleRate);
				samples = (long)((EndTime - StartTime) * FAudioFile.WaveFormat.SampleRate);
			}
			var localSpreadCount = (int)Math.Min(SpreadCount, samples);
			if (ToMono) {
				WaveFormSpread.SliceCount = 1;
				WaveFormSpread[0] = new Spread<double>(localSpreadCount);
			}
			else {
				WaveFormSpread.SliceCount = channels;
				for (int i = 0; i < channels; i++) {
					WaveFormSpread[i] = new Spread<double>(localSpreadCount);
				}
			}
			int blockSize = (int)(samples / localSpreadCount);
			FAudioFile.Position = startSample * channels * 4;
			var bufferSize = blockSize * channels;
			var buffer = new float[bufferSize];
			var maxValue = 0.0f;
			var outputBuffers = WaveFormSpread.Select(s => s.Stream.Buffer).ToArray();
			for (int slice = 0; slice < localSpreadCount; slice++) {
				//read one interleaved block
				var samplesRead = FAudioFile.Read(buffer, 0, bufferSize);
				//split into channels and do the max
				for (int channel = 0; channel < channels; channel++) {
					maxValue = MinValue;
					for (int i = 0; i < samplesRead; i += channels) {
						maxValue = Math.Max(maxValue, Math.Abs(buffer[i + channel]));
					}
					if (ToMono) {
						outputBuffers[0][slice] = Math.Max(maxValue, outputBuffers[0][slice]);
					}
					else {
						outputBuffers[channel][slice] = maxValue;
					}
					ct.ThrowIfCancellationRequested();
				}
			}
		}

		private CancellationTokenSource FCtsSource;

		private Task FCurrentTask;

		private void CancelCurrentTask()
		{
			if (FCtsSource != null) {
				FCtsSource.Cancel();
				try {
					FCurrentTask.Wait();
				}
				catch (Exception) {
					// Ignore
				}
				finally {
					FCtsSource = null;
					FCurrentTask.Dispose();
				}
			}
		}

		public override void Dispose()
		{
			CancelCurrentTask();
			FAudioFile.Dispose();
			base.Dispose();
		}
	}
}



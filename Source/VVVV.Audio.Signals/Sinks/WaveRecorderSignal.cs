#region usings
using System;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
#endregion
namespace VVVV.Audio
{
	public class WaveRecorderSignal : SinkSignal
	{
		WaveFileWriter FWriter;

		public WaveRecorderSignal()
		{
		}

		private string FFileName;

		public string Filename {
			get {
				return FFileName;
			}
			set {
				if (!string.IsNullOrWhiteSpace(value) && FFileName != value) {
					FFileName = value;
					if (FWriter != null) {
						FWriter.Close();
						FWriter.Dispose();
					}
					FWriter = new WaveFileWriter(FFileName, new WaveFormat(WaveFormat.SampleRate, 16, 1));
					SamplesWritten = 0;
				}
				else {
					SamplesWritten = 0;
					FFlushCounter = 0;
				}
			}
		}

		SampleToWaveProvider16 FWave16Provider;

		protected override void InputWasSet(AudioSignal newInput)
		{
			FWave16Provider = new SampleToWaveProvider16(newInput);
		}

		byte[] FByteBuffer = new byte[1];

		int FFlushCounter = 0;

		bool FLastWriteState;

		protected override void FillBuffer(float[] buffer, int offset, int count)
		{
			if (Write && FInput != null && FWriter != null) {
				var byteCount = count * 2;
				if (FByteBuffer.Length < byteCount)
					FByteBuffer = new byte[byteCount];
				//read bytes from input
				FWave16Provider.Read(FByteBuffer, 0, byteCount);
				//write to stream
				FWriter.Write(FByteBuffer, 0, byteCount);
				SamplesWritten += count;
				FFlushCounter += count;
				if (FFlushCounter >= 32768) {
					FWriter.Flush();
					FFlushCounter = 0;
				}
				FLastWriteState = true;
			}
			else {
				FFlushCounter = 0;
				if (FLastWriteState) {
					FWriter.Flush();
					FLastWriteState = false;
				}
			}
		}

		public int SamplesWritten {
			get;
			protected set;
		}

		public bool Write {
			get;
			set;
		}

		public override void Dispose()
		{
			if (FWriter != null) {
				FWriter.Close();
				FWriter.Dispose();
				FWriter = null;
			}
			base.Dispose();
		}
	}
}





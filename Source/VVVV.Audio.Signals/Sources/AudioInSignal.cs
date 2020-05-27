#region usings
using System;
#endregion
namespace VVVV.Audio
{
	public class AudioInSignal : AudioSignal
	{
		protected AudioEngine FEngine;

		protected int FIndex;

		public AudioInSignal(AudioEngine engine, int index)
		{
			FEngine = engine;
			FIndex = index;
			FEngine.RecordingRequestedStack.Push(this);
		}

		protected override void FillBuffer(float[] buffer, int offset, int count)
		{
			//Asio case:
			if (FEngine.AsioDevice != null)
			{
				Array.Copy(FEngine.InputBuffers[FIndex], offset, buffer, offset, count);
			}
			else
			{
				if (FEngine.SamplesCounter > count)
					FEngine.FWasapiInputBuffers[FIndex].ReadFromLastPosition(buffer, offset, count);
			}
		}

		public override void Dispose()
		{
			FEngine.RecordingRequestedStack.Pop();
			base.Dispose();
		}
	}
}





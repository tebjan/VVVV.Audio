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
				FEngine.FWasapiInputBuffers[FIndex].ReadFromLastPosition(buffer, offset, count);
			}
		}
	}
}





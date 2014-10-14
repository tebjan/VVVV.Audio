#region usings
using System;
using VVVV.Utils.VMath;
#endregion
namespace VVVV.Audio
{
	/// <summary>
	/// Class for LUT playback
	/// </summary>
	public class WaveTableSignal : AudioSignal
	{
		//constructor
		public WaveTableSignal()
		{
			LUT = new float[1024];
			LUTBuffer = new float[1024];
			DlyBufferSize = 2 * MaxDlyTime;
			DlyBuffer = new float[DlyBufferSize];
		}

		double FFrequency;

		public double Frequency {
			get {
				return FFrequency;
			}
			set {
				FFrequency = value;
			}
		}

		private float Delta;

		private float FIndex;

		private float[] LUT;

		public float[] LUTBuffer {
			get;
			private set;
		}

		public void SwapBuffers()
		{
			var tmp = LUT;
			LUT = LUTBuffer;
			LUTBuffer = tmp;
		}

		int i = 0;

		int j = 0;

		float fDlyTime = 0.25f;

		public float DelayTime {
			get {
				return fDlyTime;
			}
			set {
				fDlyTime = (float)VMath.Clamp(value, 0.0, 1.0);
			}
		}

		int MaxDlyTime = 44100 * 2;

		int DlyBufferSize;

		float[] DlyBuffer;

		public float DelayAmount;

		protected unsafe override void FillBuffer(float[] buffer, int offset, int count)
		{
			var luts = LUT.Length;
			var Delta = (float)(FFrequency * luts / WaveFormat.SampleRate);
			fixed (float* lut = LUT) {
				fixed (float* outBuff = buffer) {
					for (int n = 0; n < count; n++) {
						if (i >= DlyBufferSize)
							i = 0;
						j = i - (int)(fDlyTime * MaxDlyTime);
						if (j < 0)
							j += DlyBufferSize;
						var round = (int)FIndex;
						var index = (int)Math.Floor(FIndex);
						var s1 = LUT[index % luts];
						//var s2 = LUT[(index + 1)%luts];
						outBuff[n + offset] = DlyBuffer[i] = s1 + DlyBuffer[j] * DelayAmount;
						FIndex = (FIndex + Delta) % luts;
						i++;
					}
				}
			}
		}
	}
}





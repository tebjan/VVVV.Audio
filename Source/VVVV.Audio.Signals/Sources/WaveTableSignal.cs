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
		}

		double FFrequency;

		public double Frequency 
		{
			get 
			{
				return FFrequency;
			}
			set 
			{
				FFrequency = value;
			}
		}

		private float Delta;

		private double FIndex;

		private float[] LUT;

		public float[] LUTBuffer 
		{
			get;
			set;
		}

        //should be called from outside when new data present in the LUTBuffer
		public void SwapBuffers()
		{
			var tmp = LUT;
			LUT = LUTBuffer;

            //reuse old LUT if same size
            if (LUTBuffer.Length == tmp.Length)
            {
                LUTBuffer = tmp;
            }
            else
            {
                LUTBuffer = new float[LUTBuffer.Length];
            }
		}

		int i = 0;

		protected unsafe override void FillBuffer(float[] buffer, int offset, int count)
		{
            //capture LUT
            var lutData = LUT;
			var luts = lutData.Length;
			var Delta = (float)(FFrequency * luts / WaveFormat.SampleRate);
			fixed (float* lut = lutData) 
			{
				fixed (float* outBuff = buffer) 
				{
					for (int n = 0; n < count; n++) 
					{
                        if (FIndex >= luts) FIndex = 0;

                        var index = (int)Math.Floor(FIndex);
                        
						outBuff[n + offset] = lut[index];
						FIndex = (FIndex + Delta);
						i++;
					}
				}
			}
		}
	}
}





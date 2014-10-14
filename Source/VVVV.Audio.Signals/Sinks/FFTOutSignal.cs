#region usings
using System;
using Lomont;
#endregion
namespace VVVV.Audio
{
	public class FFTOutSignal : SinkSignal<double[]>
	{
		protected LomontFFT FFFT = new LomontFFT();

		public FFTOutSignal(AudioSignal input)
		{
			FSource = input;
		}

		AudioSignal FSource;

		double[] FFFTBuffer = new double[1];

		protected override void FillBuffer(float[] buffer, int offset, int count)
		{
			if (FInput != null) {
				if (FFFTBuffer.Length != count)
					FFFTBuffer = new double[count];
				FSource.Read(buffer, offset, count);
				buffer.ReadDouble(FFFTBuffer, offset, count);
				FFFT.RealFFT(FFFTBuffer, true);
				this.SetLatestValue(FFFTBuffer);
			}
			else {
				this.SetLatestValue(new double[1]);
			}
		}
	}
}





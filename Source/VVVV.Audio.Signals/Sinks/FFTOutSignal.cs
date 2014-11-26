#region usings
using System;
using Lomont;
#endregion
namespace VVVV.Audio
{
    public enum FFTWindowFunction
    {
        None,
        Hamming,
        Hann,
        BlackmannHarris
    }
    
	public class FFTOutSignal : SinkSignal<double[]>
	{
		protected LomontFFT FFFT = new LomontFFT();
		protected CircularBuffer FRingBuffer = new CircularBuffer(512);

		public FFTOutSignal(AudioSignal input)
		{
			FSource = input;
		}

		public int Size 
        {
            get 
            {
                return FRingBuffer.Size; 
            }
            set 
            { 
                FRingBuffer.Size = value;
            }
        }
		
        WindowFunction windowFunc;
        
        public WindowFunction WindowFunc 
        {
            get 
            { 
                return windowFunc; 
            }
            set 
            { 
                if(windowFunc != value)
                {
                    windowFunc = value;
                    FWindow = AudioUtils.CreateWindowDouble(FRingBuffer.Size, WindowFunc);
                }
            }
        }
        
        
		AudioSignal FSource;
		
		public int BufferSize;

		double[] FFFTBuffer = new double[1];
		double[] FOutBuffer = new double[1];
		double[] FWindow = new double[1];

		protected override void FillBuffer(float[] buffer, int offset, int count)
		{
			if (FInput != null) 
			{
			    FSource.Read(buffer, offset, count);
			    
			    //write to buffer
			    FRingBuffer.Write(buffer, offset, count);
			    
			    //calc fft
			    var fftSize = FRingBuffer.Size;
			    
				if (FFFTBuffer.Length != fftSize)
				{
					FFFTBuffer = new double[fftSize];
					FOutBuffer = new double[fftSize];
					FWindow = AudioUtils.CreateWindowDouble(fftSize, WindowFunc);
				}
			
				FRingBuffer.ReadDoubleWindowed(FFFTBuffer, FWindow, 0, fftSize);
				FFFT.RealFFT(FFFTBuffer, true);
				Array.Copy(FFFTBuffer, FOutBuffer, fftSize);
				this.SetLatestValue(FOutBuffer);
			}
			else 
			{
				this.SetLatestValue(new double[1]);
			}
		}
	}
}





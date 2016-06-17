/*
 * Created by SharpDevelop.
 * User: TF
 * Date: 02.05.2015
 * Time: 12:09
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using Lomont;

namespace VVVV.Audio
{
    public class IFFTPullBuffer : CircularPullBuffer
    {
        public IFFTPullBuffer(SigParam<double[]> fftDataReal, SigParam<double[]> fftDataImag, int size, WindowFunction window)
            : base(size)
        {
            FFFTDataReal = fftDataReal;
            FFFTDataImag = fftDataImag;
            RealImagData = new double[size];
            TimeDomain = new float[size];
            Window = AudioUtils.CreateWindowDouble(size, window);
            WindowFunc = window;
            PullCount = size;
        }

        readonly SigParam<double[]> FFFTDataReal;
        readonly SigParam<double[]> FFFTDataImag;
        readonly double[] RealImagData;
        readonly double[] Window;
        public readonly WindowFunction WindowFunc;
        readonly float[] TimeDomain;
        readonly LomontFFT FFFT = new LomontFFT();

        public double Gain = 1;

        public override void Pull(int count)
        {
            var real = FFFTDataReal.Value;
            var imag = FFFTDataImag.Value;
            var copyCount = Math.Min(count / 2, Math.Max(real.Length, imag.Length));
            var j = 0;

            if (real.Length > imag.Length)
            {
                for (int i = 0; i < copyCount; i++)
                {
                    RealImagData[j++] = real[i];
                    RealImagData[j++] = imag[AudioUtils.Zmod(i, imag.Length)];
                }
            }
            else if(real.Length == imag.Length)
            {
                for (int i = 0; i < copyCount; i++)
                {
                    RealImagData[j++] = real[i];
                    RealImagData[j++] = imag[i];
                }
            }
            else
            {
                for (int i = 0; i < copyCount; i++)
                {
                    RealImagData[j++] = real[AudioUtils.Zmod(i, real.Length)];
                    RealImagData[j++] = imag[i];
                }
            }

            if(copyCount < count/2)
            {

                for (int i = copyCount; i < count/2; i++)
                {
                    RealImagData[j++] = 0;
                    RealImagData[j++] = 0;
                }
            }

            FFFT.RealFFT(RealImagData, false);

            //RealImagData[0] = RealImagData[1];

            TimeDomain.WriteDoubleWindowed(RealImagData, Window, 0, count, Gain);

            Write(TimeDomain, 0, count);
        }
    }

    /// <summary>
    /// Description of IFFTSignal.
    /// </summary>
    public class IFFTSignal : AudioSignal
    {
        SigParam<double[]> FFTDataReal = new SigParam<double[]>("FFT Data Real");
        SigParam<double[]> FFTDataImag = new SigParam<double[]>("FFT Data Imaginary");
        SigParam<WindowFunction> FWindowFunc = new SigParam<WindowFunction>("Window Function");
        SigParam<double> FGain = new SigParam<double>("Gain", 0.5);
        SigParam<int> BufferSize = new SigParam<int>("IFFT Buffer Size", true);
        
        public IFFTSignal()
        {
        }

        IFFTPullBuffer FIFFTBuffer;

        int NextPow2(int val)
        {
            var result = 2;
            while(result < val)
            {
                result *= 2;
            }

            return result;
        }

        protected override void FillBuffer(float[] buffer, int offset, int count)
        {
            //recreate ring buffer?
            var size = Math.Max(NextPow2(FFTDataReal.Value.Length), NextPow2(FFTDataImag.Value.Length)) * 2;

            if (size < count)
            {
                BufferSize.Value = 0;
                return;
            }

            if(FIFFTBuffer == null || size != FIFFTBuffer.PullCount || FWindowFunc.Value != FIFFTBuffer.WindowFunc)
            {
                FIFFTBuffer = new IFFTPullBuffer(FFTDataReal, FFTDataImag, size, FWindowFunc.Value);
                BufferSize.Value = size;
            }

            FIFFTBuffer.Gain = FGain.Value;

            //perform IFFT
            FIFFTBuffer.Read(buffer, offset, count);
        }
    }
}

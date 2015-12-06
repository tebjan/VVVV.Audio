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
    /// <summary>
    /// Description of IFFTSignal.
    /// </summary>
    public class IFFTSignal : AudioSignal
    {
        SigParam<double[]> FFTData = new SigParam<double[]>("FFT Data");
        SigParam<float> BufferSize = new SigParam<float>("Buffer Size", true);
        
        LomontFFT FFFT = new LomontFFT();
        
        public IFFTSignal()
        {
        }
        
        double[] RealImagData = new double[1];
        
        protected override void FillBuffer(float[] buffer, int offset, int count)
        {
            //perform IFFT

            base.FillBuffer(buffer, offset, count);
        }
    }
}

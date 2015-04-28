/*
 * Created by SharpDevelop.
 * User: TF
 * Date: 31.12.2014
 * Time: 19:35
 * 
 */
using System;

namespace VVVV.Audio
{
    public enum WaveShaperCurve
    {
        TarrabiaDeJong,
        Watte
    }
        
    
    /// <summary>
    /// Description of ValueToAudioSignal.
    /// </summary>
    public class WaveShaperSignal : AudioSignal
    {
        SigParamAudio FAudioIn = new SigParamAudio("Input");
        SigParam<float> FDistortion = new SigParam<float>("Distortion");
        SigParam<WaveShaperCurve> FCurve = new SigParam<WaveShaperCurve>("Curve");

        public WaveShaperSignal()
        {
        }
        

        
        protected override void FillBuffer(float[] buffer, int offset, int count)
        {
            switch (FCurve.Value) 
            {
                
                case WaveShaperCurve.TarrabiaDeJong:
                    TarrabiaDeJong(buffer, offset, count);
                    break;
                case WaveShaperCurve.Watte:
                    Watte(buffer, offset, count);
                    break;
            }
        }
                
        void TarrabiaDeJong(float[] buffer, int offset, int count)
        {
            var k = 2*FDistortion.Value/(1-FDistortion.Value);
            FAudioIn.Read(buffer, offset, count);
            for (int i = 0; i < count; i++) 
            {
                var x = buffer[i];
                buffer[i] = (1+k)*x/(1+k* Math.Abs(x));
            }
        }
        
        void Watte(float[] buffer, int offset, int count)
        {
            var z = (float)Math.PI * FDistortion.Value;
            var s = 1/ (float)Math.Sin(z);
            var b = 1/FDistortion.Value;
            
            FAudioIn.Read(buffer, offset, count);
            for (int i = 0; i < count; i++)
            {
                var x = buffer[i];
                if (x > b)
                    buffer[i] = 1;
                else
                    buffer[i] = (float)Math.Sin(z*x)*s;
            }
        }
        
    }
}

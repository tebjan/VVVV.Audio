/*
 * Created by SharpDevelop.
 * User: TF
 * Date: 13.12.2014
 * Time: 16:16
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;

namespace VVVV.Audio
{
    public class MoogLowpassSignal : AudioSignal
    {
        SigParamAudio FInput = new SigParamAudio("Input");
        SigParamDiff<float> Frequency = new SigParamDiff<float>("Cutoff", 20000);
        SigParamDiff<float> Resonance = new SigParamDiff<float>("Resonance");

        float cutoff = 20000;
        float res;
        float y1,y2,y3,y4;
        float oldx;
        float oldy1,oldy2,oldy3;
        float x;
        float r;
        float p;
        float k;
        
        public MoogLowpassSignal()
        {
            Init();
            Frequency.ValueChanged = SetCutoff;
            Resonance.ValueChanged = SetRes;
        }

        void Init()
        {
            // initialize values
            y1=y2=y3=y4=oldx=oldy1=oldy2=oldy3=0;
            CalcCoeffs();
        }

        void CalcCoeffs()
        {
            float f = (cutoff+cutoff) / SampleRate; //[0 - 1]
            p=f*(1.8f-0.8f*f);
            k=p+p-1.0f;

            float t=(1.0f-p)*1.386249f;
            float t2=12.0f+t*t;
            r = res*(t2+6.0f*t)/(t2-6.0f*t);
        }
        
        void CalcCoeffs2()
        {
            float f = (cutoff+cutoff) / SampleRate; //[0 - 1]
            // empirical tuning
            p = f * (1.8f - 0.8f * f);
            // k = p + p - T(1.0);
            // A much better tuning seems to be:
            k = 2.0f * (float)Math.Sin(f * Math.PI * 0.5f) - 1.0f;

            float t1 = (1.0f - p) * 1.386249f;
            float t2 = 12.0f + t1 * t1;
            r = res * (t2 + 6.0f * t1) / (t2 - 6.0f * t1);
        }

        void SetCutoff(float c)
        { 
            cutoff=c; 
            CalcCoeffs2();
        }

        void SetRes(float r)
        { 
            res=r; 
            CalcCoeffs2(); 
        }
        
        float[] FInputBuffer = new float[1];
        protected override void FillBuffer(float[] buffer, int offset, int count)
        {
            if(FInputBuffer.Length < count)
            {
                FInputBuffer = new float[count];
            }
            
            FInput.Read(FInputBuffer, offset, count);
            
            for (int i = 0; i < count; i++)
            {
                // process input
                x = FInputBuffer[i] - r*y4;

                //Four cascaded onepole filters (bilinear transform)
                y1= x*p +  oldx*p - k*y1;
                y2=y1*p + oldy1*p - k*y2;
                y3=y2*p + oldy2*p - k*y3;
                y4=y3*p + oldy3*p - k*y4;

                //Clipper band limited sigmoid
                y4-=(y4*y4*y4)/6.0f;

                oldx = x; oldy1 = y1; oldy2 = y2; oldy3 = y3;
                buffer[i] = y4;
            }
        }
        
    }
}

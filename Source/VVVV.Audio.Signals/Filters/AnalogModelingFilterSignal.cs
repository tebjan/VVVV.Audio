/*
 * Created by SharpDevelop.
 * User: TF
 * Date: 13.12.2014
 * Time: 16:16
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using VVVV.Utils.VMath;
namespace VVVV.Audio
{
    public enum AnalogModelingFilterAlgorithm
    {
        MoogLadder = 0,
        TransistorLadder
    }
    
    public enum AnalogModelingFilterType
    {
        LowPass = 0,
        HighPass,
        BandPass
    }
    
    public class AnalogModelingFilterSignal : AudioSignal
    {
        SigParamAudio Input = new SigParamAudio("Input");
        SigParamAudio Frequency = new SigParamAudio("Frequency");
        SigParamAudio Resonance = new SigParamAudio("Resonance");
        SigParamDiff<float> FrequencyOffset = new SigParamDiff<float>("Cutoff Offset");
        SigParamDiff<float> ResonanceOffset = new SigParamDiff<float>("Resonance Offset");
        SigParam<int> OutSelect = new SigParam<int>("Pole Count", 4);
        SigParam<AnalogModelingFilterAlgorithm> Algorithm = new SigParam<AnalogModelingFilterAlgorithm>("Filter Algorithm");
        SigParam<AnalogModelingFilterType> FilterType = new SigParam<AnalogModelingFilterType>("Filter Type");
        

        float FCutoffOffset;

        float FResonanceOffset;

        float y1, y2, y3, y4;

        float oldx;

        float oldy1, oldy2, oldy3;

        float FResoCoeff;

        float p;

        float k;

		public AnalogModelingFilterSignal()
		{
			Init();
			FrequencyOffset.ValueChanged = SetCutoff;
			ResonanceOffset.ValueChanged = SetRes;
		}
		
		float[] FInputBuffer = new float[1];
		float[] FFreqBuffer = new float[1];
		float[] FResoBuffer = new float[1];

		protected override void FillBuffer(float[] buffer, int offset, int count)
		{
			if (FInputBuffer.Length < count) 
			{
				FInputBuffer = new float[count];
				FFreqBuffer = new float[count];
				FResoBuffer = new float[count];
			}
			
			
			Input.Read(FInputBuffer, offset, count);
			Frequency.Read(FFreqBuffer, offset, count);
			Resonance.Read(FResoBuffer, offset, count);
			
			switch (Algorithm.Value) 
			{
			    case AnalogModelingFilterAlgorithm.MoogLadder:
			        MoogLadder(buffer, offset, count);
			        break;
			    case AnalogModelingFilterAlgorithm.TransistorLadder:
			        TransistorLadder(buffer, offset, count);
			        break;
			}
		}
		
		//// LICENSE TERMS: Copyright 2012 Teemu Voipio
		// 
		// You can use this however you like for pretty much any purpose,
		// as long as you don't claim you wrote it. There is no warranty.
        //
		// Distribution of substantial portions of this code in source form
		// must include this copyright notice and list of conditions.
        //

		// input delay and state for member variables
		double zi;
		double[] s = { 0, 0, 0, 0 };

		// tanh(x)/x approximation, flatline at very high inputs
		// so might not be safe for very large feedback gains
		// [limit is 1/15 so very large means ~15 or +23dB]
		double tanhXdX(double x)
		{
		    double a = x*x;
		    // IIRC I got this as Pade-approx for tanh(sqrt(x))/sqrt(x)
		    return ((a + 105)*a + 945) / ((15*a + 420)*a + 945);
		}
		
		double Ff, Fr;
		void CalcTransistorCoeffs(float cutoff, float resonance)
		{
		    // tuning and feedbacc
		    Ff = Math.Tan(Math.PI * (VMath.Clamp(cutoff + FCutoffOffset, 15, SampleRate * 0.45))/SampleRate);
		    Fr = (80.0/9.0) * (resonance + FResonanceOffset);
		}

		// cutoff as normalized frequency (eg 0.5 = Nyquist)
		// resonance from 0 to 1, self-oscillates at settings over 0.9
		void TransistorLadder(float[] @out, int offset, int nsamples)
		{
		    for(int n = 0; n < nsamples; ++n)
		    {
		        CalcTransistorCoeffs(FFreqBuffer[n] * (SampleRate *0.5f), FResoBuffer[n]);
		        
		        // input with half delay, for non-linearities
		        double ih = 0.5 * (FInputBuffer[n] + zi);
		        zi = FInputBuffer[n];

		        // evaluate the non-linear gains
		        double t0 = tanhXdX(ih - Fr * s[3]);
		        double t1 = tanhXdX(s[0]);
		        double t2 = tanhXdX(s[1]);
		        double t3 = tanhXdX(s[2]);
		        double t4 = tanhXdX(s[3]);

		        // g# the denominators for solutions of individual stages
		        double g0 = 1 / (1 + Ff*t1), g1 = 1 / (1 + Ff*t2);
		        double g2 = 1 / (1 + Ff*t3), g3 = 1 / (1 + Ff*t4);
		        
		        // f# are just factored out of the feedback solution
		        double f3 = Ff*t3*g3, f2 = Ff*t2*g2*f3, f1 = Ff*t1*g1*f2, f0 = Ff*t0*g0*f1;

		        // solve feedback
		        double y3 = (g3*s[3] + f3*g2*s[2] + f2*g1*s[1] + f1*g0*s[0] + f0*FInputBuffer[n]) / (1 + Fr*f0);

		        // then solve the remaining outputs (with the non-linear gains here)
		        double xx = t0*(FInputBuffer[n] - Fr*y3);
		        double y0 = t1*g0*(s[0] + Ff*xx);
		        double y1 = t2*g1*(s[1] + Ff*y0);
		        double y2 = t3*g2*(s[2] + Ff*y1);

		        // update state
		        s[0] += 2*Ff * (xx - y0);
		        s[1] += 2*Ff * (y0 - y1);
		        s[2] += 2*Ff * (y1 - y2);
		        s[3] += 2*Ff * (y2 - t4*y3);

		        
		        switch (FilterType.Value) 
				{
				    case AnalogModelingFilterType.LowPass:
		                switch (OutSelect.Value) 
		                {
		                    case 4:
		                        @out[n] = (float)y3;
		                        break;
		                    case 0:
		                        @out[n] = FInputBuffer[n];
		                        break;
		                    case 1:
		                        @out[n] = (float)y0;
		                        break;
		                    case 2:
		                        @out[n] = (float)y1;
		                        break;
		                    case 3:
		                        @out[n] = (float)y2;
		                        break;    
		                    default:
		                        @out[n] = (float)y3;
		                        break;		                        
		                }
				        break;
				    case AnalogModelingFilterType.HighPass:
				        @out[n] = (float)(FInputBuffer[n] - y3);
				        break;
				    case AnalogModelingFilterType.BandPass:
				        @out[n] = (float)(y0 - y3);
				        break;
				}
		    }
		}
		

		void Init()
		{
			// initialize values
			y1 = y2 = y3 = y4 = oldx = oldy1 = oldy2 = oldy3 = 0;
		}
		
		void SetCutoff(float c)
		{
		    FCutoffOffset = c;
		}

		void SetRes(float r)
		{
		    FResonanceOffset = r * 0.5f;
		}

		void CalcCoeffs()
		{
		    var f = (float)Math.Min(FCutoffOffset, SampleRate * 0.34);
		    f = (f + f) / SampleRate;
		    //[0 - 1]
		    p = f * (1.8f - 0.8f * f);
		    k = p + p - 1.0f;
			var t = (1.0f - p) * 1.386249f;
			var t2 = 12.0f + t * t;
			FResoCoeff = FResonanceOffset * (t2 + 6.0f * t) / (t2 - 6.0f * t);
		}

		void CalcCoeffs2(float cutoff, float resonance)
		{
			var f = (float)VMath.Clamp(cutoff + FCutoffOffset, 15, SampleRate * 0.25);
		    f = (f + f) / SampleRate;
			//[0 - 1]
			// empirical tuning
			p = f * (1.8f - 0.8f * f);
			// k = p + p - T(1.0);
			// A much better tuning seems to be:
			k = 2.0f * (float)Math.Sin(f * Math.PI * 0.5f) - 1.0f;
			var t1 = (1.0f - p) * 1.386249f;
			var t2 = 12.0f + t1 * t1;
			FResoCoeff = (resonance + FResonanceOffset) * (t2 + 6.0f * t1) / (t2 - 6.0f * t1);
		}
		
		void MoogLadder(float[] buffer, int offset, int count)
		{
		    for (int i = 0; i < count; i++)
		    {
		        
		        //calc coeffs
		        CalcCoeffs2(FFreqBuffer[i] * (SampleRate *0.5f), FResoBuffer[i]);
		        
		        // process input
		        var x = FInputBuffer[i] - FResoCoeff * y4;
		        //Four cascaded onepole filters (bilinear transform)
		        y1 = x * p + oldx * p - k * y1;
		        y2 = y1 * p + oldy1 * p - k * y2;
		        y3 = y2 * p + oldy2 * p - k * y3;
		        y4 = y3 * p + oldy3 * p - k * y4;
		        //Clipper band limited sigmoid
		        y4 -= (y4 * y4 * y4) / 6.0f;
				oldx = x;
				oldy1 = y1;
				oldy2 = y2;
				oldy3 = y3;
				
				switch (FilterType.Value) 
				{
				    case AnalogModelingFilterType.LowPass:
				        buffer[i] = y4;
				        break;
				    case AnalogModelingFilterType.HighPass:
				        buffer[i] = FInputBuffer[i] - y4;
				        break;
				    case AnalogModelingFilterType.BandPass:
				        buffer[i] = y1 - y4;
				        break;
				}
				
			}
		}

	}
}



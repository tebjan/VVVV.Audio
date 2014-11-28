#region usings
using System;
using System.ComponentModel.Composition;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using VVVV.Core.Logging;
using VVVV.Audio;
#endregion usings

namespace VVVV.Nodes
{
	public class TestOscSignal : AudioSignal
	{
		public TestOscSignal()
	    {
	        //get param change events
	        Frequency.ValueChanged = CalcFrequencyConsts;
	        Slope.ValueChanged = CalcTriangleCoefficients;
	    }
	    
	    protected override void Engine_SampleRateChanged(object sender, EventArgs e)
	    {
	        base.Engine_SampleRateChanged(sender, e);
	        CalcFrequencyConsts(Frequency.Value);
	    }

	    public SigParamDiff<float> Frequency = new SigParamDiff<float>("Frequency", 440);
	    public SigParam<WaveFormSelection> WaveForm = new SigParam<WaveFormSelection>("Wave Form");
	    public SigParamDiff<float> Slope = new SigParamDiff<float>("Symmetry", 0.5f);
	    public SigParam<bool> UseEPTR = new SigParam<bool>("Use EPTR");
	    public SigParam<AudioSignal> FMInput = new SigParam<AudioSignal>("FM");
	    public SigParam<float> FMLevel = new SigParam<float>("FM Level");
		public SigParam<float> Gain = new SigParam<float>("Gain");

		const float TwoPi = (float)(Math.PI * 2);
		const float HalfPi = (float)(Math.PI * 0.5);
		float FPhase = -1;
        float T; 
        
        // time step and triangle params
        void CalcFrequencyConsts(float freq)
        {
            T = freq / SampleRate;
            CalcTriangleCoefficients(Slope.Value);
        }

        //triangle precalculated values
        bool FTriangleUp = true;
        float A, B, AoverB, BoverA, a2, a1, a0, b2, b1, b0;

        void CalcTriangleCoefficients(float slope)
        {
            //triangle magnitudes
            var slopeClamp = (float)VMath.Clamp(slope, 0.01, 0.99);
            A = 1/slopeClamp;
            B = -1/(1-slopeClamp);
            AoverB = A / B;
            BoverA = B / A;
            
            var t4 = 4*T;
            var t2 = 2*T;
            
            //coeffs max
            var rezDenomA = 1 / (t4*(A-1));
            
            a2 = -rezDenomA;
           
            a1 = (t2*A - t4 + 2) * rezDenomA;
            
            var tmp = A*T - 1;
            a0 = -(tmp*tmp) * rezDenomA;
            
            //coeffs min
            var rezDenomB = 1 / (t4*(B+1));
            
            b2 = -rezDenomB;
            b1 = (t2*B + t4 - 2) * rezDenomB;
            
            tmp = B*T + 1;
            b0 = -(tmp*tmp) * rezDenomB;
        }
		
		float eptr(float ramp, float regionSize, float slope)
		{
			slope *= 0.5f;
		
			//Buffer buf("eptr");         // loads eptr buffer from Max
			//d2 = 8192  / d1;            // buffer transition coefficient;
			var d2 = TwoPi *.213332 / regionSize; // transcendental coefficient;
			var slopeMin = 1-slope;                  // inverted duty cycle
			var e0= 0.0f; // pi5  = pi *.5;
			
			if (ramp <=  slope - regionSize)        // fixed low region at start
				return -1;
			else if (ramp < slope + regionSize) 
			{   // rising region
				//e0 = peek(buf, d2*(ramp -w1 +regionSize), 0);
				e0 = (float)Math.Tanh(4 * Math.Sin(d2 * (ramp -slope +regionSize) - HalfPi));
				return e0;
			}
			else if (ramp <= slopeMin - regionSize)    // middle fixed hi region
				return 1;
			else if (ramp < slopeMin + regionSize)
			{     // falling region
				//e0 = peek(buf,d2*(ramp -slopeMin +regionSize), 0);
				e0 = (float)Math.Tanh(4 * Math.Sin(d2*(ramp -slopeMin +regionSize) + HalfPi));
				return e0;
			}
			
			else return -1;             // fixed low region at end
		}
		
		float[] FMBuffer = new float[1];
        private void OscEPTR(float[] buffer, int count)
        {
            bool sync = false;
            var slope = Slope.Value;
        	
            var t2 = 2*T;
            switch (WaveForm.Value)
            {
                case WaveFormSelection.Sine:
            		for (int i = 0; i < count; i++)
                    {
                        buffer[i] = Gain.Value * (float)Math.Sin(FPhase*Math.PI);
                        
                        FPhase += t2 + FMBuffer[i]*FMLevel.Value;
                        
                        if (FPhase > 1)
                            FPhase -= 2;

                    }
                    break;
                
                case WaveFormSelection.Sawtooth: 
                    slope = AudioUtils.Wrap(slope + 0.5f, 0, 1);
                    goto case WaveFormSelection.Triangle;
                    
                case WaveFormSelection.Triangle:

                    //per sample loop
                    for (int i = 0; i < count; i++)
                    {
                        float sample;

                        if (slope >= 0.99f) // rising saw
                        { 
                            FPhase = sync ? -1 : FPhase + t2 + FMBuffer[i]*FMLevel.Value;
                            if (FPhase > 1.0f - T) //transition
                            {
                                sample = FPhase - (FPhase / T) + (1.0f / T) - 1.0f;
                                FPhase -= 2.0f;
                            }
                            else
                            {
                                sample = FPhase;
                            }
                        }
                        else if (slope <= 0.01f) // falling saw
                        { 	
                            FPhase = sync ? -1 : FPhase + t2 + FMBuffer[i]*FMLevel.Value;                            
                            if (FPhase > 1.0f - T) //transition
                            {
                                sample = -FPhase + (FPhase / T) - (1.0f / T) + 1.0f;
                                FPhase -= 2.0f;
                            }
                            else
                            {
                                sample = -FPhase;
                            }
                        }
                        else //triangle
                        { 	
                            if(FTriangleUp) //counting up
                            {
                            	FPhase = FPhase + t2*A;
                            	if (FPhase > 1 - A*T)
                            	{
                            		//transitionregion
                            		sample = a2 * (FPhase * FPhase) + a1 * FPhase + a0;
                            		FPhase = 1 + (FPhase - 1) * BoverA;
                            		FTriangleUp = false;
                            	}
                            	else //linearregion
                            	{
                            		sample = FPhase;
                            	}
                            }
                            else //counting down
                            {
                            	FPhase = FPhase + t2*B;
                            	if (FPhase < -1 - B*T)
                            	{
                            		//transitionregion
                            		sample = b2 * (FPhase * FPhase) + b1 * FPhase + b0;
                            		FPhase = -1 + (FPhase + 1) * AoverB;
                            		FTriangleUp = true;
                            	}
                            	else //linearregion
                            	{
                            		sample = FPhase;
                            	}
                            }
                        }
                        
                        buffer[i] = sample*Gain.Value;
                    }
            	break;
            	case WaveFormSelection.Square:
            	//per sample loop
            	for (int i = 0; i < count; i++)
            	{
            		// The ramp works in the range -1~+1, to prevent phase inversion
            		// by negative wraps from FM signals
            		FPhase = sync ? -1 : FPhase + t2 + FMBuffer[i]*FMLevel.Value;
            		if(FPhase > 1.0f)
            			FPhase -= 2.0f;
            		//z0 = r1;
            		// In case FM present, a new inc is interpolated from phase history
            		//z1=inc;
            		//z2=z1;
            		//z3=z2;
            		//z4=z3;
            		//inc2 = interp(inc,z1,z2,z3,z4,mode="spline");
            		/* *************************************************************/
            		/* Main
					/* *************************************************************/
            		var r2 = FPhase *0.5f + 0.5f;       // ramp rescaled to 0-1 for EPTR calcs
            		//            	if (inc2<.125){      // if Fc<sr/16 (2756Hz @441000 sr)
            		var d1 = t2 * 2;   // width of phase transition region (4*fc/sr)
            		buffer[i] = eptr(r2, 2*t2, slope) * Gain.Value;
            		//            	} else {                // adding 3x oversampling at higher freqs
            		//            		t0 = delta(r2);
            		//            		if (t0>0){ t2 = r2 -t0 *.6666667;                     //z-2
            		//            			t1 = r2 -t0 *.3333333;             //z-1
            		//            		} else {   t2 =wrap(zt *.3333333 +zr, 0, 1);          //z-2
            		//            			t1 =wrap(zt *.6666667 +zr, 0, 1);          //z-1
            		//            		}
            		//            		zt = t0;               // ramp and delta history for interp
            		//            		zr = r2;
            		//            		d1  = inc2;            // shrink transition region
            		//            		t2 =eptr(t2, d1, w1);
            		//            		t1 =eptr(t1, d1, w1);
            		//            		t0 =eptr(r2, d1, w1);
            		//
            		//            		if      (t2==t1 &amp;&amp; t1==t0)                   out1 = t0;
            		//            		else if (t2!=-1 &amp;&amp; t1==-1 &amp;&amp; t0!=-1) out1 = -1;
            		//            		else if (t2!=1  &amp;&amp; t1==1  &amp;&amp; t0!=1)  out1 =  1;
            		//            		else out1 = (t2 + t1 + t0) * .33333333;
            		//            	}
            	}
            	break;
            }
        }

        private void OscBasic(float[] buffer, int count)
        {

            var t2 = 2*T;
            var slope = (float)VMath.Clamp(Slope.Value, 0.01, 0.99);
            switch (WaveForm.Value)
            {
                case WaveFormSelection.Sine:
                    for (int i = 0; i < count; i++)
                    {
                        buffer[i] = Gain.Value * (float)Math.Sin(FPhase*Math.PI);
                        
                        FPhase += t2 + FMBuffer[i]*FMLevel.Value;
                        
                        if (FPhase > 1)
                            FPhase -= 2;

                    }
                    break;
                case WaveFormSelection.Triangle:
                    for (int i = 0; i < count; i++)
                    {
                        var phase = FPhase*0.5f + 0.5f;
                        //buffer[i] =  Gain.Value * (phase < slope ? (2/slope) * phase - 1 : 1 - (2/(1-slope)) * (phase-slope));
                    	
                    	buffer[i] =  Gain.Value * AudioUtils.Triangle(phase, slope);
                        
                        FPhase += t2 + FMBuffer[i]*FMLevel.Value;

                        if (FPhase >= 1)
                            FPhase -= 2f;
                    }
                    break;
                case WaveFormSelection.Square:
                    for (int i = 0; i < count; i++)
                    {
                        buffer[i] = FPhase < 2*slope ? Gain.Value : -Gain.Value;
                        
                        FPhase += t2 + FMBuffer[i]*FMLevel.Value;

                        if (FPhase >= 2.0f)
                            FPhase -= 2.0f;
                    }
                    break;
                case WaveFormSelection.Sawtooth:

                    for (int i = 0; i < count; i++)
                    {
                        buffer[i] = Gain.Value * FPhase;
                        
                        FPhase += t2 + FMBuffer[i]*FMLevel.Value;
                        
                        if (FPhase > 1.0f)
                        {
                            FPhase -= 2.0f;
                        }
                    }
                    break;
            }
        }
        
        protected override void FillBuffer(float[] buffer, int offset, int count)
        {
        	//FM
        	if(FMLevel.Value > 0)
        	{
        		if(FMBuffer.Length < count)
        		{
        			FMBuffer = new float[count];
        		}
        		
        		//get FM wave
        		if(FMInput.Value != null)
        			FMInput.Value.Read(FMBuffer, offset, count);
        		else
        			FMBuffer.ReadSilence(offset, count);
        	}
        	
            if(UseEPTR.Value)
                OscEPTR(buffer, count);
            else
                OscBasic(buffer, count);
        }
	}
	
	#region PluginInfo
	[PluginInfo(Name = "OSCTester", Category = "VAudio", Help = "Testing WaveGeneration", Tags = "")]
	#endregion PluginInfo
	public class VAudioOSCTesterNode : AutoAudioSignalNode<TestOscSignal>
	{
	}
}

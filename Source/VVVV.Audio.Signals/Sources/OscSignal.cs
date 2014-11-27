#region usings
using System;
using System.Collections.Generic;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;
#endregion
namespace VVVV.Audio
{
    public enum WaveFormSelection
    {
        Sine,
        Triangle,
        Square,
        Sawtooth
    }
    
	public class OscSignal : AudioSignal
	{	    
	    public OscSignal()
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

        private void OscEPTR(float[] buffer, int count)
        {
            bool sync = false;
            var slope = Slope.Value;
            
            switch (WaveForm.Value)
            {
                case WaveFormSelection.Sine:
                    break;
                
                case WaveFormSelection.Sawtooth: 
                    slope = AudioUtils.Wrap(slope + 0.5f, 0, 1);
                    goto case WaveFormSelection.Triangle;
                    
                case WaveFormSelection.Triangle:
                    
                    //get FM wave
                    if(FMInput.Value != null)
                        FMInput.Value.Read(buffer, 0, count);
                    else
                        buffer.ReadSilence(0, count);
                    
                    //per sample loop
                    for (int i = 0; i < count; i++)
                    {
                        float sample;

                        if (slope >= 0.99f) // rising saw
                        { 
                            FPhase = sync ? -1 : FPhase + 2*T + buffer[i]*FMLevel.Value;
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
                            FPhase = sync ? -1 : FPhase + 2*T + buffer[i]*FMLevel.Value;                            
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
                            	FPhase = FPhase + 2*A*T;
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
                            	FPhase = FPhase + 2*B*T;
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
                    break;
            }
        }

        private void OscBasic(float[] buffer, int count)
        {
            //get FM wave into buffer
            if(FMInput.Value != null)
                FMInput.Value.Read(buffer, 0, count);
            else
                buffer.ReadSilence(0, count);
            
            var t2 = 2*T;
            var slope = (float)VMath.Clamp(Slope.Value, 0.01, 0.99);
            switch (WaveForm.Value)
            {
                case WaveFormSelection.Sine:
                    for (int i = 0; i < count; i++)
                    {
                        buffer[i] = Gain.Value * (float)Math.Sin(FPhase*Math.PI);
                        
                        FPhase += t2 + buffer[i]*FMLevel.Value;
                        
                        if (FPhase > 1)
                            FPhase -= 2;

                    }
                    break;
                case WaveFormSelection.Triangle:
                    for (int i = 0; i < count; i++)
                    {
                        buffer[i] =  Gain.Value * (FPhase < slope ? (2/slope) * FPhase - 1 : 1 - (2/(1-slope)) * (FPhase-slope));
                        
                        FPhase += t2 + buffer[i]*FMLevel.Value;

                        if (FPhase >= 1)
                            FPhase -= 2f;
                    }
                    break;
                case WaveFormSelection.Square:
                    for (int i = 0; i < count; i++)
                    {
                        buffer[i] = FPhase < 2*slope ? Gain.Value : -Gain.Value;
                        
                        FPhase += t2 + buffer[i]*FMLevel.Value;

                        if (FPhase >= 2.0f)
                            FPhase -= 2.0f;
                    }
                    break;
                case WaveFormSelection.Sawtooth:

                    for (int i = 0; i < count; i++)
                    {
                        buffer[i] = Gain.Value * FPhase;
                        
                        FPhase += t2 + buffer[i]*FMLevel.Value;
                        
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
            //OscBasic(buffer, count);
            
            if(UseEPTR.Value)
                OscEPTR(buffer, count);
            else
                OscBasic(buffer, count);
            
//			PerfCounter.Start("Sine");
            
//			PerfCounter.Stop("Sine");
        }
	}
	
	public class MultiSineSignal : AudioSignal
	{
	    public MultiSineSignal(ISpread<float> frequency, ISpread<float> gain)
	    {
	        Frequencies = frequency;
	        Gains = gain;
	        Phases = new Spread<float>();
	    }
	    
	    public ISpread<float> Frequencies;
	    public ISpread<float> Gains;
		private readonly float TwoPi = (float)(Math.PI * 2);
		private ISpread<float> Phases;
		
		protected override void FillBuffer(float[] buffer, int offset, int count)
		{
//			PerfCounter.Start("MultiSine");
			var spreadMax = Frequencies.CombineWith(Gains);
			Phases.Resize(spreadMax, () => default(float), f => f = 0);
			for (int slice = 0; slice < spreadMax; slice++) 
			{
			 	var increment = TwoPi*Frequencies[slice]/SampleRate;
			 	var gain = Gains[slice];
			 	var phase = Phases[slice];
			 	
			 	if(slice == 0)
			 	{
			 		for (int i = 0; i < count; i++)
			 		{
			 			// Sinus Generator
			 			buffer[i] = gain*(float)Math.Sin(phase);
			 			
			 			phase += increment;
			 			if(phase > TwoPi)
			 				phase -= TwoPi;
			 			else if(phase < 0)
			 				phase += TwoPi;
			 		}
			 	}
			 	else
			 	{
			 		for (int i = 0; i < count; i++)
			 		{
			 			// Sinus Generator
			 			buffer[i] += gain*(float)Math.Sin(phase);
			 			
			 			phase += increment;
			 			if(phase > TwoPi)
			 				phase -= TwoPi;
			 			else if(phase < 0)
			 				phase += TwoPi;
			 		}
			 	}
			 		
				
				Phases[slice] = phase; //write back
			}
			
//			PerfCounter.Stop("MultiSine");
		}
			
	}
}





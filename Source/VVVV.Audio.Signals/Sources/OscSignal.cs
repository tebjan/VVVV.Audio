#region usings
using System;
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
    
	public class OscSignal : AudioSignalInput
	{
		public OscSignal(float frequency, float gain)
		{
			Frequency = frequency;
			Gain = gain;
		}

		public float Frequency;

		public float Gain = 0.1f;
		
		public float Slope = 0.5f;
		
		public float FMLevel;

		public bool PTR;
		
		public WaveFormSelection WaveForm;

		private const float TwoPi = (float)(Math.PI * 2);

		private float FPhase = 0;

		protected override void FillBuffer(float[] buffer, int offset, int count)
		{
		    //OscBasic(buffer, count);
		    
		    if(PTR)
		        OscEPTR(buffer, count);
		    else
		        OscBasic(buffer, count);
		            
//			PerfCounter.Start("Sine");
		            
//			PerfCounter.Stop("Sine");
		}

        private float triangle(float phase, float slope = 0.5f)
        {
        	return phase < slope ? (2/slope) * phase - 1 : 1 - (2/(1-slope)) * (phase-slope);
        }

        private float Wrap(float x, float min = -1.0f, float max = 1.0f)
        {
            var range = max - min;

            if (x > max)
                return x - range;

            if (x < min)
                return x + range;

            return x;
        }

        float FLastPhase;
        float FLastPhase01;
        float FLastFMInc;
        float h4;
        float out2;
        float FWaveSelector, fmInc, in5;

        private void OscEPTR(float[] buffer, int count)
        {
            bool sync = false;

            float dx1 = 0;
            float w1 = 0;
            float w2 = 0;
            float lx = 0;
            float w0 = 0;
            float w3 = 0;
            var slope = Slope;

            switch (WaveForm)
            {
                case WaveFormSelection.Sine:
                    break;
                case WaveFormSelection.Sawtooth:
                    slope = Wrap(slope + 0.5f, 0, 1);
                    goto case WaveFormSelection.Triangle;
                case WaveFormSelection.Triangle:
                    var stepInc = 2.0f * (Frequency / SampleRate);
                    
                    if(FInput != null)
                        FInput.Read(buffer, 0, count);
                    else
                        buffer.ReadSilence(0, count);
                    
                    for (int i = 0; i < count; i++)
                    {
                        float out1;
                        float phase = sync ? 0 : Wrap(stepInc + buffer[i]*FMLevel + FLastPhase); // ramp with FM and sync
                        float phase01 = phase * 0.5f + 0.5f; // utility ramp in 0~1 range
                       
                        var dx = phase01 - FLastPhase01;
                        if (slope > .99f)
                        {  			// rising saw
                            out1 = dx < 0 ? 0 : phase;
                        }
                        else if (slope < .01)
                        { 		// falling saw
                            out1 = dx < 0 ? 0 : -phase;
                        }
                        else
                        { 				    // variable slope
                            if (dx < 0) out1 = -1;    // ELTR on fall-to-rise
                            else if (phase01 > slope && FLastPhase01 < slope) out1 = 1; // LTR on rise-to-fall
                            else out1 = triangle(phase01, slope);
                        }

                        FLastPhase = phase; 		// FM phase accumulator
                        FLastPhase01 = phase01;		// previous ramp value for ELTRs
                        
                        buffer[i] = out1*Gain;
                    }
                    break;
                case WaveFormSelection.Square:
                    break;
            }

            FLastFMInc = fmInc;					// previous FM value for delta correction
            // out1 = dcblock(out1); 	// for later use

        }

        private void OscBasic(float[] buffer, int count)
        {
            var increment = Frequency / SampleRate;
            switch (WaveForm)
            {
                case WaveFormSelection.Sine:
                    var incrementSin = TwoPi * increment;
                    for (int i = 0; i < count; i++)
                    {
                        // Sinus Generator
                        buffer[i] = Gain * (float)Math.Sin(FPhase);
                        FPhase += incrementSin;
                        if (FPhase > TwoPi)
                            FPhase -= TwoPi;
                        else
                            if (FPhase < 0)
                                FPhase += TwoPi;
                    }
                    break;
                case WaveFormSelection.Triangle:
                    for (int i = 0; i < count; i++)
                    {
                        FPhase = FPhase + 2.0f * increment;
                        buffer[i] = FPhase < 1.0f ? -Gain + (2 * Gain) * FPhase : 3 * Gain - (2 * Gain) * FPhase;

                        if (FPhase >= 2.0f)
                            FPhase -= 2.0f;
                    }
                    break;
                case WaveFormSelection.Square:
                    for (int i = 0; i < count; i++)
                    {
                        FPhase = FPhase + 2.0f * increment;
                        buffer[i] = FPhase < 1.0f ? Gain : -Gain;

                        if (FPhase >= 2.0f)
                            FPhase -= 2.0f;
                    }
                    break;
                case WaveFormSelection.Sawtooth:

                    for (int i = 0; i < count; i++)
                    {
                        FPhase = FPhase + 2.0f * increment;
                        if (FPhase > 1.0f - increment)
                        {
                            buffer[i] = Gain * (FPhase - (FPhase / increment) + (1.0f / increment) - 1.0f);
                            FPhase -= 2.0f;
                        }
                        else
                        {
                            buffer[i] = Gain * FPhase;
                        }
                    }
                    break;
                default:
                    break;
            }
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





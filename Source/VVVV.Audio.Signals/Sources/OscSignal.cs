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
    
    public enum WaveGenerationMethod
    {
        Naive,
        PolyBLEP,
        EPTR
    }
    
    public class OscSignal : AudioSignal
    {
        public SigParamDiff<float> Frequency = new SigParamDiff<float>("Frequency", 440);
        public SigParam<WaveFormSelection> WaveForm = new SigParam<WaveFormSelection>("Wave Form");
        public SigParamDiff<double> Slope = new SigParamDiff<double>("Symmetry", 0.5f);
        public SigParam<WaveGenerationMethod> AntiAliasingMethod = new SigParam<WaveGenerationMethod>("Anti-Aliasing Method", WaveGenerationMethod.PolyBLEP);
        public SigParamAudio FMInput = new SigParamAudio("FM");
        public SigParam<float> FMLevel = new SigParam<float>("FM Level");
        public SigParam<float> Gain = new SigParam<float>("Gain");
        
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

        protected override void FillBuffer(float[] buffer, int offset, int count)
        {
            //FM
            if(FMBuffer.Length < count)
            {
                FMBuffer = new float[count];
            }
            
            if(FMLevel.Value > 0)
            {
                //get FM wave
                FMInput.Read(FMBuffer, offset, count);
            }
            
            switch (AntiAliasingMethod.Value)
            {
                case WaveGenerationMethod.Naive:
                    OscBasic(buffer, count);
                    break;
                case WaveGenerationMethod.PolyBLEP:
                    OscPolyBLEP(buffer, count);
                    break;
                case WaveGenerationMethod.EPTR:
                    OscEPTR(buffer, count);
                    break;
            }

        }

        const double TwoPi = (Math.PI * 2);
        const double HalfPi = (Math.PI * 0.5);
        double FPhase = -1;
        double T;
        
        // time step and triangle params
        void CalcFrequencyConsts(float freq)
        {
            T = freq / SampleRate;
            CalcTriangleCoefficients(Slope.Value);
        }

        //triangle precalculated values
        bool FTriangleUp = true;
        double A, B, AoverB, BoverA, a2, a1, a0, b2, b1, b0;

        void CalcTriangleCoefficients(double slope)
        {
            //triangle magnitudes
            var slopeClamp = VMath.Clamp(slope, 0.01, 0.99);
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
        
        // from http://www.yofiel.com/software/cycling-74-patches/antialiased-oscillators
        double EPTR(double ramp, double regionSize, double slope)
        {
            slope *= 0.5f;
            
            //d2 = 8192  / d1;            // buffer transition coefficient;
            var d2 = TwoPi *.213332 / regionSize; // transcendental coefficient;
            var slopeMin = 1-slope;                  // inverted duty cycle
            var e0= 0.0; // pi5  = pi *.5;
            
            if (ramp <=  slope - regionSize)        // fixed low region at start
                return -1;
            else if (ramp < slope + regionSize)
            {   // rising region
                //e0 = peek(buf, d2*(ramp -w1 +regionSize), 0);
                e0 = Math.Tanh(4 * Math.Sin(d2 * (ramp -slope +regionSize) - HalfPi));
                return e0;
            }
            else if (ramp <= slopeMin - regionSize)    // middle fixed hi region
                return 1;
            else if (ramp < slopeMin + regionSize)
            {     // falling region
                //e0 = peek(buf,d2*(ramp -slopeMin +regionSize), 0);
                e0 = Math.Tanh(4 * Math.Sin(d2*(ramp -slopeMin +regionSize) + HalfPi));
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
                        buffer[i] = (float)(Gain.Value * Math.Sin(FPhase*Math.PI));
                        
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
                        double sample;

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
                                FPhase = sync ? -1 : FPhase + t2*A + FMBuffer[i]*FMLevel.Value;
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
                                FPhase = sync ? -1 : FPhase + t2*B + FMBuffer[i]*FMLevel.Value;
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
                        
                        buffer[i] = (float)(sample*Gain.Value);
                    }
                    break;
                case WaveFormSelection.Square:
                    
                    //per sample loop
                    for (int i = 0; i < count; i++)
                    {
                        //from http://www.yofiel.com/software/cycling-74-patches/antialiased-oscillators
                        // The ramp works in the range -1~+1, to prevent phase inversion
                        // by negative wraps from FM signals
                        FPhase = sync ? -1 : FPhase + t2 + FMBuffer[i]*FMLevel.Value;
                        if(FPhase > 1.0)
                            FPhase -= 2.0;
                        
                        var r2 = FPhase *0.5 + 0.5;       // ramp rescaled to 0-1 for EPTR calcs
                        //            	if (inc2<.125){      // if Fc<sr/16 (2756Hz @441000 sr)
                        var d1 = t2 * 2;   // width of phase transition region (4*fc/sr)
                        buffer[i] = (float)(EPTR(r2, 2*t2, slope) * Gain.Value);
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
        
        double PolyBLEPSquare(double t, double dt)
        {
            // 0 <= t < 1
            if (t < dt)
            {
                t = t/dt - 1;
                return -t*t;
            }
            
            // -1 < t < 0
            else if (t > 1 - dt)
            {
                t = (t - 1)/dt + 1;
                return t*t;
            }
            
            // 0 otherwise
            else
            {
                return 0;
            }
        }
        
        double PolyBLEPSaw(double t, double dt)
        {
            // 0 <= t < 1
            if (t < dt)
            {
                t /= dt;
                // 2 * (t - t^2/2 - 0.5)
                return t+t - t*t - 1;
            }

            // -1 < t < 0
            else if (t > 1 - dt)
            {
                t = (t - 1) / dt;
                // 2 * (t^2/2 + t + 0.5)
                return t*t + t+t + 1;
            }

            // 0 otherwise
            else
            {
                return 0;
            }
        }

        private void OscPolyBLEP(float[] buffer, int count)
        {

            var t2 = 2*T;
            var slope = VMath.Clamp(Slope.Value, 0.01, 0.99);
            switch (WaveForm.Value)
            {
                case WaveFormSelection.Sine:
                    for (int i = 0; i < count; i++)
                    {
                        buffer[i] = (float)(Gain.Value * Math.Sin(FPhase*Math.PI));
                        
                        FPhase += t2 + FMBuffer[i]*FMLevel.Value;
                        
                        if (FPhase > 1)
                            FPhase -= 2;

                    }
                    break;
                case WaveFormSelection.Triangle:
                    for (int i = 0; i < count; i++)
                    {
                        var phase = FPhase*0.5f + 0.5f;
                        buffer[i] =  (float)(Gain.Value * AudioUtils.Triangle(phase, slope));
                        
                        FPhase += t2 + FMBuffer[i]*FMLevel.Value;

                        if (FPhase >= 1)
                            FPhase -= 2f;
                    }
                    break;
                case WaveFormSelection.Square:
                    for (int i = 0; i < count; i++)
                    {
                        FPhase += T;
                        FPhase -= Math.Floor(FPhase);
                        var naiveSaw = FPhase * 2 - 1;
                        buffer[i] = (float)((naiveSaw - PolyBLEPSquare(FPhase, T)) * Gain.Value);
                    }
                    break;
                case WaveFormSelection.Sawtooth:
                    for (int i = 0; i < count; i++)
                    {
                        FPhase += T;
                        FPhase -= Math.Floor(FPhase);
                        var naiveSaw = FPhase * 2 - 1;
                        buffer[i] = (float)((naiveSaw - PolyBLEPSaw(FPhase, T)) * Gain.Value);
                    }
                    break;
            }
        }
        
        private void OscBasic(float[] buffer, int count)
        {

            var t2 = 2*T;
            var slope = VMath.Clamp(Slope.Value, 0.01, 0.99);
            switch (WaveForm.Value)
            {
                case WaveFormSelection.Sine:
                    for (int i = 0; i < count; i++)
                    {
                        buffer[i] = (float)(Gain.Value * Math.Sin(FPhase*Math.PI));
                        
                        FPhase += t2 + FMBuffer[i]*FMLevel.Value;
                        
                        if (FPhase > 1)
                            FPhase -= 2;

                    }
                    break;
                case WaveFormSelection.Triangle:
                    for (int i = 0; i < count; i++)
                    {
                        var phase = FPhase*0.5f + 0.5f;

                        buffer[i] =  (float)(Gain.Value * AudioUtils.Triangle(phase, slope));
                        
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
                        buffer[i] = (float)(Gain.Value * FPhase);
                        
                        FPhase += t2 + FMBuffer[i]*FMLevel.Value;
                        
                        if (FPhase > 1.0f)
                        {
                            FPhase -= 2.0f;
                        }

                    }
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





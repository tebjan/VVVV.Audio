#region usings
using System;
using System.Collections.Generic;
using VVVV.PluginInterfaces.V2;
using VVVV.Audio.Utils;
#endregion
namespace VVVV.Audio
{
    public enum WaveFormSelection
    {
        Sine,
        Triangle,
        Square,
        Sawtooth,
        WhiteNoise,
        PinkNoise
    }
    
    public enum AntiAliasingAlgorithm
    {
        None,
        PolyBLEP,
        EPTR
    }
    
    public class OscSignal : AudioSignal
    {
        SigParamAudio Frequency = new SigParamAudio("Frequency");
        SigParamDiff<float> FrequencyOffset = new SigParamDiff<float>("Frequency Offset", 440);
        SigParam<WaveFormSelection> WaveForm = new SigParam<WaveFormSelection>("Waveform");
        SigParamDiff<double> Slope = new SigParamDiff<double>("Symmetry", 0.5f);
        SigParam<AntiAliasingAlgorithm> AntiAliasingMethod = new SigParam<AntiAliasingAlgorithm>("Anti-Aliasing Method", AntiAliasingAlgorithm.PolyBLEP);
        SigParamAudio FMInput = new SigParamAudio("FM");
        SigParam<float> FMLevel = new SigParam<float>("FM Level");
        SigParam<float> Gain = new SigParam<float>("Gain", 0.1f);
        
        public OscSignal()
        {
            //get param change events
            FrequencyOffset.ValueChanged = CalcFrequencyConsts;
            Slope.ValueChanged = CalcTriangleCoefficients;
        }
        
        protected override void Engine_SampleRateChanged(object sender, EventArgs e)
        {
            base.Engine_SampleRateChanged(sender, e);
            CalcFrequencyConsts(FrequencyOffset.Value);
        }
        
        const double FWhiteNoiseScale = 2.0f / 0xffffffff;
        uint FWhiteNoiseX1 = 0x67452301;
        uint FWhiteNoiseX2 = 0xefcdab89;
        
        void WhiteNoise(float[] buffer, int count, double gain)
        {
            
            gain *= FWhiteNoiseScale;
            
            for (int i = 0; i < count; i++)
            {
                FWhiteNoiseX1 ^= FWhiteNoiseX2;
                
                buffer[i] = (float)(FWhiteNoiseX2 * gain);
                
                unchecked
                {
                    FWhiteNoiseX2 += FWhiteNoiseX1;
                }
            }
            
        }
        
        
        double b0p, b1p, b2p;
        void PinkNoise(float[] buffer, int count, double gain)
        {
            
            gain *= FWhiteNoiseScale;
            
            for (int i = 0; i < count; i++)
            {
                FWhiteNoiseX1 ^= FWhiteNoiseX2;
                
                var white = (FWhiteNoiseX2 * gain);
                
                unchecked
                {
                    FWhiteNoiseX2 += FWhiteNoiseX1;
                }
                
                b0p = 0.99765 * b0p + white * 0.0990460;
                b1p = 0.96300 * b1p + white * 0.2965164;
                b2p = 0.57000 * b2p + white * 1.0526913;
                buffer[i] = (float)((b0p + b1p + b2p + white * 0.1848) * 0.0333333333333333333);

            }
            
        }


        float[] FreqBuffer = new float[1];
        protected override void FillBuffer(float[] buffer, int offset, int count)
        {
            //FM
            if(FMBuffer.Length < buffer.Length)
            {
                FMBuffer = new float[count];
                FreqBuffer = new float[count];
            }
            
            //read frequencies
            Frequency.Read(FreqBuffer, offset, count);
            
            if(FMLevel.Value > 0)
            {
                //get FM wave
                FMInput.Read(FMBuffer, offset, count);
            }
            
            if(WaveForm.Value == WaveFormSelection.WhiteNoise)
            {
                WhiteNoise(buffer, count, Gain.Value*0.5f);
            }
            else if(WaveForm.Value == WaveFormSelection.PinkNoise)
            {
                PinkNoise(buffer, count, Gain.Value*0.5f);
            }
            else
            {
                switch (AntiAliasingMethod.Value)
                {
                    case AntiAliasingAlgorithm.None:
                        OscBasic(buffer, count);
                        break;
                    case AntiAliasingAlgorithm.PolyBLEP:
                        OscPolyBLEP(buffer, count);
                        break;
                    case AntiAliasingAlgorithm.EPTR:
                        OscEPTR(buffer, count);
                        break;
                }
            }

        }

        const double TwoPi = (Math.PI * 2);
        const double HalfPi = (Math.PI * 0.5);
        double FPolyBLEPPhase = 0;
        double FEPTRPhase = 0;
        double FBasicPhase = 0;
        double T;
        
        // time step and triangle params
        void CalcFrequencyConsts(float freq)
        {
            T = freq / SampleRate;
        }

        //triangle precalculated values
        bool FTriangleUp = true;
        double A, B, AoverB, BoverA, a2, a1, a0, b2, b1, b0;

        void CalcTriangleCoefficients(double slope)
        {
            //triangle magnitudes
            var slopeClamp = MathUtils.Clamp(slope, 0.01, 0.99);
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
                        CalcFrequencyConsts(FreqBuffer[i] + FrequencyOffset.Value);
                        buffer[i] = (float)(Gain.Value * Math.Sin(FEPTRPhase*Math.PI));
                        
                        FEPTRPhase += t2 + FMBuffer[i]*FMLevel.Value;
                        
                        if (FEPTRPhase > 1)
                            FEPTRPhase -= 2;

                    }
                    break;
                    
                case WaveFormSelection.Sawtooth:
                    slope = AudioUtils.Wrap(slope + 0.5f, 0, 1);
                    goto case WaveFormSelection.Triangle;
                    
                case WaveFormSelection.Triangle:

                    //per sample loop
                    for (int i = 0; i < count; i++)
                    {
                        CalcFrequencyConsts(FreqBuffer[i] + FrequencyOffset.Value);
                        CalcTriangleCoefficients(Slope.Value);
                        double sample;

                        if (slope >= 0.99f) // rising saw
                        {
                            FEPTRPhase = sync ? -1 : FEPTRPhase + t2 + FMBuffer[i]*FMLevel.Value;
                            if (FEPTRPhase > 1.0f - T) //transition
                            {
                                sample = FEPTRPhase - (FEPTRPhase / T) + (1.0f / T) - 1.0f;
                                FEPTRPhase -= 2.0f;
                            }
                            else
                            {
                                sample = FEPTRPhase;
                            }
                        }
                        else if (slope <= 0.01f) // falling saw
                        {
                            FEPTRPhase = sync ? -1 : FEPTRPhase + t2 + FMBuffer[i]*FMLevel.Value;
                            if (FEPTRPhase > 1.0f - T) //transition
                            {
                                sample = -FEPTRPhase + (FEPTRPhase / T) - (1.0f / T) + 1.0f;
                                FEPTRPhase -= 2.0f;
                            }
                            else
                            {
                                sample = -FEPTRPhase;
                            }
                        }
                        else //triangle
                        {
                            if(FTriangleUp) //counting up
                            {
                                FEPTRPhase = sync ? -1 : FEPTRPhase + t2*A + FMBuffer[i]*FMLevel.Value;
                                if (FEPTRPhase > 1 - A*T)
                                {
                                    //transitionregion
                                    sample = a2 * (FEPTRPhase * FEPTRPhase) + a1 * FEPTRPhase + a0;
                                    FEPTRPhase = 1 + (FEPTRPhase - 1) * BoverA;
                                    FTriangleUp = false;
                                }
                                else //linearregion
                                {
                                    sample = FEPTRPhase;
                                }
                            }
                            else //counting down
                            {
                                FEPTRPhase = sync ? -1 : FEPTRPhase + t2*B + FMBuffer[i]*FMLevel.Value;
                                if (FEPTRPhase < -1 - B*T)
                                {
                                    //transitionregion
                                    sample = b2 * (FEPTRPhase * FEPTRPhase) + b1 * FEPTRPhase + b0;
                                    FEPTRPhase = -1 + (FEPTRPhase + 1) * AoverB;
                                    FTriangleUp = true;
                                }
                                else //linearregion
                                {
                                    sample = FEPTRPhase;
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
                        CalcFrequencyConsts(FreqBuffer[i] + FrequencyOffset.Value);
                        //from http://www.yofiel.com/software/cycling-74-patches/antialiased-oscillators
                        // The ramp works in the range -1~+1, to prevent phase inversion
                        // by negative wraps from FM signals
                        FEPTRPhase = sync ? -1 : FEPTRPhase + t2 + FMBuffer[i]*FMLevel.Value;
                        if(FEPTRPhase > 1.0)
                            FEPTRPhase -= 2.0;
                        
                        var r2 = FEPTRPhase *0.5 + 0.5;       // ramp rescaled to 0-1 for EPTR calcs
                        //                if (inc2<.125){      // if Fc<sr/16 (2756Hz @441000 sr)
                        var d1 = t2 * 2;   // width of phase transition region (4*fc/sr)
                        buffer[i] = (float)(EPTR(r2, 2*t2, slope) * Gain.Value);
                        //                } else {                // adding 3x oversampling at higher freqs
                        //                    t0 = delta(r2);
                        //                    if (t0>0){ t2 = r2 -t0 *.6666667;                     //z-2
                        //                        t1 = r2 -t0 *.3333333;             //z-1
                        //                    } else {   t2 =wrap(zt *.3333333 +zr, 0, 1);          //z-2
                        //                        t1 =wrap(zt *.6666667 +zr, 0, 1);          //z-1
                        //                    }
                        //                    zt = t0;               // ramp and delta history for interp
                        //                    zr = r2;
                        //                    d1  = inc2;            // shrink transition region
                        //                    t2 =eptr(t2, d1, w1);
                        //                    t1 =eptr(t1, d1, w1);
                        //                    t0 =eptr(r2, d1, w1);
                        //
                        //                    if      (t2==t1 &amp;&amp; t1==t0)                   out1 = t0;
                        //                    else if (t2!=-1 &amp;&amp; t1==-1 &amp;&amp; t0!=-1) out1 = -1;
                        //                    else if (t2!=1  &amp;&amp; t1==1  &amp;&amp; t0!=1)  out1 =  1;
                        //                    else out1 = (t2 + t1 + t0) * .33333333;
                        //                }
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
        
        double PolyBLAMP(double t, double dt)
        {
            if (t < dt)
            {
                t = t/dt - 1;
                return (double)-1/3 * t*t*t;
            }
            else if (t > 1 - dt)
            {
                t = (t - 1)/dt + 1;
                return (double)1/3 * t*t*t;
            }
            else
            {
                return 0;
            }
        }

        private void OscPolyBLEP(float[] buffer, int count)
        {

            var t2 = 2*T;
            var slope = MathUtils.Clamp(Slope.Value, 0.01, 0.99);
            switch (WaveForm.Value)
            {
                case WaveFormSelection.Sine:
                    for (int i = 0; i < count; i++)
                    {
                        CalcFrequencyConsts(FreqBuffer[i] + FrequencyOffset.Value);
                        buffer[i] = (float)(Gain.Value * Math.Sin(FPolyBLEPPhase*Math.PI));
                        
                        FPolyBLEPPhase += t2 + FMBuffer[i]*FMLevel.Value;
                        
                        if (FPolyBLEPPhase > 1)
                            FPolyBLEPPhase -= 2;

                    }
                    break;
                case WaveFormSelection.Triangle:
                    for (int i = 0; i < count; i++)
                    {
                        CalcFrequencyConsts(FreqBuffer[i] + FrequencyOffset.Value);
                        var phase = FPolyBLEPPhase*0.5f + 0.5f;
                        //var sample = AudioUtils.Triangle(phase, slope);
                        
                        // Start with naive triangle.
                        double sample = 4 * phase;
                        if (sample >= 3)
                        {
                            sample = sample - 4;
                        }
                        else if (sample > 1)
                        {
                            sample = 2 - sample;
                        }
                        
                        // Correct falling discontinuity.
                        double scale = 4 * T;
                        double phase2 = phase + 0.25;
                        phase2 = phase2 - Math.Floor(phase2);
                        sample = sample + scale * PolyBLAMP(phase2, T);

                        // Correct rising discontinuity.
                        phase2 = phase2 + 0.5;
                        phase2 = phase2 - Math.Floor(phase2);
                        sample = sample - scale * PolyBLAMP(phase2, T);
                        
                        FPolyBLEPPhase += t2 + FMBuffer[i]*FMLevel.Value;

                        if (FPolyBLEPPhase >= 1)
                            FPolyBLEPPhase -= 2f;
                        
                        buffer[i] = (float)(Gain.Value * sample);
                    }
                    break;
                case WaveFormSelection.Square:
                    for (int i = 0; i < count; i++)
                    {
                       CalcFrequencyConsts(FreqBuffer[i] + FrequencyOffset.Value);
                       // Start with naive PW square.
                       double sample;
                       
                       sample = FPolyBLEPPhase < slope ? 1 : -1;
                    
                       // Correct rising discontinuity.
                       sample = sample + PolyBLEPSquare(FPolyBLEPPhase, T);
                    
                       // Correct falling discontinuity.
                       double phase2 = FPolyBLEPPhase + 1 - slope;
                       phase2 = phase2 - Math.Floor(phase2);
                       sample = sample - PolyBLEPSquare(phase2, T);
                    
                       // Increment phase for next sample.
                       FPolyBLEPPhase += T;
                       FPolyBLEPPhase -= Math.Floor(FPolyBLEPPhase);
                    
                       // Output current sample.
                       buffer[i] = (float)(sample * Gain.Value);
                        
                    }
                    break;
                case WaveFormSelection.Sawtooth:
                    for (int i = 0; i < count; i++)
                    {
                        CalcFrequencyConsts(FreqBuffer[i] + FrequencyOffset.Value);
                       
                        var naiveSaw = FPolyBLEPPhase * 2 - 1;
                        buffer[i] = (float)((naiveSaw - PolyBLEPSaw(FPolyBLEPPhase, T)) * Gain.Value);
                        
                        FPolyBLEPPhase += T;
                        FPolyBLEPPhase -= Math.Floor(FPolyBLEPPhase);
                    }
                    break;
            }
        }
        
        private void OscBasic(float[] buffer, int count)
        {

            var t2 = 2*T;
            var slope = MathUtils.Clamp(Slope.Value, 0.01, 0.99);
            switch (WaveForm.Value)
            {
                case WaveFormSelection.Sine:
                    for (int i = 0; i < count; i++)
                    {
                        CalcFrequencyConsts(FreqBuffer[i] + FrequencyOffset.Value);
                        buffer[i] = (float)(Gain.Value * Math.Sin(FBasicPhase*Math.PI));
                        
                        FBasicPhase += t2 + FMBuffer[i]*FMLevel.Value;
                        
                        if (FBasicPhase > 1)
                            FBasicPhase -= 2;

                    }
                    break;
                case WaveFormSelection.Triangle:
                    for (int i = 0; i < count; i++)
                    {
                        CalcFrequencyConsts(FreqBuffer[i] + FrequencyOffset.Value);
                        var phase = FBasicPhase*0.5f + 0.5f;

                        buffer[i] =  (float)(Gain.Value * AudioUtils.Triangle(phase, slope));
                        
                        FBasicPhase += t2 + FMBuffer[i]*FMLevel.Value;

                        if (FBasicPhase >= 1)
                            FBasicPhase -= 2f;
                    }
                    break;
                case WaveFormSelection.Square:
                    for (int i = 0; i < count; i++)
                    {
                        CalcFrequencyConsts(FreqBuffer[i] + FrequencyOffset.Value);
                        buffer[i] = FBasicPhase < 2*slope ? Gain.Value : -Gain.Value;
                        
                        FBasicPhase += t2 + FMBuffer[i]*FMLevel.Value;

                        if (FBasicPhase >= 2.0f)
                            FBasicPhase -= 2.0f;
                    }
                   
                    break;
                case WaveFormSelection.Sawtooth:
                    for (int i = 0; i < count; i++)
                    {
                        CalcFrequencyConsts(FreqBuffer[i] + FrequencyOffset.Value);
                        buffer[i] = (float)(Gain.Value * FBasicPhase);
                        
                        FBasicPhase += t2 + FMBuffer[i]*FMLevel.Value;
                        
                        if (FBasicPhase > 1.0f)
                        {
                            FBasicPhase -= 2.0f;
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
//            PerfCounter.Start("MultiSine");
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
            
//            PerfCounter.Stop("MultiSine");
        }
        
    }
}





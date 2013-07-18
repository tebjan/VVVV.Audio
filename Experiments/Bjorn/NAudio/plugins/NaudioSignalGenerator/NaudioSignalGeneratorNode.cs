#region usings
using System;
using System.ComponentModel.Composition;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using NAudio.Wave;
using NAudio.Wave.SampleProviders;

using VVVV.Core.Logging;
#endregion usings

namespace VVVV.Nodes
{
	#region PluginInfo
	[PluginInfo(Name = "SignalGenerator", Category = "Naudio", Help = "Basic template with one value in/out", Tags = "")]
	#endregion PluginInfo
	public class NaudioSignalGeneratorNode : IPluginEvaluate, IDisposable, ISampleProvider
	{
		#region fields & pins
		[Input("Frequency", DefaultValue = 220.0)]
		IDiffSpread<double> FFrequency;
		
		[Input("End Frequency", DefaultValue = 440.0)]
		IDiffSpread<double> FEndFrequency;
		
		[Input("Sweep Length", DefaultValue = 10)]
		IDiffSpread<double> FSweepLength;			

		[Input("Type", DefaultEnumEntry = "Sin")]
		IDiffSpread<SignalGeneratorType> FType;	
		
		[Input("Gain", DefaultValue = 0.5)]
		IDiffSpread<double> FGain;		

		[Output("Output")]
		ISpread<ISampleProvider> FOutput;
		
		SignalGenerator FSigGen;

		[Import()]
		ILogger FLogger;
		#endregion fields & pins

		public NaudioSignalGeneratorNode()
		{
			FSigGen = new SignalGenerator();
			FSigGen.Type = SignalGeneratorType.Square;
		}
		
		public void Dispose()
		{
			
		}
		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			if(FOutput[0] != this)
			{
				FSigGen.Type = FType[0];
				FSigGen.FrequencyEnd = FEndFrequency[0];
				FSigGen.SweepLengthSecs = FSweepLength[0];
				FOutput[0] = this;
			}
			
			if(FFrequency.IsChanged)
			{
				FSigGen.Frequency = FFrequency[0];
			}
			if(FEndFrequency.IsChanged)
			{
				FSigGen.FrequencyEnd = FEndFrequency[0];
			}
			if(FSweepLength.IsChanged)
			{
				FSigGen.SweepLengthSecs = FSweepLength[0];
			}			
			if(FType.IsChanged)
			{
				FSigGen.Type = FType[0];
			}
			/*if(FGain.IsChanged)
			{
				FSigGen.Gain = FGain[0];
			}*/
			if(FSigGen.bufferCount < (FSigGen.BufferedGain.Length - 1))
			{
				FSigGen.BufferedGain[FSigGen.bufferCount] = FGain[0];
				FSigGen.bufferCount += 1;
			}
		}
		
		public WaveFormat WaveFormat
		{
			get{ return WaveFormat.CreateIeeeFloatWaveFormat(44100, 2); }
		}
		
		public int Read(float[] buffer, int offset, int count)
		{
			return FSigGen.Read(buffer, offset, count);
		}
	}
}

//Taken from NAudio > 1.6 src
namespace NAudio.Wave.SampleProviders
{
    /// <summary>
    /// Signal Generator
    /// Sin, Square, Triangle, SawTooth, White Noise, Pink Noise, Sweep.
    /// </summary>
    /// <remarks>
    /// Posibility to change ISampleProvider
    /// Example :
    /// ---------
    /// WaveOut _waveOutGene = new WaveOut();
    /// WaveGenerator wg = new SignalGenerator();
    /// wg.Type = ...
    /// wg.Frequency = ...
    /// wg ...
    /// _waveOutGene.Init(wg);
    /// _waveOutGene.Play();
    /// </remarks>
    public class SignalGenerator : ISampleProvider
    {
        // Wave format
        private readonly WaveFormat waveFormat;

        // Random Number for the White Noise & Pink Noise Generator
        private readonly Random random = new Random();

        private readonly double[] pinkNoiseBuffer = new double[7];

        // Const Math
        private const double TwoPi = 2*Math.PI;

        // Generator variable
        private int nSample;

        // Sweep Generator variable
        private double phi;
    	
    	public int bufferCount;
    	public double[] BufferedGain;

        /// <summary>
        /// Initializes a new instance for the Generator (Default :: 44.1Khz, 2 channels, Sinus, Frequency = 440, Gain = 1)
        /// </summary>
        public SignalGenerator()
            : this(44100, 2)
        {

        }

        /// <summary>
        /// Initializes a new instance for the Generator (UserDef SampleRate &amp; Channels)
        /// </summary>
        /// <param name="sampleRate">Desired sample rate</param>
        /// <param name="channel">Number of channels</param>
        public SignalGenerator(int sampleRate, int channel)
        {
            phi = 0;
            waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channel);

            // Default
            Type = SignalGeneratorType.Sin;
            Frequency = 440.0;
            Gain = 1;
            PhaseReverse = new bool[channel];
            SweepLengthSecs = 2;
        	
        	this.BufferedGain = new double[320];
        	this.bufferCount = 0;
        }

        /// <summary>
        /// The waveformat of this WaveProvider (same as the source)
        /// </summary>
        public WaveFormat WaveFormat
        {
            get { return waveFormat; }
        }

        /// <summary>
        /// Frequency for the Generator. (20.0 - 20000.0 Hz)
        /// Sin, Square, Triangle, SawTooth, Sweep (Start Frequency).
        /// </summary>
        public double Frequency { get; set; }

        /// <summary>
        /// Return Log of Frequency Start (Read only)
        /// </summary>
        public double FrequencyLog
        {
            get { return Math.Log(Frequency); }
        }

        /// <summary>
        /// End Frequency for the Sweep Generator. (Start Frequency in Frequency)
        /// </summary>
        public double FrequencyEnd { get; set; }

        /// <summary>
        /// Return Log of Frequency End (Read only)
        /// </summary>
        public double FrequencyEndLog
        {
            get { return Math.Log(FrequencyEnd); }
        }

        /// <summary>
        /// Gain for the Generator. (0.0 to 1.0)
        /// </summary>
        public double Gain { get; set; }

        /// <summary>
        /// Channel PhaseReverse
        /// </summary>
        public bool[] PhaseReverse { get; private set; }

        /// <summary>
        /// Type of Generator.
        /// </summary>
        public SignalGeneratorType Type { get; set; }

        /// <summary>
        /// Length Seconds for the Sweep Generator.
        /// </summary>
        public double SweepLengthSecs { get; set; }

        /// <summary>
        /// Reads from this provider.
        /// </summary>
        public int Read(float[] buffer, int offset, int count)
        {
            int outIndex = offset;

            // Generator current value
            double multiple;
            double sampleValue;
            double sampleSaw;
        	
        	int index = 0;
			double accumulator = 0.0;				
			double increment = 0.0;

			if(count > 0)
			{
				increment = bufferCount / count;
				}        	

            // Complete Buffer
            for (int sampleCount = 0; sampleCount < count/waveFormat.Channels; sampleCount++)
            {
            	index = (int) accumulator;
            	Gain = VMath.Lerp(BufferedGain[index], BufferedGain[index + 1], accumulator - index);
                switch (Type)
                {
                    case SignalGeneratorType.Sin:

                        // Sinus Generator

                        multiple = TwoPi*Frequency/waveFormat.SampleRate;
                        sampleValue = Gain*Math.Sin(nSample*multiple);

                        nSample++;

                        break;


                    case SignalGeneratorType.Square:

                        // Square Generator

                        multiple = 2*Frequency/waveFormat.SampleRate;
                        sampleSaw = ((nSample*multiple)%2) - 1;
                        sampleValue = sampleSaw > 0 ? Gain : -Gain;

                        nSample++;
                        break;

                    case SignalGeneratorType.Triangle:

                        // Triangle Generator

                        multiple = 2*Frequency/waveFormat.SampleRate;
                        sampleSaw = ((nSample*multiple)%2);
                        sampleValue = 2*sampleSaw;
                        if (sampleValue > 1)
                            sampleValue = 2 - sampleValue;
                        if (sampleValue < -1)
                            sampleValue = -2 - sampleValue;

                        sampleValue *= Gain;

                        nSample++;
                        break;

                    case SignalGeneratorType.SawTooth:

                        // SawTooth Generator

                        multiple = 2*Frequency/waveFormat.SampleRate;
                        sampleSaw = ((nSample*multiple)%2) - 1;
                        sampleValue = Gain*sampleSaw;

                        nSample++;
                        break;

                    case SignalGeneratorType.White:

                        // White Noise Generator
                        sampleValue = (Gain*NextRandomTwo());
                        break;

                    case SignalGeneratorType.Pink:

                        // Pink Noise Generator

                        double white = NextRandomTwo();
                        pinkNoiseBuffer[0] = 0.99886*pinkNoiseBuffer[0] + white*0.0555179;
                        pinkNoiseBuffer[1] = 0.99332*pinkNoiseBuffer[1] + white*0.0750759;
                        pinkNoiseBuffer[2] = 0.96900*pinkNoiseBuffer[2] + white*0.1538520;
                        pinkNoiseBuffer[3] = 0.86650*pinkNoiseBuffer[3] + white*0.3104856;
                        pinkNoiseBuffer[4] = 0.55000*pinkNoiseBuffer[4] + white*0.5329522;
                        pinkNoiseBuffer[5] = -0.7616*pinkNoiseBuffer[5] - white*0.0168980;
                        double pink = pinkNoiseBuffer[0] + pinkNoiseBuffer[1] + pinkNoiseBuffer[2] + pinkNoiseBuffer[3] + pinkNoiseBuffer[4] + pinkNoiseBuffer[5] + pinkNoiseBuffer[6] + white*0.5362;
                        pinkNoiseBuffer[6] = white*0.115926;
                        sampleValue = (Gain*(pink/5));
                        break;

                    case SignalGeneratorType.Sweep:

                        // Sweep Generator
                        double f = Math.Exp(FrequencyLog + (nSample*(FrequencyEndLog - FrequencyLog))/(SweepLengthSecs*waveFormat.SampleRate));

                        multiple = TwoPi*f/waveFormat.SampleRate;
                        phi += multiple;
                        sampleValue = Gain*(Math.Sin(phi));
                        nSample++;
                        if (nSample > SweepLengthSecs*waveFormat.SampleRate)
                        {
                            nSample = 0;
                            phi = 0;
                        }
                        break;

                    default:
                        sampleValue = 0.0;
                        break;
                }
				
                // Phase Reverse Per Channel
                for (int i = 0; i < waveFormat.Channels; i++)
                {
                    if (PhaseReverse[i])
                        buffer[outIndex++] = (float) -sampleValue;
                    else
                        buffer[outIndex++] = (float) sampleValue;
                }
            	accumulator += increment;
            }
        	bufferCount = 0;
            return count;
        }

        /// <summary>
        /// Private :: Random for WhiteNoise &amp; Pink Noise (Value form -1 to 1)
        /// </summary>
        /// <returns>Random value from -1 to +1</returns>
        private double NextRandomTwo()
        {
            return 2*random.NextDouble() - 1;
        }

    }

    /// <summary>
    /// Signal Generator type
    /// </summary>
    public enum SignalGeneratorType
    {
        /// <summary>
        /// Pink noise
        /// </summary>
        Pink,
        /// <summary>
        /// White noise
        /// </summary>
        White,
        /// <summary>
        /// Sweep
        /// </summary>
        Sweep,
        /// <summary>
        /// Sine wave
        /// </summary>
        Sin,
        /// <summary>
        /// Square wave
        /// </summary>
        Square,
        /// <summary>
        /// Triangle Wave
        /// </summary>
        Triangle,
        /// <summary>
        /// Sawtooth wave
        /// </summary>
        SawTooth,
    }

}

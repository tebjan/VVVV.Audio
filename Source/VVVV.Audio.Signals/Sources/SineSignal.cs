#region usings
using System;
using VVVV.PluginInterfaces.V2;
#endregion
namespace VVVV.Audio
{
	public class SineSignal : AudioSignal
	{
		public SineSignal(float frequency, float gain)
		{
			Frequency = frequency;
			Gain = gain;
		}

		public float Frequency;

		public float Gain = 0.1f;

		private float TwoPi = (float)(Math.PI * 2);

		private float phase = 0;

		protected override void FillBuffer(float[] buffer, int offset, int count)
		{
//			PerfCounter.Start("Sine");
			var increment = TwoPi * Frequency / SampleRate;
			for (int i = 0; i < count; i++) {
				// Sinus Generator
				buffer[i] = Gain * (float)Math.Sin(phase);
				phase += increment;
				if (phase > TwoPi)
					phase -= TwoPi;
				else
					if (phase < 0)
						phase += TwoPi;
			}
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





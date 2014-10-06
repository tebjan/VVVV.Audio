using System;
using System.Collections.Generic;

namespace VVVV.Audio
{
    public class SampleCallerSignal : AudioSignal
    {

        public Func<double, int, float> PerSample;

        double FTime = 0;
        int FSampleNumber = 0;
        protected override void FillBuffer(float[] buffer, int offset, int count)
        {
            var increment = 1.0/SampleRate;
            
            if(PerSample != null)
            {
                for (int i = 0; i < count; i++)
                {
                    //calc sample
                    buffer[i] = PerSample(FTime, FSampleNumber);
                    
                    //increment
                    FTime += increment;
                    FSampleNumber++;
                }
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    buffer[i] = 0;
                    
                    //increment
                    FTime += increment;
                    FSampleNumber++;
                }
            }
        }
    }
    
    public class AudioRender : IDisposable
    {
        AudioEngine FEngine;
        SampleCallerSignal FSignal = new SampleCallerSignal();
        List<MasterChannel> FMasterChannels = new List<MasterChannel>(2);
		
		public AudioRender()
		{
		    //setup engine
			FEngine = AudioService.Engine;
			FEngine.ChangeDriverSettings("ASIO4ALL v2", 44100, 2, 0, 2, 0);
			
			//create master channels
			FMasterChannels.Add(new MasterChannel(FSignal, 0));
			FMasterChannels.Add(new MasterChannel(FSignal, 1));
			FEngine.AddOutput(FMasterChannels);
			
		    //start rendering
			FEngine.Play = true;
		}
		
		public double Render(Func<double, int, float> perSample)
		{
		    FSignal.PerSample = perSample;
		    return FEngine.Timer.Time;
		}

		#region IDisposable implementation

		public void Dispose()
		{
		    FEngine.Dispose();
		}

		#endregion
    }
}
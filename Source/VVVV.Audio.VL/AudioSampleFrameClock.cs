using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using VL.Core;
using VL.Lib.Animation;

namespace VL.Lib.VAudio
{

    public class AudioSampleFrameClock : IFrameClock
    {
        Time FFrameTime;
        bool FInitialized;

        public AudioSampleFrameClock()
        {
        }

        public Time Time => FFrameTime;

        public void SetFrameTime(Time frameTime)
        {
            if (FInitialized)
                TimeDifference = frameTime.Seconds - FFrameTime.Seconds;
            else
                TimeDifference = 0;

            FFrameTime = frameTime;
            FInitialized = true;
        }

        public void IncrementTime(double timeDifference)
        {
            FFrameTime += timeDifference;
            TimeDifference = timeDifference;
            FInitialized = true;
        }

        public void Restart()
        {
            FInitialized = false;
            FFrameTime = 0;
            TimeDifference = 0;
        }

        public IObservable<FrameTimeMessage> GetTicks()
        {
            throw new NotImplementedException();
        }

        public IObservable<FrameFinishedMessage> GetFrameFinished()
        {
            throw new NotImplementedException();
        }

        public double TimeDifference { get; private set; }
    }

}
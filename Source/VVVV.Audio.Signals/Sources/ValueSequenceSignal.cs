/*
 * Created by SharpDevelop.
 * User: TF
 * Date: 24.12.2014
 * Time: 16:21
 * 
 */
 
using System;
using System.Linq;

namespace VVVV.Audio
{
    public class ValueSequence
    {
        float[] FTimes;
        float[] FValues;
        readonly double FLength;
        int FCount;
        bool FIsBang;
        
        public int Count
        {
            get
            {
                return FCount;
            }
        }
        
        AudioEngine FEngine;
        public ValueSequence(float[] times, float[] values, double length, bool isBang, AudioEngine engine)
        {
            FTimes = times;
            FCount = FTimes.Length;
            FEngine = engine;
            FValues = new float[FCount];
            FIsBang = isBang;
            
            for(int i=0; i<FCount; i++)
            {
                FValues[i] = values[i%values.Length];
            }
            
            Array.Sort(FTimes, FValues);
            FLength = Math.Max(Math.Abs(length), 0.00000520833f);
            
            //set state
            Next(FEngine.Timer.Beat % FLength);
        }
        
        int FIndex;
        double FNextValueTime;
        float FCurrentValue;
        
        void Next(double clipTime)
        {
            var beatToSamples = (60.0/FEngine.Timer.BPM) * FEngine.Settings.SampleRate;
            FCurrentSampleIndex = 0;
            
            // find next value index
            var nextTime = FNextValueTime;
            var valueIndex = Math.Max(FIndex - 1, 0);
            bool subtractLength = false;
            while(nextTime <= clipTime)
            { 
                FIndex++;
                if(FIndex >= FCount)
                {
                    FIndex = 0;
                    valueIndex = Math.Max(FCount - 1, 0);
                    nextTime = FTimes[FIndex] + FLength;
                    subtractLength = true;
                }
                else
                {
                    nextTime = FTimes[FIndex];
                    valueIndex = Math.Max(FIndex - 1, 0);
                    subtractLength = false;
                }
            }
            
            FCurrentValue = FValues[valueIndex];
            
            FNextValueSampleIndex = (int)((nextTime - clipTime) * beatToSamples);

            if(subtractLength)
                FNextValueTime = nextTime - FLength;
            else
                FNextValueTime = nextTime;
        }

        public double Position 
        {
            get;
            protected set;
        }
        
        int FCurrentSampleIndex;
        int FNextValueSampleIndex;
        public void Read(float[] buffer, double[] time, int offset, int count)
        {
            if(FCount > 0)
            {
                for(int i=0; i < count; i++)
                {
                    var clipTime = time[i] % FLength;

                    if (FCurrentSampleIndex >= FNextValueSampleIndex)
                    {
                        Next(clipTime);
                        if(FIsBang)
                            buffer[i] = 1;
                    }
                    else if (FIsBang)
                    {
                        buffer[i] = 0;
                    }
                    
                    if(!FIsBang)
                        buffer[i] = FCurrentValue;
                    
                    Position = clipTime;
                    
                    //inc counter
                    FCurrentSampleIndex++;
                }
            }
        }
    }
    
    public class ValueSequenceSignal : AudioSignal
    {
        //inputs
        SigParamDiff<float> Length = new SigParamDiff<float>("Length", 4);
        SigParamDiff<float[]> Times = new SigParamDiff<float[]>("Positions");
        SigParamDiff<float[]> Values = new SigParamDiff<float[]>("Values");
        SigParamDiff<bool> IsBang = new SigParamDiff<bool>("Is Bang");
        
        //output
        SigParam<double> Position = new SigParam<double>("Position", true);
        
        public ValueSequenceSignal()
        {
            Times.ValueChanged = TimesChanged;
            Values.ValueChanged = ValuesChanged;
            Length.ValueChanged = LengthChanged;
            IsBang.ValueChanged = IsBangChanged;
        }

        void LengthChanged(float obj)
        {
            BuildSequence();
        }
        
        void TimesChanged(float[] obj)
        {
            BuildSequence();
        }

        void ValuesChanged(float[] obj)
        {
            BuildSequence();
        }
        
        void IsBangChanged(bool obj)
        {
            BuildSequence();
        }
        
        ValueSequence FSequence = null;
        void BuildSequence()
        {
            if(Times.Value != null && Values.Value != null && Length.Value > 0)
                FSequence = new ValueSequence(Times.Value, Values.Value, Length.Value, IsBang.Value, AudioService.Engine);
            else
                FSequence = null;
        }

        protected double LoopTimer;
        protected override void FillBuffer(float[] buffer, int offset, int count)
        {
            var seq = FSequence;
            if(seq != null)
            {
                seq.Read(buffer, AudioService.Engine.Timer.BeatBuffer, offset, count);
                Position.Value = FSequence.Position;
            }
        }
    }

}

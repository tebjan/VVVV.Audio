/*
 * Created by SharpDevelop.
 * User: TF
 * Date: 24.12.2014
 * Time: 16:21
 * 
 */
 
using System;
using System.Linq;

namespace VVVV.Audio.MIDI
{
    public struct MidiNoteData
    {
        public byte NoteNumber;
        public byte Velocity;
    }
    
    public class MidiSequence
    {
        float[] FTimes;
        MidiNoteData[] FValues;
        readonly double FLength;
        int FCount;
        
        public int Count
        {
            get
            {
                return FCount;
            }
        }
        
        ManualMidiEvents FEventSender;
        AudioEngine FEngine;
        public MidiSequence(float[] times, float[] notes, float[] lengths, float[] velocities, double length, ManualMidiEvents eventSender, AudioEngine engine)
        {
            FEventSender = eventSender;
            
            //double length for on and off events
            FTimes = new float[times.Length * 2];
            FCount = FTimes.Length;
            FEngine = engine;
            FValues = new MidiNoteData[FCount];
            
            var i = 0;
            var j = 0;
            foreach (var time in times)
            {
                FTimes[i] = Math.Max(time, 0);
                FTimes[i+1] = FTimes[i] + Math.Max(lengths[j % lengths.Length], 0.00000520833f);
                
                var noteOn = new MidiNoteData() { 
                    NoteNumber = (byte)notes[j % notes.Length], 
                    Velocity = (byte) (velocities[j % velocities.Length]*255) 
                };
                
                var noteOff = new MidiNoteData() {
                    NoteNumber = noteOn.NoteNumber,
                    Velocity = (byte)0
                };
                
                FValues[i] = noteOn;
                FValues[i+1] = noteOff;
                
                i += 2;
                j++;
            }
            
            
            Array.Sort(FTimes, FValues);
            FLength = Math.Max(length, 0);
            
            //set state
            Next(FEngine.Timer.Beat % FLength);
        }
        
        int FIndex;
        double FNextValueTime;
        MidiNoteData FCurrentValue;
        
        void Next(double clipTime)
        {
            var beatToSamples = (60.0/FEngine.Timer.BPM) * FEngine.Settings.SampleRate;
            FCurrentSampleIndex = 0;
            
            // find next value index
            var nextTime = FNextValueTime;
            var valueIndex = Math.Max(FIndex - 1, 0);
            var subtractLength = false;
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
        public void Read(double[] time, int count)
        {
            if(FCount > 0)
            {
                for(int i=0; i < count; i++)
                {
                    var clipTime = time[i] % FLength;
                    
                    if (FCurrentSampleIndex >= FNextValueSampleIndex)
                    {
                        Next(clipTime);
                        FEventSender.SendRawMessage(i, 144, FCurrentValue.NoteNumber, FCurrentValue.Velocity);
                        
                    }
                    
                    Position = clipTime;
                    
                    //inc counter
                    FCurrentSampleIndex++;
                }
            }
        }
    }
    
    public class MidiSequenceSignal : NotifyProccessSinkSignal
    {
        //inputs
        SigParamDiff<float> Length = new SigParamDiff<float>("Length", 4);
        SigParamDiff<float[]> Times = new SigParamDiff<float[]>("Times");
        SigParamDiff<int[]> Values = new SigParamDiff<int[]>("Notes");
        SigParamDiff<float[]> Velocities = new SigParamDiff<float[]>("Velocities");
        SigParamDiff<float[]> Lengths = new SigParamDiff<float[]>("Lengths");
        SigParam<int> Channel = new SigParam<int>("Channel");
        //SigParam<bool> EventType = new SigParam<bool>("Is Bang",);
        
        //output
        SigParam<MidiEvents> MidiEvents = new SigParam<MidiEvents>("Events", true);
        SigParam<double> Position = new SigParam<double>("Position", true);
        
        public MidiSequenceSignal()
        {
            Times.ValueChanged = TimesChanged;
            Values.ValueChanged = ValuesChanged;
            Lengths.ValueChanged = LengthsChanged;
            Velocities.ValueChanged = VelocitiesChanged;
            Length.ValueChanged = LengthChanged;
            
            MidiEvents.Value = new ManualMidiEvents();
        }
        
        void VelocitiesChanged(float[] obj)
        {
            BuildSequence();
        }
        
        void LengthsChanged(float[] obj)
        {
            BuildSequence();
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
        
        MidiSequence FSequence = null;
        void BuildSequence()
        {
            if(Times.Value != null && Values.Value != null && Lengths.Value != null && Velocities.Value != null && Length.Value > 0)
                FSequence = new MidiSequence(Times.Value, 
                                             Values.Value, 
                                             Lengths.Value, 
                                             Velocities.Value, 
                                             Length.Value, 
                                             MidiEvents.Value as ManualMidiEvents, 
                                             AudioService.Engine);
            else
                FSequence = null;
        }

        public override void NotifyProcess(int count)
        {
            var seq = FSequence;
            if(seq != null)
            {
                seq.Read(AudioService.Engine.Timer.BeatBuffer, count);
                Position.Value = FSequence.Position;
            }
        }
    }

}

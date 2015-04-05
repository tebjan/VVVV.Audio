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
    public class TimeArrayIterator
    {
        readonly float[] FTimes;
        int FIndex = 0;
        readonly double FLength;
        public int Index { get { return FIndex; } }
        
        public TimeArrayIterator(float[] times, double length)
        {
            FTimes = times;
            FLength = length;
        }
        
        /// <summary>
        /// Move the index to the time just before the input time
        /// </summary>
        /// <param name="currentTime"></param>
        public void Init(double currentTime)
        {
            var i = 0;
            foreach (var time in FTimes) 
            {
                if(time > currentTime)
                {
                    break;
                }
                i++;
            }
            
            if(i >= FTimes.Length)
            {
                i = 0;
            }
            
            FIndex = i;
        }
        
        /// <summary>
        /// Returns the next time value
        /// </summary>
        /// <returns></returns>
        public float NextTime()
        {
            FIndex++;
            if(FIndex >= FTimes.Length)
            {
                FIndex = FTimes.Length-1;
            }
            
            return FTimes[FIndex];
        }
        
        public bool Finished 
        {
            get
            {
                return FIndex == FTimes.Length-1;
            }
        }
        
        public void Reset()
        {
            FIndex = 0;
        }

        /// <summary>
        /// True if the current index time is lower than the input time
        /// </summary>
        /// <param name="endTime"></param>
        /// <returns></returns>
        public bool HasTime(double endTime)
        {
            return FTimes[FIndex] < endTime && !Finished;           
        }

    }
    
    public struct MidiNoteData
    {
        public byte NoteNumber;
        public byte Velocity;
    }
    
    public class MidiSequence
    {
        float[] FStartTimes;
        float[] FEndTimes;
        TimeArrayIterator FOns;
        TimeArrayIterator FOffs;
        MidiNoteData[] FNoteOns;
        MidiNoteData[] FNoteOffs;
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
        public MidiSequence(float[] times, int[] notes, float[] lengths, float[] velocities, double length, ManualMidiEvents eventSender, AudioEngine engine)
        {
            FEventSender = eventSender;
            
            //double length for on and off events
            FStartTimes = new float[times.Length];
            FEndTimes = new float[times.Length];
            FCount = FStartTimes.Length;
            FEngine = engine;
            FNoteOns = new MidiNoteData[FCount];
            FNoteOffs = new MidiNoteData[FCount];
            
            var j = 0;
            foreach (var time in times)
            {
                FStartTimes[j] = Math.Max(time, 0);
                FEndTimes[j] = FStartTimes[j] + Math.Max(lengths[j % lengths.Length], 0.00000520833f);
                
                var noteOn = new MidiNoteData() { 
                    NoteNumber = (byte)notes[j % notes.Length], 
                    Velocity = (byte) (velocities[j % velocities.Length]*255) 
                };
                
                var noteOff = new MidiNoteData() {
                    NoteNumber = noteOn.NoteNumber,
                    Velocity = (byte)0
                };
                
                FNoteOns[j] = noteOn;
                FNoteOffs[j] = noteOff;
                
                j++;
            }
            
            FLength = Math.Max(length, 0.0000001);
        }
        
        /// <summary>
        /// Sets the sequence to the current beat
        /// </summary>
        public void Init(double currentBeat)
        {
            Array.Sort(FStartTimes, FNoteOns);
            Array.Sort(FEndTimes, FNoteOffs);
            
            FOns = new TimeArrayIterator(FStartTimes, FLength);
            FOffs = new TimeArrayIterator(FEndTimes, FLength);
            
            var currentSeqTime = currentBeat % FLength;
            
            FOns.Init(currentSeqTime);
            FOffs.Init(currentSeqTime);
            
            FLastEndBeat = currentBeat;
        }
        
        public double Position 
        {
            get;
            protected set;
        }
        
        double FLastEndBeat;
        void NextEvents(double endBeat)
        {
            //if the sequence wrapps, the times have to wrap too
            if(endBeat < FLastEndBeat)
            {
                while(!FOffs.Finished)
                {
                    var noteOff = FNoteOffs[FOffs.Index];
                    FEventSender.SendRawMessage(0, (byte)144, noteOff.NoteNumber, noteOff.Velocity);
                    var time = FOffs.NextTime();
                }
                
                while(!FOns.Finished)
                {
                    var noteOn = FNoteOns[FOns.Index];
                    FEventSender.SendRawMessage(0, (byte)144, noteOn.NoteNumber, noteOn.Velocity);
                    var time = FOns.NextTime();
                }
                
                FOffs.Reset();
                FOns.Reset();
            }

            while(FOffs.HasTime(endBeat))
            {
                var noteOff = FNoteOffs[FOffs.Index];
                FEventSender.SendRawMessage(0, (byte)144, noteOff.NoteNumber, noteOff.Velocity);
                var time = FOffs.NextTime();
            }
            
            while(FOns.HasTime(endBeat))
            {
                var noteOn = FNoteOns[FOns.Index];
                FEventSender.SendRawMessage(0, (byte)144, noteOn.NoteNumber, noteOn.Velocity);
                var time = FOns.NextTime();
            }

            
            FLastEndBeat = endBeat;
        }
        
        public void Read(double[] beatBuffer, int count)
        {
            if(FCount > 0)
            {
//                for(int i=0; i < count; i++)
//                {
//                    var clipTime = time[i] % FLength;
//
//                    if (FCurrentSampleIndex >= FNextValueSampleIndex)
//                    {
//                        Next(clipTime);
//                        FEventSender.SendRawMessage(i, 144, FCurrentValue.NoteNumber, FCurrentValue.Velocity);
//
//                    }
//
//                    Position = clipTime;
//
//                    //inc counter
//                    FCurrentSampleIndex++;
//                }
                
                NextEvents(beatBuffer[beatBuffer.Length - 1] % FLength);
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

        void ValuesChanged(int[] obj)
        {
            BuildSequence();
        }
        
        MidiSequence FSequence = null;
        void BuildSequence()
        {
            if(Times.Value != null && Values.Value != null && Lengths.Value != null && Velocities.Value != null && Length.Value > 0)
            {
                FSequence = new MidiSequence(Times.Value, 
                                             Values.Value, 
                                             Lengths.Value, 
                                             Velocities.Value, 
                                             Length.Value, 
                                             MidiEvents.Value as ManualMidiEvents, 
                                             AudioService.Engine);
                
                FNeedsInit = true;
            }
            else
                FSequence = null;
        }
        bool FNeedsInit;

        public override void NotifyProcess(int count)
        {
            var seq = FSequence;
            
            if(seq != null)
            {
                if(FNeedsInit)
                {
                    seq.Init(AudioService.Engine.Timer.Beat);
                }
                
                seq.Read(AudioService.Engine.Timer.BeatBuffer, count);
                Position.Value = FSequence.Position;
                
                FNeedsInit = false;
            }
        }
    }

}

/*
 * Created by SharpDevelop.
 * User: TF
 * Date: 24.12.2014
 * Time: 16:21
 * 
 */

using Sanford.Multimedia.Midi;
using System;
using System.Linq;

namespace VVVV.Audio.MIDI
{
    /// <summary>
    /// Iterates thru an array of time values
    /// </summary>
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
        public void Next()
        {
            FIndex++;
            if(FIndex > FTimes.Length-1)
            {
                FIndex = FTimes.Length-1;
                FFinished = true;
            }
        }
        
        public float Time
        {
            get
            {
                return FTimes[FIndex];
            }
        }
        
        bool FFinished;
        public bool Finished 
        {
            get
            {
                return FFinished;
            }
        }
        
        public void Reset()
        {
            FFinished = false;
        }

        /// <summary>
        /// True if the current index time is lower than the input time
        /// </summary>
        /// <param name="endTime"></param>
        /// <returns></returns>
        public bool HasTime(double endTime)
        {
            return FTimes[FIndex] < endTime && !FFinished;  
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
            FEngine = engine;
            FEventSender = eventSender;
            
            //note count
            FCount = times.Length;
            
            //arrays for on and off events
            FStartTimes = new float[FCount];
            FEndTimes = new float[FCount];
 
            FNoteOns = new MidiNoteData[FCount];
            FNoteOffs = new MidiNoteData[FCount];
            
            var i = 0;
            foreach (var time in times)
            {
                FStartTimes[i] = Math.Max(time, 0);
                
                //end times must be at least about one sample after the note on
                FEndTimes[i] = FStartTimes[i] + Math.Max(lengths[i % lengths.Length], 0.00000520833f);
                
                var noteOn = new MidiNoteData() { 
                    NoteNumber = (byte)notes[i % notes.Length], 
                    Velocity = (byte) (velocities[i % velocities.Length]*255) 
                };
                
                var noteOff = new MidiNoteData() {
                    NoteNumber = noteOn.NoteNumber,
                    Velocity = (byte)0
                };
                
                FNoteOns[i] = noteOn;
                FNoteOffs[i] = noteOff;
                
                i++;
            }
            
            FLength = Math.Max(Math.Abs(length), 0.00000520833f);
        }
        
        /// <summary>
        /// Sets the sequence to the current beat, called just before first Read() call
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
                FOffs.Reset();
                FOns.Reset();
            }

            while(FOffs.HasTime(endBeat))
            {
                var noteOff = FNoteOffs[FOffs.Index];
                FEventSender.SendRawMessage(0, (byte)144, noteOff.NoteNumber, noteOff.Velocity);
                FOffs.Next();
            }
            
            while(FOns.HasTime(endBeat))
            {
                var noteOn = FNoteOns[FOns.Index];
                FEventSender.SendRawMessage(0, (byte)144, noteOn.NoteNumber, noteOn.Velocity);
                FOns.Next();
            }

            
            FLastEndBeat = endBeat;
        }
        
        /// <summary>
        /// Called from the sequence signal just befor the audio buffer will be calculated
        /// </summary>
        /// <param name="beatBuffer">Array of beat time values for each sample of the next buffer</param>
        /// <param name="count"></param>
        public void Read(double[] beatBuffer, int count)
        {
            if(FCount > 0)
            {              
                NextEvents(beatBuffer[beatBuffer.Length - 1] % FLength);
            }
        }
    }
    
    
    public class MidiSequenceSignal : NotifyProccessSinkSignal
    {
        //inputs
        SigParamDiff<float> Length = new SigParamDiff<float>("Length", 4);
        SigParamDiff<float[]> Times = new SigParamDiff<float[]>("Positions");
        SigParamDiff<int[]> Notes = new SigParamDiff<int[]>("Notes");
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
            Notes.ValueChanged = NotesChanged;
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

        void NotesChanged(int[] obj)
        {
            BuildSequence();
        }
        
        MidiSequence FSequence = null;
        void BuildSequence()
        {
            if(Times.Value != null && Notes.Value != null && Lengths.Value != null && Velocities.Value != null && Length.Value > 0)
            {
                FSequence = new MidiSequence(Times.Value, 
                                             Notes.Value, 
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

        /// <summary>
        /// Called just befor the audio buffer will be calculated
        /// </summary>
        /// <param name="count"></param>
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

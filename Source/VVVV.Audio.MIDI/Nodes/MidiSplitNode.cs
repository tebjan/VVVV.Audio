/*
 * Created by SharpDevelop.
 * User: TF
 * Date: 03.04.2015
 * Time: 03:57
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using Sanford.Multimedia.Midi;
using VVVV.PluginInterfaces.V2;
using System.ComponentModel.Composition;
using VVVV.Audio.MIDI;
using System.Reactive.Linq;

namespace VVVV.Nodes
{
    public class MidiEventSourceManager : IEnumerator<IList<ShortMessageEventArgs>>, IDisposable
    {
        public readonly IEnumerator<IList<ShortMessageEventArgs>> FReceivedEvents;
        
        public MidiEventSourceManager(MidiEvents evts)
        {
            if(evts != null)
            {
                var obs = Observable.FromEventPattern<ShortMessageEventArgs>(evts, "ShortMessageReceived");
                FReceivedEvents = obs.Select(evt => evt.EventArgs).Chunkify().GetEnumerator();
            }
            else
            {
                FReceivedEvents = Observable.Empty<ShortMessageEventArgs>().Chunkify().GetEnumerator();
            }
        }

        public bool MoveNext()
        {
            return FReceivedEvents.MoveNext();
        }
        
        public void Reset()
        {
            FReceivedEvents.Reset();
        }

        object System.Collections.IEnumerator.Current 
        {
            get 
            {
                return FReceivedEvents.Current;
            }
        }
        
        public IList<ShortMessageEventArgs> Current 
        {
            get 
            {
                return FReceivedEvents.Current;
            }
        }      
        
        public void Dispose()
        {
            FReceivedEvents.Dispose();
        }
    }
    
    
    [PluginInfo(Name = "MidiSplit", Category = "VAudio", Version = "Sink", Help = "Splits gathers midi events and splits it into its raw format", Tags = "MidiShort", Author = "tonfilm")]
    public class MidiSplitNode : IPluginEvaluate, IPartImportsSatisfiedNotification
    {
        [Input("Events")]
        IDiffSpread<MidiEvents> FEventsIn;

        [Output("Status")]
        ISpread<ISpread<int>> FMessageOut;
        
        [Output("Data 1")]
        ISpread<ISpread<int>> FData1Out;
        
        [Output("Data 2")]
        ISpread<ISpread<int>> FData2Out;
        
        [Output("Sample Offset")]
        ISpread<ISpread<int>> FSampleOffsetOut;
        
        [Output("On Data", IsBang = true)]
        ISpread<bool> FOnDataOut;
        
        Spread<MidiEventSourceManager> FEventSources = new Spread<MidiEventSourceManager>();

        public void OnImportsSatisfied()
        {
            FEventsIn.SliceCount = 0;
        }

        public void Evaluate(int SpreadMax)
        {

            FOnDataOut.SliceCount = SpreadMax;
            FMessageOut.ResizeAndDismiss(SpreadMax, () => new Spread<int>());
            FData1Out.ResizeAndDismiss(SpreadMax, () => new Spread<int>());
            FData2Out.ResizeAndDismiss(SpreadMax, () => new Spread<int>());
            FSampleOffsetOut.ResizeAndDismiss(SpreadMax, () => new Spread<int>());
            
            if(FEventsIn.IsChanged)
            { 
                FEventSources.SliceCount = SpreadMax;

                for (int i = 0; i < SpreadMax; i++) 
                {
                    FEventSources[i] = new MidiEventSourceManager(FEventsIn[i]);
                }
            }
            
            for (int i = 0; i < SpreadMax; i++)
            {               
                var eventSource = FEventSources[i];
                
                var messageSpread = FMessageOut[i];
                var data1Spread = FData1Out[i];
                var data2Spread = FData2Out[i];
                var sampleOffsetSpread = FSampleOffsetOut[i];
                
                messageSpread.SliceCount = 0;
                data1Spread.SliceCount = 0;
                data2Spread.SliceCount = 0;
                FSampleOffsetOut.SliceCount = 0;
                
                if(eventSource.MoveNext())
                {
                    var evts = eventSource.Current;
                    if(evts.Count > 0)
                    {
                        FOnDataOut[i] = true;
                        foreach (var evt in evts) 
                        {
                            messageSpread.Add(evt.Message.Bytes[0]);
                            data1Spread.Add(evt.Message.Bytes[1]);
                            data2Spread.Add(evt.Message.Bytes[2]);
                            sampleOffsetSpread.Add(evt.Message.DeltaFrames);
                        }
                    }
                    else
                    {
                        FOnDataOut[i] = false;
                    }
                }
                else
                {
                    FOnDataOut[i] = false;
                }
            }
        }
    }
}

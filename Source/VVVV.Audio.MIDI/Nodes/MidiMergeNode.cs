/*
 * Created by SharpDevelop.
 * User: TF
 * Date: 03.04.2015
 * Time: 04:50
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Audio.MIDI;
using Sanford.Multimedia.Midi;

namespace VVVV.Nodes
{
    /// <summary>
    /// Description of MidiMergeNode.
    /// </summary>
    [PluginInfo(Name = "MidiMerge", Category = "VAudio", Version = "Filter", Help = "Merges multiple midi event senders into one", Tags = "join", Author = "tonfilm")]
    public class MidiMergeNode : IPluginEvaluate
    {
        [Input("Events", IsPinGroup = true)]
        IDiffSpread<ISpread<MidiEvents>> FEventsIn;
        
        [Output("Events")]
        ISpread<MidiEvents> FEventsOut;

        public void Evaluate(int SpreadMax)
        {
            var pinCount = FEventsIn.SliceCount;
            FEventsOut.SliceCount = SpreadMax;
            
            if(FEventsIn.IsChanged)
            {
                for (int i = 0; i < SpreadMax; i++)
                {
                    FEventsOut[i] = new MergeMidiEvents(Merge(i, pinCount));
                }
            }
        }

        IEnumerable<MidiEvents> Merge(int slice, int pinCount)
        {
            for (int i = 0; i < pinCount; i++) 
            {
                yield return FEventsIn[i][slice];
            }            
        }
    }
}

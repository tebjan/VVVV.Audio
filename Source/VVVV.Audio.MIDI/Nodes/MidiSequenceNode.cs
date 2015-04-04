/*
 * Created by SharpDevelop.
 * User: TF
 * Date: 24.12.2014
 * Time: 16:21
 * 
 */
 
using System;
using System.Linq;
using VVVV.PluginInterfaces.V2;
using VVVV.Audio;
using VVVV.Audio.MIDI;

namespace VVVV.Nodes
{  
    [PluginInfo(Name = "MidiSequence", Category = "VAudio", Version = "Source", Help = "Generates a sequence of midi events which are played back in the audio thread", Tags = "sequencer, clip, loop", Author = "tonfilm")]
    public class ValueSequenceNode : AutoAudioSignalNode<MidiSequenceSignal>
    {
        protected override PinVisibility GetOutputVisiblilty()
        {
            return PinVisibility.False;
        }
    }
}

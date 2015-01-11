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

namespace VVVV.Nodes
{  
    [PluginInfo(Name = "ValueSequence", Category = "VAudio", Version = "Source", Help = "Generates a sequence of values which are played back in the audio thread", Tags = "sequencer, clip, loop", Author = "tonfilm")]
    public class ValueSequenceNode : AutoAudioSignalNode<ValueSequenceSignal>
    {
    }
}

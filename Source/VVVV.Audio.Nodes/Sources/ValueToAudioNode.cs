/*
 * Created by SharpDevelop.
 * User: TF
 * Date: 31.12.2014
 * Time: 19:38
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using VVVV.Audio;
using VVVV.PluginInterfaces.V2;

namespace VVVV.Nodes
{

    [PluginInfo(Name = "V2A", Category = "VAudio", Version = "Source", Help = "Converts a value into a static audio signal", AutoEvaluate = true, Tags = "")]
    public class ValueToAudioNode : AutoAudioSignalNode<ValueToAudioSignal>
    {
    }
}

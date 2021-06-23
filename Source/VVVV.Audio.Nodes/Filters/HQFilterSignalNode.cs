#region usings
using System;
using System.ComponentModel.Composition;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.PluginInterfaces.V2.NonGeneric;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;
using VVVV.Audio;
using VVVV.Core.Logging;

#endregion usings

namespace VVVV.Nodes
{
    
    [PluginInfo(Name = "Filter", Category = "VAudio", Version = "Filter", Help = "Analog modeling filter", AutoEvaluate = true, Tags = "Moog, Zero Delay")]
    public class AnalogModelingFilterSignalNode : AutoAudioSignalNode<AnalogModelingFilterSignal>
    {
        
    }
}

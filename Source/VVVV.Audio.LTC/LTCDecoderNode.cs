#region usings
using System;
using System.ComponentModel.Composition;
using System.Collections.Generic;
using System.Linq;

using System.Runtime.InteropServices;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Audio;

using LTCSharp;

using VVVV.Core.Logging;
#endregion usings

namespace VVVV.Nodes
{
    
    [PluginInfo(Name = "LTCDecoder", Category = "VAudio", Version = "Sink", Help = "Decodes LTC audio signals", Tags = "timecode, SMPTE, synchronization")]
    public class LTCDecoderSignalNode : GenericAudioSinkNode<LTCDecoderSignal>
    {        
        
        [Output("Timecode")]
        public ISpread<Timecode> FTimecodeOut;

        protected override void SetOutputs(int i, LTCDecoderSignal instance)
        {
            if (instance != null)
            {
                FTimecodeOut[i] = instance.Timecode;
            }
            else
            {
                FTimecodeOut[i] = new Timecode();
            }
        }

        protected override void SetOutputSliceCount(int sliceCount)
        {
            FTimecodeOut.SliceCount = sliceCount;
        }

        protected override LTCDecoderSignal GetInstance(int i)
        {
            return new LTCDecoderSignal(FInputs[i]);
        }

        protected override void SetParameters(int i, LTCDecoderSignal instance)
        {
            instance.InputSignal.Value = FInputs[i];
        }
    }
}



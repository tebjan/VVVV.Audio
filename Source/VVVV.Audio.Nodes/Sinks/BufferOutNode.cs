#region usings
using System;
using System.ComponentModel.Composition;
using System.Collections.Generic;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;
using VVVV.Audio;


using VVVV.Core.Logging;
#endregion usings

namespace VVVV.Nodes
{
    
    
    [PluginInfo(Name = "GetBuffer", Category = "VAudio", Version = "Sink", Help = "Returns the last N samples rescaled to M values", Tags = "Scope, Samples")]
    public class BufferOutNode : GenericAudioSinkNode<BufferOutSignal>
    {
        [Input("Buffer Size", DefaultValue = 512)]
        public IDiffSpread<int> FSize;
        
        [Input("Spread Count", DefaultValue = 512)]
        public IDiffSpread<int> FSpreadCount;
        
        [Output("Buffer")]
        public ISpread<ISpread<float>> FBufferOut;

        protected override void SetOutputs(int i, BufferOutSignal instance)
        {
            if (instance != null)
            {
                var spread = FBufferOut[i];
                spread.SliceCount = FSpreadCount[i];
                AudioUtils.ResampleMax(instance.BufferOut, spread.Stream.Buffer, spread.SliceCount);
                FBufferOut[i] = spread;
            }
            else
            {
                FBufferOut[i].SliceCount = 0;
            }
        }

        protected override void SetOutputSliceCount(int sliceCount)
        {
            FBufferOut.SliceCount = sliceCount;
        }

        protected override BufferOutSignal GetInstance(int i)
        {
            return new BufferOutSignal(FInputs[i]);
        }

        protected override void SetParameters(int i, BufferOutSignal instance)
        {
            instance.InputSignal.Value = FInputs[i];
            instance.Buffer.Size = FSize[i];
        }
    }
}



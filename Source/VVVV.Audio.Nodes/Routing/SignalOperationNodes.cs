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
using System.Linq;
#endregion usings

namespace VVVV.Nodes
{

    public class SignalOperationNode<TOperator> : IPluginEvaluate where TOperator : AudioSignalOperator, new()
    {
        [Input("Input", IsPinGroup = true)]
        public IDiffSpread<ISpread<AudioSignal>> Inputs;

        [Output("Audio Out")]
        public ISpread<AudioSignal> OutBuffer;

        public void Evaluate(int SpreadMax)
        {
            if (Inputs.IsChanged)
            {
                //old signals must be disposed
                OutBuffer.Resize(0, () => { return null; }, s => { if (s != null) s.Dispose(); });
                for (int outSlice = 0; outSlice < SpreadMax; outSlice++)
                {
                    var sig = new TOperator();
                    sig.Inputs = new List<AudioSignal>(Inputs.SliceCount);

                    for (int i = 0; i < Inputs.SliceCount; i++)
                    {
                        sig.Inputs.Add(Inputs[i][outSlice]);
                    }

                    OutBuffer.Add(sig);
                }
            }
        }
    }

    public class SignalOperationSpectralNode<TOperator> : IPluginEvaluate where TOperator : AudioSignalOperator, new()
    {
        [Input("Input")]
        public IDiffSpread<ISpread<AudioSignal>> Inputs;

        [Output("Audio Out")]
        public ISpread<AudioSignal> OutBuffer;

        public void Evaluate(int SpreadMax)
        {
            if (Inputs.IsChanged)
            {
                var count = Inputs.SliceCount;
                if (Inputs[0].SliceCount == 0) count = 0;

                OutBuffer.Resize(count, () => { return new TOperator(); }, s => { if (s != null) s.Dispose(); });
                for (int outSlice = 0; outSlice < OutBuffer.SliceCount; outSlice++)
                {
                    if (OutBuffer[outSlice] == null) OutBuffer[outSlice] = new TOperator();
                    (OutBuffer[outSlice] as AudioSignalOperator).Inputs = Inputs[outSlice].ToList();
                }
            }
        }
    }

    [PluginInfo(Name = "Multiply", Category = "VAudio", Help = "Multiplies audio signals", AutoEvaluate = true, Tags = "AM")]
    public class SignalMultiplyNode : SignalOperationNode<AudioSignalMultiplyOperator>
    {
    }

    [PluginInfo(Name = "Multiply", Category = "VAudio", Version = "Spectral", Help = "Multiplies audio signals", AutoEvaluate = true, Tags = "AM")]
    public class SignalMultiplySpectralNode : SignalOperationSpectralNode<AudioSignalMultiplyOperator>
    {
    }

    [PluginInfo(Name = "Add", Category = "VAudio", Help = "Adds audio signals", AutoEvaluate = true, Tags = "Mix")]
    public class SignalAddNode : SignalOperationNode<AudioSignalAddOperator>
    {
    }

    [PluginInfo(Name = "Add", Category = "VAudio", Version = "Spectral", Help = "Adds audio signals", AutoEvaluate = true, Tags = "Mix")]
    public class SignalAddSpectralNode : SignalOperationSpectralNode<AudioSignalAddOperator>
    {
    }

    [PluginInfo(Name = "Subtract", Category = "VAudio", Help = "Subtracts audio signals", AutoEvaluate = true, Tags = "Difference")]
    public class SignalSubtractNode : SignalOperationNode<AudioSignalSubtractOperator>
    {
    }

    [PluginInfo(Name = "Subtract", Category = "VAudio", Version = "Spectral", Help = "Subtracts audio signals", AutoEvaluate = true, Tags = "Difference")]
    public class SignalSubtractSpectralNode : SignalOperationSpectralNode<AudioSignalSubtractOperator>
    {
    }


}



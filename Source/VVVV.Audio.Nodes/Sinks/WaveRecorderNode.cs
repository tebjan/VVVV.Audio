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
	
	[PluginInfo(Name = "Writer", Category = "VAudio", Version = "Sink", Help = "Records audio to disk", Tags = "file, wave, record")]
	public class WaveWriterNode : GenericAudioSinkNode<WaveRecorderSignal>
	{

        [Input("Write")]
        public IDiffSpread<bool> FWriteIn;

        [Input("Filename", DefaultString = "", StringType = StringType.Filename, FileMask = ".wav")]
        public IDiffSpread<string> FNameIn;

        [Output("Samples Written")]
        public ISpread<int> FSamplesWrittenOut;

        protected override void SetOutputs(int i, WaveRecorderSignal instance)
        {
            FSamplesWrittenOut[i] = instance.SamplesWritten;
        }

        protected override void SetOutputSliceCount(int sliceCount)
        {
            FSamplesWrittenOut.SliceCount = sliceCount;
        }

        protected override WaveRecorderSignal GetInstance(int i)
        {
            return new WaveRecorderSignal();
        }

        protected override void SetParameters(int i, WaveRecorderSignal instance)
        {
            instance.InputSignal.Value = FInputs[i];
            instance.Filename = FNameIn[i];
            instance.Write = FWriteIn[i];
        }

        //dont forget to close the files and write the headers
        public override void Dispose()
        {
            foreach (var item in FSignals)
            {
                if (item != null)
                    item.Dispose();
            }
            base.Dispose();
        }
    }
}



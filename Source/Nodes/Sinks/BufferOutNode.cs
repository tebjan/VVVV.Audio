#region usings
using System;
using System.ComponentModel.Composition;
using System.Collections.Generic;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;
using VVVV.Audio;

using NAudio.Wave;
using NAudio.Wave.Asio;
using NAudio.CoreAudioApi;
using NAudio.Wave.SampleProviders;
using NAudio.Utils;


using VVVV.Core.Logging;
#endregion usings

namespace VVVV.Nodes
{
	public class BufferOutSignal : SinkSignal<float[]>
	{
		public BufferOutSignal(AudioSignal input)
		{
			FInput = input;
		}
		
		protected override void FillBuffer(float[] buffer, int offset, int count)
		{
            if (FInput != null)
            {
                FInput.Read(buffer, offset, count);
                this.SetLatestValue((float[])buffer.Clone());
            }
            else
            {
                this.SetLatestValue(new float[1]);
            }
		}
	}
	
	[PluginInfo(Name = "GetBuffer", Category = "VAudio", Version = "Sink", Help = "Returns a complete buffer", Tags = "Scope, Samples")]
    public class BufferOutNode : GenericAudioSinkNodeWithOutputs<BufferOutSignal, float[]>
	{
		
		[Output("Buffer")]
		public ISpread<ISpread<float>> FBufferOut;

        protected override void SetOutputs(int i, BufferOutSignal instance)
        {
            if (instance != null)
            {
                var spread = FBufferOut[i];
                float[] buffer = null;
                instance.GetLatestValue(out buffer);
                if (buffer != null)
                {
                    if (spread == null)
                    {
                        spread = new Spread<float>(buffer.Length);
                    }
                    spread.SliceCount = buffer.Length;
                    spread.AssignFrom(buffer);
                }
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
            instance.Input = FInputs[i];
        }
    }
}



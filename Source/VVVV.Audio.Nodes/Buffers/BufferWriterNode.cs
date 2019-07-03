#region usings
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

using VVVV.Audio;
using VVVV.Core.Logging;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

#endregion usings

namespace VVVV.Nodes
{
	
	
	[PluginInfo(Name = "BufferWriter", Category = "VAudio", Version = "Sink", Help = "Records audio into a buffer", Tags = "Scope, Samples", AutoEvaluate = true)]
	public class BufferWriterNode : IPluginEvaluate
	{
		[Input("Input")]
		public IDiffSpread<AudioSignal> FInput;
		
		[Input("Write")]
		public IDiffSpread<bool> FWriteIn;

        [Input("Write Position")]
        public IDiffSpread<int> FWritePositionIn;

        [Input("Set Write Position", IsBang = true)]
        public IDiffSpread<bool> FSetWritePositionIn;

        [Input("Clear Buffer", IsBang = true)]
        public IDiffSpread<bool> FClearIn;

        [Input("Preview Spread Count", DefaultValue = 100)]
		public IDiffSpread<int> FPreviewSizeIn;
		
		[Input("Buffer ID", EnumName = "AudioBufferStorageKeys")]
		public IDiffSpread<EnumEntry> FKeys;
		
		[Output("Buffer Preview")]
		public ISpread<ISpread<float>> FBufferPreviewOut;

        [Output("Current Write Position")]
        public ISpread<int> FCurrentWritePositionOut;

        Spread<BufferWriterSignal> FBufferWriters = new Spread<BufferWriterSignal>();
		
		[ImportingConstructor]
		public BufferWriterNode()
		{
			var bufferKeys = AudioService.BufferStorage.Keys.ToArray();
			
			if (bufferKeys.Length > 0)
			{
				EnumManager.UpdateEnum("AudioBufferStorageKeys", bufferKeys[0], bufferKeys);
			}
			else
			{
				bufferKeys = new string[]{"No Buffers Created yet"};
				EnumManager.UpdateEnum("AudioBufferStorageKeys", bufferKeys[0], bufferKeys);
			}
			
		}
		
		public void Evaluate(int SpreadMax)
		{
			if(FInput.IsChanged || FKeys.IsChanged)
			{
				
				//delete and dispose all inputs
				foreach (var element in FBufferWriters) 
				{
					if(element != null)
						element.Dispose();
				}
				
				FBufferWriters.SliceCount = SpreadMax;
				for (int i = 0; i < SpreadMax; i++)
				{
					if(FInput[i] != null)
					{
						FBufferWriters[i] = (new BufferWriterSignal(FInput[i], FKeys[i].Name, FPreviewSizeIn[i]));
					}
				}
				
				FBufferPreviewOut.SliceCount = SpreadMax;
                FCurrentWritePositionOut.SliceCount = SpreadMax;
			}

			//output value
			for (int i = 0; i < SpreadMax; i++)
			{
                var bufferWriter = FBufferWriters[i];
                if (bufferWriter != null)
				{
                    if(FSetWritePositionIn[i])
                        bufferWriter.SetWritePosition(FWritePositionIn[i]);

                    if (FClearIn[i])
                        bufferWriter.Clear();

                    bufferWriter.DoWrite = FWriteIn[i];
                    bufferWriter.PreviewSize = FPreviewSizeIn[i];

                    FCurrentWritePositionOut[i] = bufferWriter.WritePosition;
					var spread = FBufferPreviewOut[i];
					float[] val = bufferWriter.Preview;
					//FBufferReaders[i].GetLatestValue(out val);
					if(val != null)
					{
						if(spread == null)
						{
							spread = new Spread<float>(val.Length);
						}
						spread.SliceCount = val.Length;
						spread.AssignFrom(val);
					}
				}
				else
				{
					FBufferPreviewOut[i].SliceCount = 0;
				}
			}
		}
	}
}



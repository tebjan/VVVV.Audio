#region usings
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

using NAudio.CoreAudioApi;
using NAudio.Utils;
using NAudio.Wave;
using NAudio.Wave.Asio;
using NAudio.Wave.SampleProviders;
using VVVV.Audio;
using VVVV.Core.Logging;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

#endregion usings

namespace VVVV.Nodes
{
	public class BufferWriterSignal : SinkSignal<float[]>
	{
		public BufferWriterSignal(AudioSignal input, string bufferKey)
			: base(44100)
		{
			if (input == null)
				throw new ArgumentNullException("Input of BufferWriterSignal construcor is null");
			FSource = input;
			FBuffer = AudioService.BufferStorage[bufferKey];
			FBufferSize = FBuffer.Length;
		}
		
		protected string FBufferKey;
		protected int FBufferSize;
		public int WritePosition;
		protected float[] FBuffer;
		public int PreviewSize;
		protected override void FillBuffer(float[] buffer, int offset, int count)
		{
			FSource.Read(buffer, offset, count);
			if(WritePosition >= FBufferSize) WritePosition %= FBufferSize;
			Array.Copy(buffer, 0, FBuffer, WritePosition, Math.Min(FBufferSize - WritePosition, count));
			WritePosition += count;
			
			//do proper preview
			FStack.Push((float[])buffer.Clone());
		}
	}
	
	[PluginInfo(Name = "BufferWriter", Category = "Audio", Version = "Sink", Help = "Records audio into a buffer", Tags = "Scope, Samples")]
	public class BufferWriterNode : IPluginEvaluate
	{
		[Input("Input")]
		IDiffSpread<AudioSignal> FInput;
		
		[Input("Buffer Key", EnumName = "AudioBufferStorageKeys")]
		IDiffSpread<EnumEntry> FKeys;
		
		[Input("Write")]
		IDiffSpread<bool> FRead;
		
		[Output("Buffer Preview")]
		ISpread<ISpread<float>> FBufferPreviewOut;
		
		Spread<BufferWriterSignal> FBufferReaders = new Spread<BufferWriterSignal>();
		
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
			if(FInput.IsChanged)
			{
				//delete and dispose all inputs
				FBufferReaders.ResizeAndDispose(0, () => new BufferWriterSignal(FInput[0], FKeys[0].Name));
				
				FBufferReaders.SliceCount = SpreadMax;
				for (int i = 0; i < SpreadMax; i++)
				{
					if(FInput[i] != null)
					{
						if(AudioService.BufferStorage.ContainsKey(FKeys[i].Name))
							FBufferReaders[i] = (new BufferWriterSignal(FInput[i], FKeys[i].Name));
					}
					
				}
				
				FBufferPreviewOut.SliceCount = SpreadMax;
			}
			
			//output value
			for (int i = 0; i < SpreadMax; i++)
			{
				if(FBufferReaders[i] != null)
				{
					var spread = FBufferPreviewOut[i];
					float[] val = null;
					FBufferReaders[i].GetLatestValue(out val);
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



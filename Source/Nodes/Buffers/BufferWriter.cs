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
	public class BufferWriterSignal : BufferAudioSignal, IAudioSink
	{
		public BufferWriterSignal(AudioSignal input, string bufferKey, int previewSize)
			: base(bufferKey)
		{
			AudioService.AddSink(this);
			FInput = input;
			PreviewSize = previewSize;
			if (input == null)
				throw new ArgumentNullException("Input of BufferWriterSignal construcor is null");

		}
		
		public int WritePosition;
		public float[] Preview = new float[1];
		public int PreviewSize;
		public volatile bool DoRead; 
		protected override void FillBuffer(float[] buffer, int offset, int count)
		{
			
			if(DoRead)
			{
				FInput.Read(buffer, offset, count);
				if(WritePosition >= FBufferSize) WritePosition %= FBufferSize;
				
				var copyCount = Math.Min(FBufferSize - WritePosition, count);
				Array.Copy(buffer, 0, FBuffer, WritePosition, copyCount);
				
				if(copyCount < count) //copy rest to front
				{
					Array.Copy(buffer, 0, FBuffer, 0, count - copyCount);
				}
				
				WritePosition += count;
			}
			
			//do preview
			if(PreviewSize > 0)
			{
				if(Preview.Length != PreviewSize)
					Preview = new float[PreviewSize];
				var stepsize = (FBufferSize / PreviewSize) + 1;
				var index = 0;
				for (int i = stepsize/2; i < FBufferSize; i+=stepsize)
				{
					Preview[index] = FBuffer[i];
					index++;
				}
			}
		}
		
		protected float[] FInternalBuffer;
		public void Read(int offset, int count)
		{
			FInternalBuffer = BufferHelpers.Ensure(FInternalBuffer, count);
			base.Read(FInternalBuffer, offset, count);
		}
		
		public override void Dispose()
		{
			AudioService.RemoveSink(this);
			base.Dispose();
		}
	}
	
	[PluginInfo(Name = "BufferWriter", Category = "Audio", Version = "Sink", Help = "Records audio into a buffer", Tags = "Scope, Samples", AutoEvaluate = true)]
	public class BufferWriterNode : IPluginEvaluate
	{
		[Input("Input")]
		IDiffSpread<AudioSignal> FInput;
		
		[Input("Write")]
		IDiffSpread<bool> FReadIn;
		
		[Input("Preview Spread Count", DefaultValue = 100)]
		IDiffSpread<int> FPreviewSizeIn;
		
		[Input("Buffer ID", EnumName = "AudioBufferStorageKeys")]
		IDiffSpread<EnumEntry> FKeys;
		
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
			if(FInput.IsChanged || FKeys.IsChanged)
			{
				
				//delete and dispose all inputs
				foreach (var element in FBufferReaders) 
				{
					if(element != null)
						element.Dispose();
				}
				
				FBufferReaders.SliceCount = SpreadMax;
				for (int i = 0; i < SpreadMax; i++)
				{
					if(FInput[i] != null)
					{
						FBufferReaders[i] = (new BufferWriterSignal(FInput[i], FKeys[i].Name, FPreviewSizeIn[i]));
					}
				}
				
				FBufferPreviewOut.SliceCount = SpreadMax;
			}

			//output value
			for (int i = 0; i < SpreadMax; i++)
			{
				if(FBufferReaders[i] != null)
				{
					FBufferReaders[i].DoRead = FReadIn[i];
					FBufferReaders[i].PreviewSize = FPreviewSizeIn[i];
					var spread = FBufferPreviewOut[i];
					float[] val = FBufferReaders[i].Preview;
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



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
	public class BufferReaderSignal : BufferAudioSignal
	{
		public BufferReaderSignal(string bufferKey)
			: base(bufferKey)
		{
		}
		
		public bool DoRead;
		public int ReadPosition;
		public int PreviewSize;
		
		protected override void FillBuffer(float[] buffer, int offset, int count)
		{
			if(DoRead)
			{
				if(ReadPosition >= FBufferSize) ReadPosition %= FBufferSize;
				
				var copyCount = Math.Min(FBufferSize - ReadPosition, count);
				Array.Copy(FBuffer, ReadPosition, buffer, 0, copyCount);
				
				if(copyCount < count) //copy rest from front
				{
					Array.Copy(FBuffer, 0, buffer, copyCount, count - copyCount);
				}
				
				ReadPosition += count;
			}
			else
			{
				buffer.ReadSilence(offset, count);
			}
		}
	}
	
	[PluginInfo(Name = "BufferReader", Category = "Audio", Version = "Source", Help = "Reads audio from a buffer", Tags = "samples, play")]
	public class BufferReaderNode : GenericAudioSourceNodeWithOutputs<BufferReaderSignal>
	{
		[Input("Buffer ID", EnumName = "AudioBufferStorageKeys")]
		IDiffSpread<EnumEntry> FKeys;
		
		[Input("Read")]
		IDiffSpread<bool> FRead;
		
		[Input("Do Seek", IsBang = true)]
		ISpread<bool> FDoSeekIn;
		
		[Input("Seek Position")]
		IDiffSpread<int> FSeekPositionIn;
		
		[Output("Read Position")]
		ISpread<int> FReadPosition;
		
		//always evaluate parameters
		protected override bool AnyInputChanged()
		{
			return true;
		}
		
		protected override void SetParameters(int i, BufferReaderSignal instance)
		{
			if(FRead.IsChanged)
			{
				instance.DoRead = FRead[i];
			}
			
			if(FKeys.IsChanged)
			{
				instance.BufferKey = FKeys[i];
			}
			
			if(FDoSeekIn[i])
			{
				instance.ReadPosition = FSeekPositionIn[i];
			}
		}
		
		protected override void SetOutputSliceCount(int sliceCount)
		{
			FReadPosition.SliceCount = sliceCount;
		}
		
		protected override void SetOutputs(int i, BufferReaderSignal instance)
		{
			FReadPosition[i] = (OutBuffer[i] as BufferReaderSignal).ReadPosition;
		}
		
		protected override AudioSignal GetInstance(int i)
		{
			return new BufferReaderSignal(FKeys[i].Name);
		}
	}
}



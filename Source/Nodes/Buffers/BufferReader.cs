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
	public class BufferReaderSignal : AudioSignal
	{
		public BufferReaderSignal(string bufferKey)
			: base(44100)
		{
			FBuffer = AudioService.BufferStorage[bufferKey];
			FBufferSize = FBuffer.Length;
		}
		
		public bool DoRead;
		protected string FBufferKey;
		protected int FBufferSize;
		public int ReadPosition;
		protected float[] FBuffer;
		public int PreviewSize;
		
		protected override void FillBuffer(float[] buffer, int offset, int count)
		{
			if(DoRead)
			{
				if(ReadPosition >= FBufferSize) ReadPosition %= FBufferSize;
				Array.Copy(FBuffer, ReadPosition, buffer, 0, Math.Min(FBufferSize - ReadPosition, count));
				ReadPosition += count;
			}
			else
			{
				buffer.ReadSilence(offset, count);
			}
		}
	}
	
	[PluginInfo(Name = "BufferReader", Category = "Audio", Version = "Source", Help = "Reads audio from a buffer", Tags = "samples, play")]
	public class BufferReaderNode : AudioNodeBase
	{
		[Input("Buffer ID", EnumName = "AudioBufferStorageKeys")]
		IDiffSpread<EnumEntry> FKeys;
		
		[Input("Read")]
		IDiffSpread<bool> FRead;
		
		private bool FFirstFrame = true;
		public override void Evaluate(int SpreadMax)
		{
			if(FKeys.IsChanged || FFirstFrame)
			{
				OutBuffer.SliceCount = SpreadMax;
				for(int i=0; i<SpreadMax; i++)
				{
					if(OutBuffer[i] == null) OutBuffer[i] = new BufferReaderSignal(FKeys[i].Name); 
					
					(OutBuffer[i] as BufferReaderSignal).DoRead = FRead[i];
				}
			}
			
			if(FRead.IsChanged)
			{
				for(int i=0; i<SpreadMax; i++)
				{
					(OutBuffer[i] as BufferReaderSignal).DoRead = FRead[i];
				}
			}
			
			FFirstFrame = false;
		}
	}
}



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
			: base(44100)
		{
			if (input == null)
				throw new ArgumentNullException("Input of LevelMeterSignal construcor is null");
			Source = input;
		}
		
		protected override void FillBuffer(float[] buffer, int offset, int count)
		{
			FSource.Read(buffer, offset, count);
			FStack.Push((float[])buffer.Clone());
		}
	}
	
	[PluginInfo(Name = "GetBuffer", Category = "Audio", Version = "Sink", Help = "Calculates the max dBs", Tags = "Scope, Samples")]
	public class BufferOutNode : IPluginEvaluate
	{
		[Input("Input")]
		IDiffSpread<AudioSignal> FInput;
		
		[Output("Buffer")]
		ISpread<ISpread<float>> FLevelOut;
		
		Spread<BufferOutSignal> FBufferReaders = new Spread<BufferOutSignal>();
		
		public void Evaluate(int SpreadMax)
		{
			if(FInput.IsChanged)
			{
				//delete and dispose all inputs
				FBufferReaders.ResizeAndDispose(0, () => new BufferOutSignal(FInput[0]));
				
				FBufferReaders.SliceCount = SpreadMax;
				for (int i = 0; i < SpreadMax; i++)
				{
					if(FInput[i] != null)
						FBufferReaders[i] = (new BufferOutSignal(FInput[i]));
					
				}
				
				FLevelOut.SliceCount = SpreadMax;
			}
			
			//output value
			for (int i = 0; i < SpreadMax; i++)
			{
				if(FBufferReaders[i] != null)
				{
					var spread = FLevelOut[i];
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
					FLevelOut[i].SliceCount = 0;
				}
			}
		}
	}
}



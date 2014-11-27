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
	
	
	[PluginInfo(Name = "BufferReader", Category = "VAudio", Version = "Source", Help = "Reads audio from a buffer", Tags = "samples, play")]
	public class BufferReaderNode : GenericAudioSourceNode<BufferReaderSignal>
	{
		[Input("Buffer ID", EnumName = "AudioBufferStorageKeys")]
		public IDiffSpread<EnumEntry> FKeys;
		
		[Input("Read")]
		public IDiffSpread<bool> FRead;
		
		[Input("Do Seek", IsBang = true)]
		public ISpread<bool> FDoSeekIn;
		
		[Input("Seek Position")]
		public IDiffSpread<int> FSeekPositionIn;
		
		[Output("Read Position")]
		public ISpread<int> FReadPosition;
		
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
			FReadPosition[i] = (FOutputSignals[i] as BufferReaderSignal).ReadPosition;
		}

        protected override BufferReaderSignal GetInstance(int i)
		{
			return new BufferReaderSignal(FKeys[i].Name);
		}
	}
}



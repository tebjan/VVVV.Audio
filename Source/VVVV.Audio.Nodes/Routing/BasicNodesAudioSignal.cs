using System;
using VVVV.Audio;
using VVVV.Nodes.Generic;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.Streams;

namespace VVVV.Nodes
{

	//1.) do a 'replace all' of AudioSignal with the name of your own type
	
	//2.) do a 'replace all' for VAudio to set the category and the class name prefix for all nodes
	
	#region SingleValue
	
	[PluginInfo(Name = "Cast",
                Category = "VAudio",
                Help = "Casts any type to a type of this category, so be sure the input is of the required type",
                Tags = "convert, as, generic"
                )]
    public class AudioSignalCastNode : Cons<AudioSignal> {}
    
    #endregion SingleValue
    
    #region SpreadOps
	
	[PluginInfo(Name = "Cons",
                Category = "VAudio",
                Help = "Concatenates all input spreads to one output spread.",
                Tags = "generic, spreadop"
                )]
    public class AudioSignalConsNode : Cons<AudioSignal> {}
	
	[PluginInfo(Name = "CAR", 
	            Category = "VAudio",
	            Version = "Bin", 
	            Help = "Splits a given spread into first slice and remainder.", 
	            Tags = "split, generic, spreadop",
	            Author = "woei"
	           )]
	public class AudioSignalCARBinNode : CARBin<AudioSignal> {}
	
	[PluginInfo(Name = "CDR", 
	            Category = "VAudio", 
	            Version = "Bin", 
	            Help = "Splits a given spread into remainder and last slice.", 
	            Tags = "split, generic, spreadop",
	            Author = "woei"
	           )]
	public class AudioSignalCDRBinNode : CDRBin<AudioSignal> {}
	
	[PluginInfo(Name = "Reverse", 
	            Category = "VAudio", 
	            Version = "Bin",
	            Help = "Reverses the order of slices in a given spread.",
	            Tags = "invert, generic, spreadop",
	            Author = "woei"
	           )]
	public class AudioSignalReverseBinNode : ReverseBin<AudioSignal> {}

	[PluginInfo(Name = "Shift", 
	            Category = "VAudio", 
	            Version = "Bin", 
	            Help = "Shifts the slices in a spread upwards by the given phase.", 
	            Tags = "generic, spreadop",
	            Author = "woei"
	           )]
	public class AudioSignalShiftBinNode : ShiftBin<AudioSignal> {}
	
	[PluginInfo(Name = "SetSlice",
	            Category = "VAudio",
	            Version = "Bin",
	            Help = "Replaces individual slices of a spread with the given input",
	            Tags = "generic, spreadop",
	            Author = "woei"
	           )]
	public class AudioSignalSetSliceNode : SetSlice<AudioSignal> {}
    
	[PluginInfo(Name = "DeleteSlice",
	            Category = "VAudio",
	            Help = "Deletes the slice at the given index.",
	            Tags = "remove, generic, spreadop",
	            Author = "woei"
	           )]
	public class AudioSignalDeleteSliceNode : DeleteSlice<AudioSignal> {}
	
	[PluginInfo(Name = "Select",
                Category = "VAudio",
                Help = "Select which slices and how many form the output spread.",
	            Tags = "resample, generic, spreadop"
	           )]
    public class AudioSignalSelectNode : Select<AudioSignal> {}
    
    [PluginInfo(Name = "Select", 
				Category = "VAudio",
				Version = "Bin",				
				Help = "Select the slices which form the new spread.", 
				Tags = "repeat, generic, spreadop",
				Author = "woei"
			)]
    public class AudioSignalSelectBinNode : SelectBin<AudioSignal> {}
    
	[PluginInfo(Name = "Unzip", 
	            Category = "VAudio",
	            Help = "Unzips a spread into multiple spreads.", 
	            Tags = "split, generic, spreadop"
	           )]
	public class AudioSignalUnzipNode : Unzip<AudioSignal> {}
	
	[PluginInfo(Name = "Unzip", 
	            Category = "VAudio",
	            Version = "Bin",
	            Help = "Unzips a spread into multiple spreads.", 
	            Tags = "split, generic, spreadop"
	           )]
	public class AudioSignalUnzipBinNode : Unzip<IInStream<AudioSignal>> {}
	
	[PluginInfo(Name = "Zip", 
	            Category = "VAudio",
	            Help = "Zips spreads together.", 
	            Tags = "join, generic, spreadop"
	           )]
	public class AudioSignalZipNode : Zip<AudioSignal> {}
	
	[PluginInfo(Name = "Zip", 
	            Category = "VAudio",
				Version = "Bin",	            
	            Help = "Zips spreads together.", 
	            Tags = "join, generic, spreadop"
	           )]
	public class AudioSignalZipBinNode : Zip<IInStream<AudioSignal>> {}
	
    [PluginInfo(Name = "GetSpread",
                Category = "VAudio",
                Version = "Bin",
                Help = "Returns sub-spreads from the input specified via offset and count",
                Tags = "generic, spreadop",
                Author = "woei")]
    public class AudioSignalGetSpreadNode : GetSpreadAdvanced<AudioSignal> {}
    
	[PluginInfo(Name = "SetSpread",
	            Category = "VAudio",
	            Version = "Bin",
	            Help = "Allows to set sub-spreads into a given spread.",
	            Tags = "generic, spreadop",
	            Author = "woei"
	           )]
	public class AudioSignalSetSpreadNode : SetSpread<AudioSignal> {}
    
    [PluginInfo(Name = "Pairwise",
                Category = "VAudio",
                Help = "Returns all pairs of successive slices. From an input ABCD returns AB, BC, CD.",
                Tags = "generic, spreadop"
                )]
    public class AudioSignalPairwiseNode : Pairwise<AudioSignal> {}

    [PluginInfo(Name = "SplitAt",
                Category = "VAudio",
                Help = "Splits a spread at the given index.",
                Tags = "generic, spreadop"
                )]
    public class AudioSignalSplitAtNode : SplitAtNode<AudioSignal> { }
    
   	#endregion SpreadOps

    #region Collections
    
    [PluginInfo(Name = "Buffer",
	            Category = "VAudio",
	            Help = "Inserts the input at the given index.",
	            Tags = "generic, spreadop, collection",
	            AutoEvaluate = true
	           )]
	public class AudioSignalBufferNode : BufferNode<AudioSignal> {}
    
    [PluginInfo(Name = "Queue",
	            Category = "VAudio",
	            Help = "Inserts the input at index 0 and drops the oldest slice in a FIFO (First In First Out) fashion.",
	            Tags = "generic, spreadop, collection",
	            AutoEvaluate = true
	           )]
	public class AudioSignalQueueNode : QueueNode<AudioSignal> {}
	
	[PluginInfo(Name = "RingBuffer",
	            Category = "VAudio",
	            Help = "Inserts the input at the ringbuffer position.",
	            Tags = "generic, spreadop, collection",
	            AutoEvaluate = true
	           )]
	public class AudioSignalRingBufferNode : RingBufferNode<AudioSignal> {}
    
	[PluginInfo(Name = "Store", 
	            Category = "VAudio", 
	            Help = "Stores a spread and sets/removes/inserts slices.", 
	            Tags = "add, insert, remove, generic, spreadop, collection",
	            Author = "woei", 
	            AutoEvaluate = true
	           )]
	public class AudioSignalStoreNode: Store<AudioSignal> {}
	
	[PluginInfo(Name = "Stack",
				Category = "VAudio",
				Help = "Stack data structure implementation using the LIFO (Last In First Out) paradigm.",
				Tags = "generic, spreadop, collection",
				Author="vux"
				)]
	public class AudioSignalStackNode : StackNode<AudioSignal> {}
	
	#endregion Collections
	
}


#region usings
using System;
using System.ComponentModel.Composition;
using System.Collections.Generic;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;
using VVVV.Audio;
using VVVV.Nodes;
using VVVV.Nodes.Generic;

using NAudio.Wave;
using NAudio.Wave.Asio;
using NAudio.CoreAudioApi;
using NAudio.Wave.SampleProviders;
using NAudio.Utils;


using VVVV.Core.Logging;
#endregion usings

namespace VVVV.Nodes
{
	[PluginInfo(Name = "Cons",
	            Category = "VAudio",
	            Help = "Concatenates all input audio signals to one output spread",
	            Tags = ""
	           )]
	public class AudioCons : Cons<AudioSignal>
	{
	}
	
	[PluginInfo(Name = "Zip", 
	            Category = "VAudio", 
	            Help = "Zips spreads together", 
	            Tags = "spread, join")]
	public class AudioZipNode : Zip<AudioSignal>
	{
	}
	
	[PluginInfo(Name = "Unzip", 
	            Category = "VAudio", 
	            Help = "Unzips a spread into multiple spreads", 
	            Tags = "spread, split")]
	public class AudioUnzipNode : Unzip<AudioSignal>
	{
	}
	
	[PluginInfo(Name = "SetSlice",
	            Category = "VAudio",
	            Help = "Replace individual slices of the audio spread with the given input",
	            Tags = "replace",
	            Author = "woei")]
	public class AudioSetSlice : SetSlice<AudioSignal> 
	{
	}
	
	[PluginInfo(Name = "DeleteSlice",
	            Category = "VAudio",
	            Help = "Delete the slice at the given index",
	            Tags = "remove, filter",
	            Author = "woei")]
	public class AudioDeleteSlice : DeleteSlice<AudioSignal> 
	{
	}
	
	[PluginInfo(Name = "Pairwise",
	            Category = "VAudio",
	            Help = "Returns all combinations of successive slices. From an input ABCD returns AB, BC, CD",
	            Tags = ""
	           )]
	public class AudioPairwise : Pairwise<AudioSignal>
	{
	}
	
//	[PluginInfo(Name = "Select", 
//	            Category = "VAudio",
//	            Help = "Select which slices and how many form the output spread",
//	            Tags = "resample")]
//    public class AudioSelectNode : SelectNode<AudioSignal>
//    { 
//    }
}



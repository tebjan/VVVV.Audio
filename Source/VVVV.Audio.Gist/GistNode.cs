/*
 * Created by SharpDevelop.
 * User: TF
 * Date: 24.06.2015
 * Time: 02:39
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
 
using System;
using VVVV.Audio;
using VVVV.PluginInterfaces.V2;
 
 namespace VVVV.Nodes
 {
    [PluginInfo(Name = "Gist", Category = "VAudio", Version = "Sink", Help = "Tracks several features of the incoming audio", AutoEvaluate = true, Tags = "Analysis, FFT, ", Credits = "Adam Stark" )]
	public class GistNode : AutoAudioSinkSignalNode<GistSignal>
	{
	    
	}
 }


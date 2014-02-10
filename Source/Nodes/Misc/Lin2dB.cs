#region usings
using System;
using System.ComponentModel.Composition;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using VVVV.Core.Logging;
using NAudio.Utils;
#endregion usings

namespace VVVV.Nodes
{
	#region PluginInfo
	[PluginInfo(Name = "Lin2dB",
	            Category = "Value",
	            Help = "Converts linear amplitude values in the range [0..1] to dBfs",
	            Tags = "VAudio, convert")]
	#endregion PluginInfo
	public class Lin2dBNode : IPluginEvaluate
	{
		#region fields & pins
		[Input("Input", DefaultValue = 0.5)]
        public ISpread<double> FInput;

		[Output("Output")]
        public ISpread<double> FOutput;

		[Import()]
        public ILogger FLogger;
		#endregion fields & pins
 
		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{ 
			FOutput.SliceCount = SpreadMax;

			for (int i = 0; i < SpreadMax; i++)
				FOutput[i] = Decibels.LinearToDecibels(FInput[i]);
				 
			//FLogger.Log(LogType.Debug, "hi tty!");
		}
	}
	
		#region PluginInfo
	[PluginInfo(Name = "dB2Lin",
	            Category = "Value",
	            Help = "Converts dBfs values to linear amplitude values in the range [0..1]",
	            Tags = "VAudio, convert")]
	#endregion PluginInfo
	public class dB2LinNode : IPluginEvaluate
	{
		#region fields & pins
		[Input("Input", DefaultValue = -6)]
        public ISpread<double> FInput;

		[Output("Output")]
        public ISpread<double> FOutput;

		[Import()]
        public ILogger FLogger;
		#endregion fields & pins
 
		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{ 
			FOutput.SliceCount = SpreadMax;

			for (int i = 0; i < SpreadMax; i++)
				FOutput[i] = Decibels.DecibelsToLinear(FInput[i]);
				 
			//FLogger.Log(LogType.Debug, "hi tty!");
		}
	}
}

#region usings
using System;
using System.ComponentModel.Composition;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using VVVV.Core.Logging;
#endregion usings

namespace VVVV.Nodes
{
	#region PluginInfo
	[PluginInfo(Name = "PolyToggle", Category = "Value", Help = "Basic template with one value in/out", Tags = "")]
	#endregion PluginInfo
	public class ValuePolyToggleNode : IPluginEvaluate
	{
		#region fields & pins
		[Input("Input")]
		ISpread<bool> FInput;
		
		[Input("Count", DefaultValue = 8)]
		ISpread<int> FCount;		

		[Output("Output")]
		ISpread<bool> FOutput;
		
		int FIndex = 0;

		[Import()]
		ILogger FLogger;
		#endregion fields & pins

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			FOutput.SliceCount = FCount[0];

			for (int i = 0; i < FCount[0]; i++)
				FOutput[i] = false;
			
			foreach( bool slice in FInput)
			{
				if(slice == true)
				{
					FOutput[FIndex] = true;
					FIndex = (FIndex + 1) % FCount[0];					
				}

			}


			//FLogger.Log(LogType.Debug, "hi tty!");
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LTCSharp;
using VVVV.PluginInterfaces.V2;

namespace VVVV.Nodes.LTC
{
	#region PluginInfo
	[PluginInfo(Name = "Split", Category = "Timecode", Help = "Split Timecode into parts", Tags = "", Author = "elliotwoods", AutoEvaluate = true)]
	#endregion PluginInfo
	public class TimecodeSplit : IPluginEvaluate
	{
		[Input("Input")]
		IDiffSpread<Timecode> FInTimecode;

		[Output("Time Zone", Visibility = PinVisibility.OnlyInspector)]
		ISpread<string> FOutTimeZone;

		[Output("Year", Visibility = PinVisibility.OnlyInspector)]
		ISpread<int> FOutYear;

		[Output("Month", Visibility = PinVisibility.OnlyInspector)]
		ISpread<int> FOutMonth;

		[Output("Day", Visibility = PinVisibility.OnlyInspector)]
		ISpread<int> FOutDay;

		[Output("Hours")]
		ISpread<int> FOutHours;

		[Output("Minutes")]
		ISpread<int> FOutMinutes;

		[Output("Seconds")]
		ISpread<int> FOutSeconds;

		[Output("Frame")]
		ISpread<int> FOutFrame;

		public void Evaluate(int SpreadMax)
		{
			if (FInTimecode.IsChanged)
			{
				FOutTimeZone.SliceCount = SpreadMax;
				FOutYear.SliceCount = SpreadMax;
				FOutMonth.SliceCount = SpreadMax;
				FOutDay.SliceCount = SpreadMax;
				FOutHours.SliceCount = SpreadMax;
				FOutMinutes.SliceCount = SpreadMax;
				FOutSeconds.SliceCount = SpreadMax;
				FOutFrame.SliceCount = SpreadMax;

				for (int i = 0; i < SpreadMax; i++)
				{
					var timecode = FInTimecode[i];

					if (timecode == null)
					{
						FOutTimeZone[i] = "";
						FOutYear[i] = 0;
						FOutMonth[i] = 0;
						FOutDay[i] = 0;
						FOutHours[i] = 0;
						FOutMinutes[i] = 0;
						FOutSeconds[i] = 0;
						FOutFrame[i] = 0;
					}
					else
					{
						FOutTimeZone[i] = timecode.TimeZone;
						FOutYear[i] = timecode.Years;
						FOutMonth[i] = timecode.Months;
						FOutDay[i] = timecode.Days;
						FOutHours[i] = timecode.Hours;
						FOutMinutes[i] = timecode.Minutes;
						FOutSeconds[i] = timecode.Seconds;
						FOutFrame[i] = timecode.Frame;
					}
				}
			}
		}
	}
}

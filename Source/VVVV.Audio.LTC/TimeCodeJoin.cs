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
    [PluginInfo(Name = "Join", 
                Category = "Timecode", 
                Help = "Join Timecode from parts", 
                Tags = "", 
                Author = "sebl")]
    #endregion PluginInfo
    public class TimecodeJoin : IPluginEvaluate
    {
        #pragma warning disable 649
        [Input("Time Zone", Visibility = PinVisibility.OnlyInspector)]
        IDiffSpread<string> FInTimeZone;

        [Input("Year", Visibility = PinVisibility.OnlyInspector)]
        IDiffSpread<int> FInYear;

        [Input("Month", Visibility = PinVisibility.OnlyInspector)]
        IDiffSpread<int> FInMonth;

        [Input("Day", Visibility = PinVisibility.OnlyInspector)]
        IDiffSpread<int> FInDay;

        [Input("Hours")]
        IDiffSpread<int> FInHours;

        [Input("Minutes")]
        IDiffSpread<int> FInMinutes;

        [Input("Seconds")]
        ISpread<int> FInSeconds;

        [Input("Frame")]
        ISpread<int> FInFrame;

        [Output("Timecode")]
        ISpread<Timecode> FOutTimeCode;
        #pragma warning restore


        Spread<Timecode> timecodes = new Spread<Timecode>(0);

        public void Evaluate(int SpreadMax)
        {
            if (FInTimeZone.IsChanged ||
                FInYear.IsChanged ||
                FInMonth.IsChanged ||
                FInDay.IsChanged ||
                FInHours.IsChanged ||
                FInMinutes.IsChanged ||
                FInSeconds.IsChanged ||
                FInFrame.IsChanged)
            {
                FOutTimeCode.SliceCount = SpreadMax;


                for (int i = 0; i < SpreadMax; i++)
                {
                    FOutTimeCode[i] = new Timecode(
                        FInTimeZone[i],
                        FInYear[i],
                        FInMonth[i],
                        FInDay[i],
                        FInHours[i],
                        FInMinutes[i],
                        FInSeconds[i],
                        FInFrame[i]);
                }
            }
        }
    }
}

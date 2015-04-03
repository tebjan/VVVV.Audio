/*
 * Created by SharpDevelop.
 * User: TF
 * Date: 03.04.2015
 * Time: 03:57
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using VVVV.PluginInterfaces.V2;
using System.ComponentModel.Composition;
using VVVV.Audio.MIDI;

namespace VVVV.Nodes
{

    [PluginInfo(Name = "MidiRaw", Category = "VAudio", Version = "Source", Help = "Sends midi events with raw bytes", Tags = "MidiShort", Author = "tonfilm")]
    public class MidiRawNode : IPluginEvaluate, IPartImportsSatisfiedNotification
    {
        [Input("Do Send", IsBang = true)]
        ISpread<ISpread<bool>> FDoSendIn;
        
        [Input("Status")]
        ISpread<ISpread<int>> FMessageIn;
        
        [Input("Data 1")]
        ISpread<ISpread<int>> FData1In;
        
        [Input("Data 2")]
        ISpread<ISpread<int>> FData2In;
        
        [Output("Events")]
        ISpread<MidiEvents> FEventsOut;

        public void OnImportsSatisfied()
        {
            FEventsOut.SliceCount = 0;
        }

        public void Evaluate(int SpreadMax)
        {
            SpreadMax = FDoSendIn.CombineWith(FMessageIn).CombineWith(FData1In).CombineWith(FData2In);
            
            FEventsOut.ResizeAndDispose(SpreadMax, () => new ManualMidiEvents());
            
            for (int i = 0; i < SpreadMax; i++)
            {               
                var eventSender = FEventsOut[i] as ManualMidiEvents;
                var doSends = FDoSendIn[i];
                var messages = FMessageIn[i];
                var data1s = FData1In[i];
                var data2s = FData2In[i];
                
                var max = doSends.CombineWith(messages).CombineWith(data1s).CombineWith(data2s);
                
                //send all events
                for (int j = 0; j < max; j++)
                {
                    if(doSends[j])
                    {
                        eventSender.SendRawMessage((byte)messages[j], (byte)data1s[j], (byte)data2s[j]);
                    }
                }
            }
        }
    }
}

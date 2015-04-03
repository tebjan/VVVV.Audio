/*
 * Created by SharpDevelop.
 * User: TF
 * Date: 03.04.2015
 * Time: 04:36
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using Sanford.Multimedia.Midi;

namespace VVVV.Audio.MIDI
{
    /// <summary>
    /// Description of MergeMidiEvents.
    /// </summary>
    public class MergeMidiEvents : MidiEvents
    {
        public event EventHandler<RawMessageEventArgs> RawMessageReceived
        {
            add
            {
                foreach (var elem in FMidiEventsList) 
                {
                    elem.RawMessageReceived += value;
                }
            }
            remove
            {
                foreach (var elem in FMidiEventsList) 
                {
                    elem.RawMessageReceived -= value;
                }
            }
        }

        public event EventHandler<ChannelMessageEventArgs> ChannelMessageReceived
        {
            add
            {
                foreach (var elem in FMidiEventsList)
                {
                    elem.ChannelMessageReceived += value;
                }
            }
            remove
            {
                foreach (var elem in FMidiEventsList)
                {
                    elem.ChannelMessageReceived -= value;
                }
            }
        }

        public event EventHandler<SysExMessageEventArgs> SysExMessageReceived
        {
            add
            {
                foreach (var elem in FMidiEventsList) 
                {
                    elem.SysExMessageReceived += value;
                }
            }
            remove
            {
                foreach (var elem in FMidiEventsList) 
                {
                    elem.SysExMessageReceived -= value;
                }
            }
        }

        public event EventHandler<SysCommonMessageEventArgs> SysCommonMessageReceived
        {
            add
            {
                foreach (var elem in FMidiEventsList) 
                {
                    elem.SysCommonMessageReceived += value;
                }
            }
            remove
            {
                foreach (var elem in FMidiEventsList) 
                {
                    elem.SysCommonMessageReceived -= value;
                }
            }
        }

        public event EventHandler<SysRealtimeMessageEventArgs> SysRealtimeMessageReceived
        {
            add
            {
                foreach (var elem in FMidiEventsList) 
                {
                    elem.SysRealtimeMessageReceived += value;
                }
            }
            remove
            {
                foreach (var elem in FMidiEventsList) 
                {
                    elem.SysRealtimeMessageReceived -= value;
                }
            }
        }


        public int DeviceID
        {
            get
            {
                return -3;
            }
        }
        
        List<MidiEvents> FMidiEventsList = new List<MidiEvents>();
        
        public MergeMidiEvents(IEnumerable<MidiEvents> midiEvents)
        {
            foreach (var elem in midiEvents) 
            {
                if(elem != null)
                    FMidiEventsList.Add(elem);
            }
        }

        public void Dispose()
        {
        }
    }
}

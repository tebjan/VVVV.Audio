/*
 * Created by SharpDevelop.
 * User: TF
 * Date: 03.04.2015
 * Time: 03:40
 * 
 */
using System;
using Sanford.Multimedia.Midi;

namespace VVVV.Audio.MIDI
{
    public class ManualMidiEvents : MidiEvents
    {
        public event EventHandler<ChannelMessageEventArgs> ChannelMessageReceived;
        public event EventHandler<SysExMessageEventArgs> SysExMessageReceived;
        public event EventHandler<SysCommonMessageEventArgs> SysCommonMessageReceived;
        public event EventHandler<SysRealtimeMessageEventArgs> SysRealtimeMessageReceived;
        public event MidiMessageEventHandler MessageReceived;
        public event EventHandler<ShortMessageEventArgs> ShortMessageReceived;

        public int DeviceID
        {
            get
            {
                return -2;
            }
        }

        public void Dispose()
        {
        }
        
        public void SendRawMessage(byte status, byte data1, byte data2)
        {
            ShortMessageReceived?.Invoke(this, new ShortMessageEventArgs(status, data1, data2));
        } 

        public void SendRawMessage(int sampleOffset, byte status, byte data1, byte data2)
        {
            var handler = ShortMessageReceived;
            if(handler != null)
            {
                var args = new ShortMessageEventArgs(status, data1, data2);
                args.Message.DeltaFrames = sampleOffset;
                handler(this, args);
            }
        }   

        public void SendChannelMessage(ChannelCommand command, int midiChannel, byte data1, byte data2)
        {
            ChannelMessageReceived?.Invoke(this, new ChannelMessageEventArgs(new ChannelMessage(command, midiChannel, data1, data2)));
        }

        public void SendChannelMessage(ChannelCommand command, int midiChannel, byte data1)
        {
            ChannelMessageReceived?.Invoke(this, new ChannelMessageEventArgs(new ChannelMessage(command, midiChannel, data1)));
        }
    }
}
/*
 * Created by SharpDevelop.
 * User: TF
 * Date: 19.01.2015
 * Time: 17:55
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using Sanford.Multimedia.Midi;

namespace VVVV.Audio.MIDI
{
    
    /// <summary>
    /// MidiSignal provides all midi events
    /// </summary>
    public class MidiEvents
    {
        public event EventHandler<byte[]> RawMessageReceived
        {
            add
            {
                FInDevice.RawMessageReceived += value;
            }
            
            remove
            {
                FInDevice.RawMessageReceived -= value;
            }
        }
        
        public event EventHandler<ChannelMessageEventArgs> ChannelMessageReceived
        {
            add
            {
                FInDevice.ChannelMessageReceived += value;
            }
            
            remove
            {
                FInDevice.ChannelMessageReceived -= value;
            }
        }

        public event EventHandler<SysExMessageEventArgs> SysExMessageReceived
        {
            add
            {
                FInDevice.SysExMessageReceived += value;
            }
            
            remove
            {
                FInDevice.SysExMessageReceived -= value;
            }
        }

        public event EventHandler<SysCommonMessageEventArgs> SysCommonMessageReceived
        {
            add
            {
                FInDevice.SysCommonMessageReceived += value;
            }
            
            remove
            {
                FInDevice.SysCommonMessageReceived -= value;
            }
        }

        public event EventHandler<SysRealtimeMessageEventArgs> SysRealtimeMessageReceived
        {
            add
            {
                FInDevice.SysRealtimeMessageReceived += value;
            }
            
            remove
            {
                FInDevice.SysRealtimeMessageReceived -= value;
            }
        }
        
        readonly InputDevice FInDevice;
        
        /// <summary>
        /// Create Midisignal with an input device which fires the events
        /// </summary>
        /// <param name="inDevice"></param>
        public MidiEvents(InputDevice inDevice)
        {
            FInDevice = inDevice;
        }
    }
    
    
}

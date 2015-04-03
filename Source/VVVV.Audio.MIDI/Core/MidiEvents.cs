/*
 * Created by SharpDevelop.
 * User: TF
 * Date: 19.01.2015
 * Time: 17:55
 * 
 */
using System;
using Sanford.Multimedia.Midi;

namespace VVVV.Audio.MIDI
{
    
    /// <summary>
    /// MidiSignal provides all midi events
    /// </summary>
    public interface MidiEvents : IDisposable
    {  
        int DeviceID { get; }
        
        /// <summary>
        /// All incoming midi messages in byte format
        /// </summary>
        event EventHandler<RawMessageEventArgs> RawMessageReceived;
        
        /// <summary>
        /// Channel messages like, note, controller, program, ...
        /// </summary>
        event EventHandler<ChannelMessageEventArgs> ChannelMessageReceived;
       
        /// <summary>
        /// SysEx messages
        /// </summary>
        event EventHandler<SysExMessageEventArgs> SysExMessageReceived;
        
        /// <summary>
        /// Midi timecode, song position, song select, tune request
        /// </summary>
        event EventHandler<SysCommonMessageEventArgs> SysCommonMessageReceived;

        /// <summary>
        /// Timing events, midi clock, start, stop, reset, active sense, tick
        /// </summary>
        event EventHandler<SysRealtimeMessageEventArgs> SysRealtimeMessageReceived;
    }
    
    
    
    
}

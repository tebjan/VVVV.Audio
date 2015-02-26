/*
 * Created by SharpDevelop.
 * User: TF
 * Date: 21.12.2014
 * Time: 20:49
 * 
 */
 
using System;
using VVVV.PluginInterfaces.V2;
using Sanford.Multimedia.Midi;
using System.ComponentModel.Composition;
using VVVV.Audio.MIDI;

namespace VVVV.Nodes
{
    
    [PluginInfo(Name = "MidiIn", Category = "VAudio", Version = "Source", Help = "Opens a Midi driver and outputs its events", Tags = "", Author = "tonfilm")]
    public class MidiInNode : IPluginEvaluate
    {

        [Input("Driver", EnumName = "VAudioMidiDevice", IsSingle = true)]
        IDiffSpread<EnumEntry> FDriverIn;
        
        [Output("Events", EnumName = "VAudioMidiDevice", IsSingle = true)]
        ISpread<MidiEvents> FEventsOut;

        [ImportingConstructor]
        public MidiInNode()
		{

			var drivers = GetDriverNames();
			
			if (drivers.Length > 0)
			{
				EnumManager.UpdateEnum("VAudioMidiDevice", drivers[0], drivers);
			}
			else
			{
				drivers = new string[]{"No ASIO!? -> go download ASIO4All"};
				EnumManager.UpdateEnum("VAudioMidiDevice", drivers[0], drivers);
			}
		}

        private string[] GetDriverNames()
        {
            var drivers = new string[InputDevice.DeviceCount];
            if (InputDevice.DeviceCount > 0)
            {
                for (int i = 0; i < InputDevice.DeviceCount; i++)
                {
                    drivers[i] = InputDevice.GetDeviceCapabilities(i).name;
                }
            }

            return drivers;
        }

        public void Evaluate(int SpreadMax)
        {
            throw new NotImplementedException();
        }
    }
}

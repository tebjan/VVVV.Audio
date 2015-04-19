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
    public class MidiInNode : IPluginEvaluate, IDisposable
    {

        [Input("Driver", EnumName = "VAudioMidiDevice", IsSingle = true)]
        IDiffSpread<EnumEntry> FDriverIn;
        
        [Input("Rescan", IsBang = true, IsSingle = true)]
        ISpread<bool> FRescanIn;
        
        [Output("Events", IsSingle = true)]
        ISpread<MidiEvents> FEventsOut;

        [ImportingConstructor]
        public MidiInNode()
		{
            RefreshDrivers();			
		}
        
        void RefreshDrivers()
        {
            var drivers = GetDriverNames();
			
			if (drivers.Length > 0)
			{
				EnumManager.UpdateEnum("VAudioMidiDevice", drivers[0], drivers);
			}
			else
			{
				drivers = new string[]{"No Midi Devices Found"};
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
            if(FRescanIn[0])
            {
                RefreshDrivers();
            }
            
            if(FDriverIn.IsChanged)
            {
                FEventsOut.SliceCount = SpreadMax;
                for (int i = 0; i < SpreadMax; i++) 
                {
                    var oldDevice = FEventsOut[i];
                    var newDeviceID = FDriverIn[i].Index;
                    
                    if(oldDevice == null)
                    {
                        FEventsOut[i] = InputDeviceMidiEvents.FromDeviceID(newDeviceID);
                    }
                    else
                    {
                        if(oldDevice.DeviceID != newDeviceID)
                        {
                            FEventsOut[i] = InputDeviceMidiEvents.FromDeviceID(newDeviceID);
                            oldDevice.Dispose();
                        }
                    }
                }
            }
        }


        public void Dispose()
        {
            foreach (var element in FEventsOut) 
            {
                if(element != null)
                    element.Dispose();
            }
        }

    }
}

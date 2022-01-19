#region usings
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

using NAudio.Wave;
using VVVV.Audio;
using VVVV.Core.Logging;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

#endregion usings

namespace VVVV.Nodes
{
    public enum AudioSampleRate
    {
        Hz8000 = 8000,
        Hz11025 = 11025,
        Hz16000 = 16000,
        Hz22050 = 22050,
        Hz32000 = 32000,
        Hz44056	= 44056,
        Hz44100 = 44100,
        Hz48000 = 48000,
        Hz88200 = 88200,
        Hz96000 = 96000,
        Hz176400 = 176400,
        Hz192000 = 192000,
        Hz352800 = 352800
        
    }
    
    [PluginInfo(Name = "AudioEngine", Category = "VAudio", Help = "Configures the audio engine", AutoEvaluate = true, Tags = "Asio")]
    public class AudioEngineNode : IPluginEvaluate, IDisposable
    {
        #region fields & pins
        #pragma warning disable 0649
        [Input("Play", DefaultValue = 0, IsSingle = true)]
        IDiffSpread<bool> FPlayIn;
        
        [Input("BPM", DefaultValue = 120, IsSingle = true)]
        IDiffSpread<double> FBPMIn;
        
        [Input("Loop", IsSingle = true)]
        public IDiffSpread<bool> FLoop;
        
        [Input("Loop Start Beat", IsSingle = true)]
        public IDiffSpread<double> FLoopStartBeat;
        
        [Input("Loop End Beat", IsSingle = true)]
        public IDiffSpread<double> FLoopEndBeat;
        
        [Input("Do Seek", IsBang = true, IsSingle = true)]
        ISpread<bool> FDoSeek;
        
        [Input("Seek Beat", IsSingle = true)]
        ISpread<double> FSeekBeat;
        
        [Input("Driver", EnumName = "NAudioASIO", IsSingle = true)]
        IDiffSpread<EnumEntry> FDriverIn;
        
        [Input("Sample Rate", EnumName = "ASIODriverSampleRates", DefaultEnumEntry = "44100", IsSingle = true)]
        IDiffSpread<EnumEntry> FSamplingRateIn;
        
        [Input("Desired Input Channels", DefaultValue = 2, IsSingle = true)]
        IDiffSpread<int> FInputChannelsIn;
        
        [Input("Input Channel Offset", DefaultValue = 0, Visibility = PinVisibility.OnlyInspector, IsSingle = true)]
        IDiffSpread<int> FInputChannelOffsetIn;
        
        [Input("Desired Output Channels", DefaultValue = 2, IsSingle = true)]
        IDiffSpread<int> FOutputChannelsIn;
        
        [Input("Output Channel Offset", DefaultValue = 0, Visibility = PinVisibility.OnlyInspector, IsSingle = true)]
        IDiffSpread<int> FOutputChannelOffsetIn;
        
        [Input("Control Panel", IsBang = true, IsSingle = true)]
        IDiffSpread<bool> FShowPanelIn;
        
        [Output("Time")]
        ISpread<double> FTime;
        
        [Output("Beat")]
        ISpread<double> FBeat;
        
        [Output("Buffer Size")]
        ISpread<int> FBufferSizeOut;
        
        [Output("Driver Input Chanels")]
        ISpread<int> FInputChannelsOut;
        
        [Output("Driver Output Chanels")]
        ISpread<int> FOutputChannelsOut;
        
        [Output("Open Input Chanels")]
        ISpread<int> FOpenInputChannelsOut;
        
        [Output("Open Output Chanels")]
        ISpread<int> FOpenOutputChannelsOut;
    
        [Import()]
        ILogger FLogger;
        AudioEngine FEngine;
        #pragma warning restore
        #endregion fields & pins	
        
        [ImportingConstructor]
        public AudioEngineNode()
        {
            FEngine = AudioService.Engine;
            
            var drivers = AsioOut.GetDriverNames();
            
            if (drivers.Length > 0)
            {
                EnumManager.UpdateEnum("NAudioASIO", drivers[0], drivers);
            }
            else
            {
                drivers = new string[]{"No ASIO!? -> go download ASIO4All"};
                EnumManager.UpdateEnum("NAudioASIO", drivers[0], drivers);
            }

            //also add a default entry to the sampling rate enum
            var samplingRates = new string[] { "44100" };
            EnumManager.UpdateEnum("ASIODriverSampleRates", samplingRates[0], samplingRates);
            
        }

        private void UpdateSampleRateEnum()
        {
            var tempList = new List<string>();

            foreach (var item in Enum.GetValues(typeof(AudioSampleRate)))
            {
                if (FEngine.AsioOut.IsSampleRateSupported((int)item))
                {
                    tempList.Add(((int)item).ToString());
                }
            }

            var samplingRates = tempList.ToArray();
            
            if (samplingRates.Length > 0)
            {
                var defaultIndex = tempList.IndexOf("44100");
                
                if(defaultIndex < 0)
                    defaultIndex = 0;
                
                EnumManager.UpdateEnum("ASIODriverSampleRates", samplingRates[defaultIndex], samplingRates);
            }
            else
            {
                samplingRates = new string[]{"Could not obtain sampling rates from driver"};
                EnumManager.UpdateEnum("ASIODriverSampleRates", samplingRates[0], samplingRates);
            }
        }
        
        //called when data for any output pin is requested
        public void Evaluate(int SpreadMax)
        {
            if(FDriverIn.IsChanged || FSamplingRateIn.IsChanged ||
               FInputChannelsIn.IsChanged || FInputChannelOffsetIn.IsChanged ||
               FOutputChannelsIn.IsChanged || FOutputChannelOffsetIn.IsChanged || FEngine.NeedsReset)
            {
                FEngine.ChangeDriver(FDriverIn[0].Name, FInputChannelOffsetIn[0], FOutputChannelOffsetIn[0]);
                FEngine.ChangeDriverSettings(int.Parse(FSamplingRateIn[0]), FInputChannelsIn[0], FOutputChannelsIn[0]);
                
                FEngine.Play = FPlayIn[0];
                FInputChannelsOut[0] = FEngine.AsioOut.DriverInputChannelCount;
                FOutputChannelsOut[0] = FEngine.AsioOut.DriverOutputChannelCount;
                FOpenInputChannelsOut[0] = FEngine.AsioOut.NumberOfInputChannels;
                FOpenOutputChannelsOut[0] = FEngine.AsioOut.NumberOfOutputChannels;
                
                FBufferSizeOut[0] = FEngine.Settings.BufferSize;
                UpdateSampleRateEnum();
            }
            
            if(FShowPanelIn[0])
            {
                FEngine.AsioOut.ShowControlPanel();
            }
            
            if(FPlayIn.IsChanged)
            {
                FEngine.Play = FPlayIn[0];
            }
            
            if(FBPMIn.IsChanged)
            {
                FEngine.Timer.BPM =FBPMIn[0];
            }
            
            if(FLoop.IsChanged)
            {
                FEngine.Timer.Loop = FLoop[0];
            }
            
            if(FLoopStartBeat.IsChanged)
            {
                FEngine.Timer.LoopStartBeat = FLoopStartBeat[0];
            }
            
            if(FLoopEndBeat.IsChanged)
            {
                FEngine.Timer.LoopEndBeat = FLoopEndBeat[0];
            }
            
            if(FDoSeek[0])
            {
                FEngine.Timer.Beat = FSeekBeat[0];
            }
            
            FTime[0] = FEngine.Timer.Time;
            FBeat[0] = FEngine.Timer.Beat;
        }
        
        //HACK: coupled lifetime of engine to this node
        public void Dispose()
        {
            AudioService.DisposeEngine();
        }
    
    }
}



#region usings
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

using Jacobi.Vst.Core;
using Jacobi.Vst.Framework;
using Jacobi.Vst.Interop.Host;

using Sanford.Multimedia.Midi;
using VVVV.Audio;
using VVVV.Audio.MIDI;
using VVVV.Core.Logging;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.PluginInterfaces.V2.Graph;
using VVVV.PluginInterfaces.V2.NonGeneric;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;
using System.IO;

#endregion usings

namespace VVVV.Audio.VST
{
    public static class VSTService
    {
        public static VstTimeInfo TimeInfo = new VstTimeInfo();
        
        static VSTService()
        {
            AudioService.Engine.FinishedReading += SetupVSTTime;
        }

        static void SetupVSTTime(object sender, EventArgs e)
        {
            var timer = AudioService.Engine.Timer;
            var settings = AudioService.Engine.Settings;
            TimeInfo.SamplePosition = timer.BufferStart;
            TimeInfo.SampleRate = settings.SampleRate;
            TimeInfo.NanoSeconds = timer.Time * 1000000000;
            TimeInfo.PpqPosition = timer.Beat;
            TimeInfo.CycleStartPosition = timer.LoopStartBeat;
            TimeInfo.CycleEndPosition = timer.LoopEndBeat;
            TimeInfo.BarStartPosition = Math.Floor(timer.Beat / 4) * 4;
            TimeInfo.Tempo = timer.BPM;
            TimeInfo.TimeSignatureNumerator = timer.TimeSignatureNumerator;
            TimeInfo.TimeSignatureDenominator = timer.TimeSignatureDenominator;


            TimeInfo.Flags = VstTimeInfoFlags.NanoSecondsValid |
                VstTimeInfoFlags.TempoValid |
                VstTimeInfoFlags.PpqPositionValid |
                VstTimeInfoFlags.CyclePositionValid |
                VstTimeInfoFlags.BarStartPositionValid |
                VstTimeInfoFlags.TransportChanged |
                VstTimeInfoFlags.TimeSignatureValid;

            if (AudioService.Engine.Play)
                TimeInfo.Flags |= VstTimeInfoFlags.TransportPlaying;

            if (timer.Loop)
                TimeInfo.Flags |= VstTimeInfoFlags.TransportCycleActive;

        }
    }
    
    public class VSTSignal : MultiChannelInputSignal
    {
        public VstPluginContext PluginContext;
        internal PluginInfoForm InfoForm;
        protected int FInputCount, FOutputCount;
        IWin32Window FOwnerWindow;
        
        public VSTSignal(string filename, IWin32Window ownerWindow)
        {
            FOwnerWindow = ownerWindow;
            Filename = filename;
        }

        private string FFilename;

        public string Filename
        {
            get { return FFilename; }
            set 
            {
                if (value != FFilename)
                {
                    FDoProcess = false;
                    FFilename = value;
                    Load(FFilename);
                }
            }
        }

        public string[] ProgramNames = new string[0];

        bool FIsSynth;
        protected void Load(string filename)
        {
            if (File.Exists(filename))
            {

                PluginContext = OpenPlugin(filename);
                if(PluginContext == null) return;

                SetOutputCount(PluginContext.PluginInfo.AudioOutputCount);

                PluginContext.PluginCommandStub.MainsChanged(true);
                PluginContext.PluginCommandStub.SetSampleRate(WaveFormat.SampleRate);
                PluginContext.PluginCommandStub.SetBlockSize(AudioService.Engine.Settings.BufferSize);
                FIsSynth = PluginContext.PluginInfo.Flags.HasFlag(VstPluginFlags.IsSynth);
                
                PluginContext.PluginCommandStub.StartProcess();

                FInputCount = PluginContext.PluginInfo.AudioInputCount;
                FOutputCount = PluginContext.PluginInfo.AudioOutputCount;

                FInputMgr = new VstAudioBufferManager(FInputCount, AudioService.Engine.Settings.BufferSize);
                FOutputMgr = new VstAudioBufferManager(FOutputCount, AudioService.Engine.Settings.BufferSize);

                FInputBuffers = FInputMgr.ToArray();
                FOutputBuffers = FOutputMgr.ToArray();

                // plugin does not support processing audio
                if ((PluginContext.PluginInfo.Flags & VstPluginFlags.CanReplacing) == 0)
                {
                    MessageBox.Show("This plugin does not process any audio.");
                    return;
                }

                FCanEvents = PluginContext.PluginCommandStub.CanDo("receiveVstMidiEvent") == VstCanDoResult.Yes; 

                InfoForm = new PluginInfoForm();
                InfoForm.PluginContext = PluginContext;
                InfoForm.DataToForm();
                InfoForm.Dock = DockStyle.Fill;
                InfoForm.ParameterCheck += InfoForm_ParameterCheck;

                GetPluginInfo();
                GetProgramNames();

                if (PluginChanged != null)
                    PluginChanged();

                FDoProcess = true;
            }
        }



        public event ItemCheckEventHandler InfoFormParameterCheck;

        private void InfoForm_ParameterCheck(object sender, ItemCheckEventArgs e)
        {
            this.ParamIndex = e.Index;
            if (InfoFormParameterCheck != null)
            {
                InfoFormParameterCheck(this, e);
            }
        }

        //gets called when the plugin was changed
        public Action PluginChanged
        {
            get;
            set;
        }
        
        void GetPluginInfo()
        {
//			PluginContext.PluginInfo.PluginID;
//			
//			 ListViewItem lvItem = new ListViewItem(PluginContext.PluginCommandStub.GetEffectName());
//                lvItem.SubItems.Add(PluginContext.PluginCommandStub.GetProductString());
//                lvItem.SubItems.Add(PluginContext.PluginCommandStub.GetVendorString());
//                lvItem.SubItems.Add(PluginContext.PluginCommandStub.GetVendorVersion().ToString());
//                lvItem.SubItems.Add(PluginContext.Find<string>("PluginPath"));
        }

        
        private void GetProgramNames()
        {
            ProgramNames = new string[PluginContext.PluginInfo.ProgramCount];
            
            for (int i = 0; i < ProgramNames.Length; i++)
            {
                ProgramNames[i] = PluginContext.PluginCommandStub.GetProgramNameIndexed(i);
            }
            
            //HACK: very evil hack
//            var ctx = OpenPlugin(FFilename);
//
//            for (int i = 0; i < ctx.PluginInfo.ProgramCount; i++)
//            {
//                ctx.PluginCommandStub.SetProgram(i);
//                ProgramNames[i] = ctx.PluginCommandStub.GetProgramName();
//            }
//            
//            ctx.Dispose();
        }

        private void SetNeedsSafe(int index)
        {
            NeedsSave = true;
            ParamIndex = index;
            if (LastParamChangeInfo != null && PluginContext != null)
            {
                string name = PluginContext.PluginCommandStub.GetParameterName(index);
                string label = PluginContext.PluginCommandStub.GetParameterLabel(index);
                string display = PluginContext.PluginCommandStub.GetParameterDisplay(index);

                LastParamChangeInfo(name + " " + label + " " + display);
            }
        }

        public Action<string> LastParamChangeInfo
        {
            get;
            set;
        }

        public int ParamIndex
        {
            get;
            protected set;
        }

        #region Save plugin state
        public string GetSaveString()
        {
            Debug.WriteLine("Saving state: " + PluginContext.PluginCommandStub.GetEffectName());
            
            byte[] chunk = null;
            if(PluginContext.PluginInfo.Flags.HasFlag(VstPluginFlags.ProgramChunks))
            {
                chunk = PluginContext.PluginCommandStub.GetChunk(true);
            }
            else //serialize the floats
            {
                var count = PluginContext.PluginInfo.ParameterCount;

                chunk = new byte[count * 4];

                for (int i = 0; i < count; i++)
                {
                    var floatBytes = BitConverter.GetBytes(PluginContext.PluginCommandStub.GetParameter(i));

                    for (int j = 0; j < 4; j++)
                    {
                        chunk[i * 4 + j] = floatBytes[j];
                    }
                }
            }

            

            if (chunk != null)
                return Convert.ToBase64String(chunk);// + "|" + PluginContext.PluginInfo.PluginID.ToString();
            else
                return "";

        }

        public void LoadFromSafeString(string safeString)
        {
            //safeString.LastIndexOf('|');
            
            if (string.IsNullOrWhiteSpace(safeString) || PluginContext == null) return;
            Debug.WriteLine("Loading state: " + PluginContext.PluginCommandStub.GetEffectName());

            if (PluginContext.PluginInfo.Flags.HasFlag(VstPluginFlags.ProgramChunks))
            {
                PluginContext.PluginCommandStub.BeginSetProgram();
                PluginContext.PluginCommandStub.SetChunk(Convert.FromBase64String(safeString), true);
                PluginContext.PluginCommandStub.EndSetProgram();
            }
            else
            {

                var count = PluginContext.PluginInfo.ParameterCount;

                var data = Convert.FromBase64String(safeString);

                if(count == data.Length/4)
                {
                    for (int i = 0; i < count; i++)
                    {
                        PluginContext.PluginCommandStub.SetParameter(i, BitConverter.ToSingle(data, i*4));
                    }
                }
            }
        }

        #endregion

        private void HostCmdStub_PluginCalled(object sender, PluginCalledEventArgs e)
        {
            HostCommandStub hostCmdStub = (HostCommandStub)sender;
            
            // can be null when called from inside the plugin main entry point.
            if (hostCmdStub.PluginContext.PluginInfo != null)
            {
                Debug.WriteLine("Plugin " + hostCmdStub.PluginContext.PluginInfo.PluginID + " called:" + e.Message);
            }
            else
            {
                Debug.WriteLine("The loading Plugin called:" + e.Message);
            }
        }
        
        //load from file
        private VstPluginContext OpenPlugin(string pluginPath)
        {
            try
            {
                HostCommandStub hostCmdStub = new HostCommandStub();
                hostCmdStub.PluginCalled += HostCmdStub_PluginCalled;
                hostCmdStub.RaiseSave = SetNeedsSafe;
                
                VstPluginContext ctx = VstPluginContext.Create(pluginPath, hostCmdStub);
                var midiOutChannels = ctx.PluginCommandStub.GetNumberOfMidiOutputChannels();
                
                //if(midiOutChannels > 0)
                {
                    hostCmdStub.FProcessEventsAction = ReceiveEvents;
                }
                
                // add custom data to the context
                ctx.Set("PluginPath", pluginPath);
                ctx.Set("HostCmdStub", hostCmdStub);
                
                // actually open the plugin itself
                ctx.PluginCommandStub.Open();
                
                return ctx;
            }
            catch (Exception e)
            {
                MessageBox.Show(FOwnerWindow, e.ToString(), "VST Load", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            
            return null;
        }
        
        //midi out
        void ReceiveEvents(VstEvent[] events)
        {
            foreach (var evt in events) 
            {
                MidiEventSender.SendRawMessage(evt.DeltaFrames, evt.Data[0], evt.Data[1], evt.Data[2]);
            }
        }

        //format changes
        protected override void Engine_SampleRateChanged(object sender, EventArgs e)
        {
            base.Engine_SampleRateChanged(sender, e);
            if (PluginContext != null)
            {
                PluginContext.PluginCommandStub.StopProcess();
                PluginContext.PluginCommandStub.SetSampleRate(WaveFormat.SampleRate);
                PluginContext.PluginCommandStub.StartProcess();
            }
        }

        protected override void Engine_BufferSizeChanged(object sender, EventArgs e)
        {
            base.Engine_BufferSizeChanged(sender, e);
            if (PluginContext != null)
            {
                PluginContext.PluginCommandStub.StopProcess();
                PluginContext.PluginCommandStub.SetBlockSize(BufferSize);
                PluginContext.PluginCommandStub.StartProcess();
            }
        }

        #region midi in
        MidiEvents FMidiEventSource;
        public void SetMidiEventSource(MidiEvents midiEvents)
        {
            if(FMidiEventSource != midiEvents)
            {
                if(FMidiEventSource != null)
                    FMidiEventSource.ShortMessageReceived -= FMidiEventSource_RawMessageReceived;
                
                FMidiEventSource = midiEvents;
                
                if(FMidiEventSource != null)
                {
                    //receive midi events
                    FMidiEventSource.ShortMessageReceived += FMidiEventSource_RawMessageReceived;
                }
            }
        }

        void FMidiEventSource_RawMessageReceived(object sender, ShortMessageEventArgs e)
        {
            SetMidiEvent(e.Message.DeltaFrames, e.Message.Bytes);
        }      
        
        //midi events
        public VstEventCollection MidiEvents = new VstEventCollection();
        private bool FCanEvents;
        private bool FHasEvents;
        public void SetMidiEvent(int deltaFrames, byte[] msg)
        {
            if (FCanEvents)
            {
                VstEvent evt = new VstMidiEvent(deltaFrames, 0, 0, msg, 0, 0);
                MidiEvents.Add(evt);
                FHasEvents = true;
            }
        }
        #endregion
        
        #region midi out
        public ManualMidiEvents MidiEventSender = new ManualMidiEvents();
        
        #endregion
        
        //unmanaged buffers
        VstAudioBufferManager FInputMgr = new VstAudioBufferManager(2, 1);
        VstAudioBufferManager FOutputMgr = new VstAudioBufferManager(2, 1);
        VstAudioBuffer[] FInputBuffers;
        VstAudioBuffer[] FOutputBuffers;
        
        protected void ManageBuffers(int count)
        {
            if(FInputMgr.BufferSize != count)
            {
                FInputMgr.Dispose();
                FOutputMgr.Dispose();
                
                FInputMgr = new VstAudioBufferManager(FInputCount, count);
                FOutputMgr = new VstAudioBufferManager(FOutputCount, count);
                
                FInputBuffers = FInputMgr.ToArray();
                FOutputBuffers = FOutputMgr.ToArray();
            }
        }
        
        //process
        bool FDoProcess;

        public bool Bypass 
        {
            get;
            set;
        }

        protected override void FillBuffers(float[][] buffer, int offset, int count)
        {
            
            if(Bypass) //effects just bypass
            {
                if(FIsSynth) //synths return silece
                {
                    foreach(var buf in buffer)
                    {
                        buf.ReadSilence(offset, count);
                    }
                }
                else
                {
                    var minChannels = Math.Min(FInputCount, FOutputCount);    
                    
                    for (int b = 0; b < minChannels; b++)
                    {
                        var inSig = FInput[b];
                        if (inSig != null)
                        {
                            //read input signals
                            inSig.Read(buffer[b], offset, count);
                        }
                    }
                    
                    if(FOutputCount > FInputCount)
                    {
                        var remains = FOutputCount - FInputCount;
                        for(int i = 0; i < remains; i++)
                        {
                            buffer[i + minChannels - 1] = buffer[i % minChannels];
                        }
                    }
                }
                    
                
                return;
            }
               
            
            if (PluginContext != null && FDoProcess)
            {
                ManageBuffers(count);

                if (FInput != null)
                {
                    FInputMgr.ClearAllBuffers();
                    for (int b = 0; b < FInputCount; b++)
                    {
                        var inSig = FInput[b];
                        if (inSig != null)
                        {
                            var vstBuffer = FInputBuffers[b % FInputCount];

                            //read input, use buffer[0] as temp buffer
                            inSig.Read(buffer[0], offset, count);

                            //copy to vst buffer
                            for (int i = 0; i < count; i++)
                            {
                                vstBuffer[i] += buffer[0][i];
                            }
                        }
                    }
                }

                //add events?
                if (FHasEvents)
                {
                    PluginContext.PluginCommandStub.ProcessEvents(MidiEvents.ToArray());
                    MidiEvents.Clear();
                    FHasEvents = false;
                }
                
                //process audio
                PluginContext.PluginCommandStub.ProcessReplacing(FInputBuffers, FOutputBuffers);

                //copy buffers
                for (int i = 0; i < FOutputBuffers.Length; i++)
                {
                    for (int j = 0; j < count; j++)
                    {
                        buffer[i][j] = FOutputBuffers[i][j];
                    }
                }
            }
        }
        
        public override void Dispose()
        {
            FDoProcess = false;

            //close and dispose vst
            if (PluginContext != null && PluginContext.PluginCommandStub != null)
            {
                PluginContext.PluginCommandStub.StopProcess();
                PluginContext.PluginCommandStub.MainsChanged(false);
                PluginContext.Dispose();
                
            }

            if (FInputMgr != null)
            {
                FInputMgr.Dispose();
                FOutputMgr.Dispose();
            }

            if (InfoForm != null)
            {
                InfoForm.ParameterCheck -= InfoForm_ParameterCheck;
                InfoForm.Dispose();
            }

            
            base.Dispose();
        }


        public bool NeedsSave { get; set; }
        public int EditorHandle { get; internal set; }
    }
}



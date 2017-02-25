#region usings
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using System.Linq;

using Jacobi.Vst.Core;
using Jacobi.Vst.Framework;
using Jacobi.Vst.Interop.Host;

using VVVV.Audio;
using VVVV.Audio.MIDI;
using VVVV.Audio.VST;
using VVVV.Core.Logging;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.PluginInterfaces.V2.Graph;
using VVVV.PluginInterfaces.V2.NonGeneric;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;
using VVVV.Nodes.Nodes.VST;
using Sanford.Multimedia.Midi;

#endregion usings

namespace VVVV.Nodes
{

	
	[PluginInfo(Name = "VSTHost", Category = "VAudio", Version = "Source", Help = "Loads a VST plugin", AutoEvaluate = true, Tags = "plugin, effect")]
    public class VSTHostNode : UserControl, IPluginEvaluate, IDisposable, IPartImportsSatisfiedNotification
	{
        [Config("Safe Data")]
        public ISpread<string> FSafeConfig;

        [Config("Exposed Pins")]
        public IDiffSpread<string> ParameterNamesConfig;

		[Input("Input", BinSize = 2)]
		public IDiffSpread<ISpread<AudioSignal>> FInputSignals;

        [Input("Midi Events")]
        public IDiffSpread<MidiEvents> FMidiEventsIn;
		
		[Input("Filename", StringType = StringType.Filename, FileMask="VST Plugin (*.dll, *.vst3)|*.dll;*.vst3")]
		public IDiffSpread<string> FFilename;

        [Input("Auto Save", DefaultValue = 1)]
        public IDiffSpread<bool> FAutosaveIn;
        
        [Input("Bypass")]
        public IDiffSpread<bool> FBypassIn;
		
		[Output("Audio Out", Order = -10)]
		public Pin<AudioSignal> FOutputSignals;

        [Output("Latency")]
        public ISpread<int> FLatencyOut;

        [Output("Input Channels")]
        public ISpread<int> FInChannelsOut;

        [Output("Output Channels")]
        public ISpread<int> FOutChannelsOut;

        [Output("Editor Handle")]
        public ISpread<int> FEditorHandleOut;

        [Output("Midi Events")]
        public ISpread<MidiEvents> FMidiEventsOut;
        
		IHDEHost FHDEHost;

		IPluginHost FHost;

        [Import]
        IIOFactory FIOFactory;
		
		protected List<IDiffSpread> FDiffInputs = new List<IDiffSpread>();
		
		protected AudioEngine FEngine;
		
        VstPluginControl FPluginControl;
		VstPluginContext SelectedPluginContext;
        
		[ImportingConstructor]
        public VSTHostNode([Import] IHDEHost host, [Import] IPluginHost plugHost)
		{
			FHDEHost = host;
            FHost = plugHost;
			
            //add plugin control
			FPluginControl = new VstPluginControl(this);
            FPluginControl.Dock = DockStyle.Fill;
            FPluginControl.Parent = this;
            this.Controls.Add(FPluginControl);	
			
			Rectangle wndRect = new Rectangle();
			if (SelectedPluginContext != null)
			{
				var PluginCommandStub = SelectedPluginContext.PluginCommandStub;

				this.Text = PluginCommandStub.GetEffectName();

				if (PluginCommandStub.EditorGetRect(out wndRect))
				{
					PluginCommandStub.EditorOpen(this.Handle);
				}
				
				//when window mode changes
				FHDEHost.BeforeComponentModeChange += delegate(object sender, ComponentModeEventArgs args)
				{
					var me = (FHost as INode);
					if(me == args.Window.Node.InternalCOMInterf)
						PluginCommandStub.EditorClose();
				};
				
				FHDEHost.AfterComponentModeChange += delegate(object sender, ComponentModeEventArgs args)
				{
					var me = (FHost as INode);
					if(me == args.Window.Node.InternalCOMInterf)
						PluginCommandStub.EditorOpen(this.Handle);
				};
			}
		}
		
		#region audio node base copy
		public virtual void OnImportsSatisfied()
		{
			FEngine = AudioService.Engine;
            ParameterNamesConfig.SliceCount = 0;
			
			var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

			//Retrieve all FieldInfos
			var fields = GetType().GetFields(flags);
			
			FDiffInputs.Clear();
			
			foreach (var fi in fields)
			{
				if(typeof(IDiffSpread).IsAssignableFrom(fi.FieldType))
				{
					//Retrieve the value of the field, and cast as necessary
					var spread = (IDiffSpread)fi.GetValue(this);
					FDiffInputs.Add(spread);
				}
			}

            ParameterNamesConfig.Changed += ParameterNamesConfig_Changed;
		}
		
		/// <summary>
		/// Should return whether new parameters need to be set on the audio signals
		/// </summary>
		/// <returns></returns>
		protected virtual bool AnyInputChanged()
		{
            FChangedParamPins.Clear();
            foreach (var paramPin in FParamPins.Values)
            {
                if (paramPin.Pin.PinIsChanged)
                    FChangedParamPins.Add(paramPin);
            }
	
			for (int i = 0; i < FDiffInputs.Count; i++) 
			{
				if (FDiffInputs[i].IsChanged) 
                    return true;
			}

			return FChangedParamPins.Count > 0;
		}
        private List<ParamPin> FChangedParamPins = new List<ParamPin>();
		
		//for other methods
        protected int CalculatedSpreadMax
        {
            get;
            private set;
        }

        //all current vst signals
        protected ISpread<VSTSignal> FInternalSignals = new Spread<VSTSignal>();

        //throttle counter for the gui updates
        int FFrameDivider = 3;

        public void Evaluate(int SpreadMax)
        {
        	CalculatedSpreadMax = GetSpreadMax(SpreadMax);
        	FInternalSignals.Resize(CalculatedSpreadMax, GetInstance, DisposeInstance);

            if (AnyInputChanged())
            {
                for(int i=0; i<CalculatedSpreadMax; i++)
				{
					var audioSignal = FInternalSignals[i];
					
					if(audioSignal == null) 
						audioSignal = GetInstance(i);
					
					SetParameters(i, audioSignal);
				}
                
                var outCount = 0;
				for (int i = 0; i < FInternalSignals.SliceCount; i++)
				{
					outCount += FInternalSignals[i].Outputs.Count;
				}
				
				if(FOutputSignals.SliceCount != outCount)
				{
					FOutputSignals.SliceCount = outCount;
					
					var outSlice = 0;
					for (int i = 0; i < FInternalSignals.SliceCount; i++)
					{
						for (int j = 0; j < FInternalSignals[i].Outputs.Count; j++)
						{
							FOutputSignals[outSlice] = FInternalSignals[i].Outputs[j];
							outSlice++;
						}
					}
				}
            }
            
            SetOutputSliceCount(CalculatedSpreadMax);
            
            for(int i=0; i < CalculatedSpreadMax; i++)
            {
            	var audioSignal = FInternalSignals[i];
                if (audioSignal != null)
                {
                    SetOutputs(i, audioSignal);

                    if (audioSignal.PluginContext != null)
                    {
                        //let plugin editor draw itself
                        if (FFrameDivider == 0)
                            audioSignal.PluginContext.PluginCommandStub.EditorIdle();
                    }
                }
            }

            if (FPluginControl.SelectedSignal == null)
            {
                FPluginControl.SelectedSignal = FInternalSignals[0];
            }

            FPluginControl.SetSliceCount(FInternalSignals.SliceCount);

			FFrameDivider++;
			FFrameDivider %= 4;
        }

        /// <summary>
        /// Override this in subclass if you want to set the number of output signals manually
        /// </summary>
        /// <param name="originalSpreadMax"></param>
        /// <returns></returns>
        protected virtual int GetSpreadMax(int originalSpreadMax)
        {
            return FInputSignals.CombineWith(FFilename);
        }

        /// <summary>
        /// This should return a new instance of the desired audio signal class
        /// </summary>
        /// <param name="i">The current slice index of the output signal</param>
        /// <returns>New instnace of the audio signal class</returns>
        protected VSTSignal GetInstance(int i)
        {
            
        	var vst = new VSTSignal(FFilename[i], this);
            vst.LoadFromSafeString(FSafeConfig[i]);

            SetOutputSliceCount(CalculatedSpreadMax);

            if (vst.PluginContext != null)
            {
                FLatencyOut[i] = vst.PluginContext.PluginInfo.InitialDelay;
                FInChannelsOut[i] = vst.PluginContext.PluginInfo.AudioInputCount;
                FOutChannelsOut[i] = vst.PluginContext.PluginInfo.AudioOutputCount;
            }

            foreach (var paramPin in FParamPins.Values)
            {
                UpdateExposedState(vst, paramPin, true);
            }

            return vst;
        }
        
        protected void DisposeInstance(VSTSignal instance)
        {
        	if(instance != null) 
        	{
        		instance.Dispose();
        	}
        }

        /// <summary>
        /// This should set the parameters of the given audio signal class
        /// </summary>
        /// <param name="i">Current index of the output signal spread</param>
        /// <param name="instance">Curretn instance</param>
        protected void SetParameters(int i, VSTSignal instance)
        {
        	instance.Input = FInputSignals[i];
            instance.Filename = FFilename[i];

            foreach (var item in FChangedParamPins)
	        {
                if (instance.PluginContext.PluginCommandStub.GetParameterName(item.ParamIndex) == item.ParamName)
                {
                    double val;
                    item.Pin.GetValue(i, out val);
                    if (val <= 0)
                    {
                        Debug.WriteLine("0 val");
                    }
                    instance.PluginContext.PluginCommandStub.SetParameter(item.ParamIndex, (float)val);
                    FPluginControl.RefreshUI(item.ParamIndex);
                }
            }

            instance.SetMidiEventSource(FMidiEventsIn[i]);
            
            instance.Bypass = FBypassIn[i];
        }
		
		/// <summary>
		/// Set the output pins of the node
		/// </summary>
		/// <param name="i">Current slice index</param>
		/// <param name="instance">Current instance</param>
		protected void SetOutputs(int i, VSTSignal instance)
		{
            if (FAutosaveIn[i] && instance.NeedsSave)
            {
                FSafeConfig[i] = instance.GetSaveString();
                instance.NeedsSave = false;
            }
            
            //assign event sender?
            if(instance.MidiEventSender != FMidiEventsOut[i])
            {
                FMidiEventsOut[i] = instance.MidiEventSender;
            }

            FEditorHandleOut[i] = FPluginControl.EditorHandle;

		}
		
		/// <summary>
		/// In this method the slicecount of the output pins should be set
		/// </summary>
		/// <param name="sliceCount"></param>
		protected void SetOutputSliceCount(int sliceCount)
		{
            if (sliceCount != FSafeConfig.SliceCount) FSafeConfig.SliceCount = sliceCount;
            FLatencyOut.SliceCount = sliceCount;
            FInChannelsOut.SliceCount = sliceCount;
            FOutChannelsOut.SliceCount = sliceCount;
            FMidiEventsOut.SliceCount = sliceCount;
		}

		#endregion
			
		//dispose stuff
		public virtual void Dispose()
		{
			foreach (var element in FInternalSignals) 
			{
				if(element != null)
					element.Dispose();
			}
		}

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // VSTHostNode
            // 
            this.Name = "VSTHostNode";
            this.Size = new System.Drawing.Size(503, 330);
            this.ResumeLayout(false);

        }

        public VSTSignal GetPluginContext(int index)
        {
            return FInternalSignals[index];
        }

        #region pin handling

        public class ParamPin
        {
            public IValueIn Pin
            {
                get;
                set;
            }

            public int ParamIndex
            {
                get;
                set;
            }

            public string ParamName
            {
                get;
                set;
            }

            public string PluginName
            {
                get;
                set;
            }


            public static ParamPin Parse(string paramString)
            {
                var afterIndexNumber = paramString.IndexOf('|');
                var beforePluginName = paramString.LastIndexOf('|');
                var nameLength = beforePluginName - afterIndexNumber - 1;

                var paramIndex = int.Parse(paramString.Substring(0, afterIndexNumber));
                var paramName = paramString.Substring(afterIndexNumber + 1, nameLength);
                var pluginName = paramString.Substring(beforePluginName + 1);

                return new ParamPin { ParamIndex = paramIndex, ParamName = paramName, PluginName = pluginName };
            }
        }

        public void ExposePin(string paramPinDefinitionString)
        {
            if (!ParameterNamesConfig.Contains(paramPinDefinitionString))
            {
                ParameterNamesConfig.Add(paramPinDefinitionString);
            }
        }

        public void RemovePin(string paramPinDefinitionString)
        {
            ParameterNamesConfig.Remove(paramPinDefinitionString);
        }

        private Dictionary<string, ParamPin> FParamPins = new Dictionary<string, ParamPin>();
        void ParameterNamesConfig_Changed(IDiffSpread<string> spread)
        {
            //temp pin dictionary
            var prevPins = new Dictionary<string, ParamPin>(FParamPins);

            //create pin?
            foreach (var pinDefinitionString in spread)
            {
                if(string.IsNullOrWhiteSpace(pinDefinitionString))
                    continue;

                if (!prevPins.ContainsKey(pinDefinitionString))
                {
                    var paramPin = ParamPin.Parse(pinDefinitionString);

                    var oa = new InputAttribute(paramPin.ParamName);
                    //FLogger.Log(LogType.Debug, col.DataType.ToString());

                    paramPin.Pin = FHost.CreateValueInput(oa, typeof(float));
                    foreach (var signal in FInternalSignals)
                    {
                        UpdateExposedState(signal, paramPin, true);
                    }
                    FParamPins[pinDefinitionString] = paramPin;
                }
                else
                {
                    prevPins.Remove(pinDefinitionString);
                }
            }

            //any pin which is left over can be removed
            foreach (var pin in prevPins)
            {
                FHost.DeletePin(pin.Value.Pin);
                FParamPins.Remove(pin.Key);

                foreach (var signal in FInternalSignals)
                {
                    UpdateExposedState(signal, pin.Value, false);
                }
            }
        }

        public static void UpdateExposedState(VSTSignal signal, ParamPin pin, bool isExposed)
        {
            if (signal.PluginContext.PluginCommandStub.GetEffectName() == pin.PluginName)
            {
                signal.InfoForm.ChangeExposedState(pin.ParamIndex, isExposed);
            }
        }

        #endregion pin handling
    }
}



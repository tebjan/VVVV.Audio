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
using NAudio.CoreAudioApi;
using NAudio.Utils;
using NAudio.Wave;
using NAudio.Wave.Asio;
using NAudio.Wave.SampleProviders;
using VVVV.Audio;
using VVVV.Audio.VST;
using VVVV.Core.Logging;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.PluginInterfaces.V2.Graph;
using VVVV.PluginInterfaces.V2.NonGeneric;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;
using VVVV.Nodes.Nodes.VST;

#endregion usings

namespace VVVV.Nodes
{

	
	[PluginInfo(Name = "VSTHost", Category = "Audio", Version = "Source", Help = "Loads a VST plugin", AutoEvaluate = true, Tags = "plugin, effect")]
    public class VSTHostNode : UserControl, IPluginEvaluate, IDisposable, IPartImportsSatisfiedNotification
	{
        [Config("Safe Data")]
        ISpread<string> FSafeConfig;

		[Input("Input", BinSize = 2)]
		IDiffSpread<ISpread<AudioSignal>> FInputSignals;

        [Input("Do Send Midi", IsBang = true)]
        ISpread<bool> FSendMidiIn;

        [Input("Midi Message")]
        IDiffSpread<int> FMsgIn;

        [Input("Midi Data 1")]
        IDiffSpread<int> FData1In;

        [Input("Midi Data 2")]
        IDiffSpread<int> FData2In;
		
		[Input("Filename", StringType = StringType.Filename, FileMask="VST Plugin (*.dll, *.vst3)|*.dll;*.vst3")]
		IDiffSpread<string> FFilename;

        [Input("Auto Save", DefaultValue = 1)]
        ISpread<bool> FAutosafeIn;
		
		[Output("Audio Out", Order = -10)]
		protected Pin<AudioSignal> FOutputSignals;
		
		IHDEHost FHost;
		
		[Import]
		IPluginHost FPlugHost;
		
		protected List<IDiffSpread> FDiffInputs = new List<IDiffSpread>();
		
		protected AudioEngine FEngine;

        VstPluginContext SelectedPluginContext;
        VstPluginControl FPluginControl;
		
		[ImportingConstructor]
		public VSTHostNode([Import] IHDEHost host)
		{
			FHost = host;
			
            //add plugin control
			FPluginControl = new VstPluginControl(this);
            FPluginControl.Dock = DockStyle.Fill;
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
				FHost.BeforeComponentModeChange += delegate(object sender, ComponentModeEventArgs args)
				{
					var me = (FPlugHost as INode);
					if(me == args.Window.Node.InternalCOMInterf)
						PluginCommandStub.EditorClose();
				};
				
				FHost.AfterComponentModeChange += delegate(object sender, ComponentModeEventArgs args)
				{
					var me = (FPlugHost as INode);
					if(me == args.Window.Node.InternalCOMInterf)
						PluginCommandStub.EditorOpen(this.Handle);
				};
			}
		}
		
		#region audio node base copy
		public virtual void OnImportsSatisfied()
		{
			FEngine = AudioService.Engine;
			
			BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

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
		}
		
		/// <summary>
		/// Should return whether new parameters need to be set on the audio signals
		/// </summary>
		/// <returns></returns>
		protected virtual bool AnyInputChanged()
		{			
			for (int i = 0; i < FDiffInputs.Count; i++) 
			{
				if(FDiffInputs[i].IsChanged) return true;
			}
			
			return false;
		}
		
		//for other methods
        protected int CalculatedSpreadMax
        {
            get;
            private set;
        }

        protected ISpread<VSTSignal> FInternalSignals = new Spread<VSTSignal>();
        int FFrameDivider = 0;
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
					outCount += FInternalSignals[i].Outputs.SliceCount;
				}
				
				if(FOutputSignals.SliceCount != outCount)
				{
					FOutputSignals.SliceCount = outCount;
					
					var outSlice = 0;
					for (int i = 0; i < FInternalSignals.SliceCount; i++)
					{
						for (int j = 0; j < FInternalSignals[i].Outputs.SliceCount; j++)
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
            return FInputSignals.SliceCount;
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

            if (FSendMidiIn[i])
            {
                instance.SetMidiEvent((byte)FMsgIn[i], (byte)FData1In[i], (byte)FData2In[i]);
            }
        }
		
		/// <summary>
		/// Set the output pins of the node
		/// </summary>
		/// <param name="i">Current slice index</param>
		/// <param name="instance">Current instance</param>
		protected void SetOutputs(int i, VSTSignal instance)
		{
            if (FAutosafeIn[i] && instance.NeedsSave)
            {
                FSafeConfig[i] = instance.GetSaveString();
                instance.NeedsSave = false;
            }

		}
		
		/// <summary>
		/// In this method the slicecount of the output pins should be set
		/// </summary>
		/// <param name="sliceCount"></param>
		protected void SetOutputSliceCount(int sliceCount)
		{
            if (sliceCount != FSafeConfig.SliceCount) FSafeConfig.SliceCount = sliceCount;
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
    }
}



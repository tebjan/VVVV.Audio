#region usings
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Reflection;

using VVVV.Audio;
using VVVV.Core.Logging;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.PluginInterfaces.V2.NonGeneric;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

#endregion usings

namespace VVVV.Nodes
{
    public abstract class AudioNodeBase<TSignal> : IPluginEvaluate, IDisposable, IPartImportsSatisfiedNotification where TSignal : AudioSignal
    {
        protected List<IDiffSpread> FDiffInputs = new List<IDiffSpread>();
		
		protected AudioEngine FEngine;
		public virtual void OnImportsSatisfied()
		{
			FEngine = AudioService.Engine;
			
			BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

			//Retrieve all FieldInfos
			var fields = GetType().GetFields(flags);
			
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
        
        //for subclasses
        protected int CalculatedSpreadMax
        {
            get;
            private set;
        }

        protected abstract ISpread<AudioSignal> GetSignalSpread();

        public virtual void Evaluate(int SpreadMax)
        {
            
            var signalSpread = GetSignalSpread();
            CalculatedSpreadMax = GetSpreadMax(SpreadMax);
            signalSpread.Resize(CalculatedSpreadMax, GetInstance, x => { if(x != null) x.Dispose(); } );

            if (AnyInputChanged())
            {
                for (int i = 0; i < CalculatedSpreadMax; i++)
                {
                    var audioSignal = signalSpread[i];

                    if (audioSignal == null)
                        audioSignal = GetInstance(i);

                    if (audioSignal is TSignal)
                        SetParameters(i, audioSignal as TSignal);
                }
            }
   
            SetOutputSliceCount(CalculatedSpreadMax);
            
            for(int i=0; i<CalculatedSpreadMax; i++)
            {
                var audioSignal = signalSpread[i];
                
                var tSignal = audioSignal as TSignal;
                if(tSignal != null)
                    SetOutputs(i, tSignal);
            }
        }

        /// <summary>
        /// Override this in subclass if you want to set the number of output signals manually
        /// </summary>
        /// <param name="originalSpreadMax"></param>
        /// <returns></returns>
        protected virtual int GetSpreadMax(int originalSpreadMax)
        {
            return originalSpreadMax;
        }

        /// <summary>
        /// This should return a new instance of the desired audio signal class
        /// </summary>
        /// <param name="i">The current slice index of the output signal</param>
        /// <returns>New instnace of the audio signal class</returns>
        protected abstract TSignal GetInstance(int i);

        /// <summary>
        /// This should set the parameters of the given audio signal class
        /// </summary>
        /// <param name="i">Current index of the output signal spread</param>
        /// <param name="instance">Curretn instance</param>
        protected abstract void SetParameters(int i, TSignal instance);
        
        		/// <summary>
		/// Set the output pins of the node
		/// </summary>
		/// <param name="i">Current slice index</param>
		/// <param name="instance">Current instance</param>
		protected virtual void SetOutputs(int i, TSignal instance) {}
		
		/// <summary>
		/// In this method the slicecount of the output pins should be set
		/// </summary>
		/// <param name="sliceCount"></param>
		protected virtual void SetOutputSliceCount(int sliceCount) {}
		
		//dispose stuff?
		public virtual void Dispose()
		{
		}
    }

    /// <summary>
    /// Base class for audio sink nodes
    /// </summary>
    public abstract class GenericAudioSinkNode<TSignal> : AudioNodeBase<TSignal> where TSignal : SinkSignal
    {
        [Input("Input", Order = -10)]
        public IDiffSpread<AudioSignal> FInputs;

        protected ISpread<AudioSignal> FSignals = new Spread<AudioSignal>();

        protected override ISpread<AudioSignal> GetSignalSpread()
        {
            return FSignals;
        }
    }

    /// <summary>
    /// Audio source nodes
    /// </summary>
    public abstract class GenericAudioSourceNode<TSignal> : AudioNodeBase<TSignal> where TSignal : AudioSignal
	{
    	[Import]
    	protected IIOFactory FIOFactory;
		//[Output("Audio Out", Order = -10)]
		public Pin<AudioSignal> FOutputSignals;

		public override void OnImportsSatisfied()
		{
			
			base.OnImportsSatisfied();

			var outputAttribute = new OutputAttribute("Audio Out")
			{
				Order = -10,
				Visibility = GetOutputVisiblilty()
			};
			
			FOutputSignals = FIOFactory.CreatePin<AudioSignal>(outputAttribute);
			
			FOutputSignals.Connected += delegate
			{
				CheckOutConnections();
			};
			
			FOutputSignals.Disconnected += delegate
			{
				CheckOutConnections();
			};

			//set out buffer slice count to 0 so the
			FOutputSignals.SliceCount = 0;
		}
		
		protected virtual PinVisibility GetOutputVisiblilty()
		{
			return PinVisibility.True;
		}
		
		private void CheckOutConnections()
		{
			var hasMultiple = (FOutputSignals.PluginIO as IPin).GetConnectedPins().Length > 1;
			
			for (int i = 0; i < FOutputSignals.SliceCount; i++)
			{
				FOutputSignals[i].NeedsBufferCopy = hasMultiple;
			}
		}

        protected override ISpread<AudioSignal> GetSignalSpread()
        {
            return FOutputSignals;
        }
    }
	
	/// <summary>
	/// Audio node base class with multichannel signals and automatic instance handling
	/// </summary>
    public abstract class GenericMultiAudioSourceNode<TSignal> : GenericAudioSourceNode<TSignal> where TSignal : MultiChannelSignal
	{
		protected ISpread<TSignal> FInternalSignals = new Spread<TSignal>();
		
		//for subclasses
		protected int CalculatedSpreadMax
		{
			get;
			private set;
		}
		
		public override void Evaluate(int SpreadMax)
		{
			CalculatedSpreadMax = GetSpreadMax(SpreadMax);
			FInternalSignals.Resize(CalculatedSpreadMax, GetInstance, x => { if(x != null) x.Dispose(); } );
			
			if(AnyInputChanged())
			{
				for(int i=0; i<CalculatedSpreadMax; i++)
				{
					var audioSignal = FInternalSignals[i];
					
					if(audioSignal == null) 
						audioSignal = GetInstance(i);
					
                    var tSignal = audioSignal as TSignal;
					if(tSignal != null)
                        SetParameters(i, tSignal);
				}
				
				var outCount = 0;
				for (int i = 0; i < FInternalSignals.SliceCount; i++)
				{
					outCount += FInternalSignals[i].Outputs.Count;
				}
				
				if(FOutputSignals.SliceCount != outCount)
				{
					//FOutputSignals.SliceCount = outCount;
					FOutputSignals.ResizeAndDispose(outCount, () => { return null; });
					
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
			
			for(int i=0; i<CalculatedSpreadMax; i++)
			{
				var audioSignal = FInternalSignals[i];
				
                var tSignal = audioSignal as TSignal;
				if(tSignal != null)
                    SetOutputs(i, tSignal);
			}
		}
		
	}
	
}



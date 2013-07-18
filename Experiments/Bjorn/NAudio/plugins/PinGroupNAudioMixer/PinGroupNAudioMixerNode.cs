#region usings
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.Streams;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using NAudio;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

using VVVV.Core.Logging;
#endregion usings

namespace VVVV.Nodes
{
	#region PluginInfo
	[PluginInfo(Name = "Mixer", Category = "NAudio", Version = "PinGroup", Help = "Basic template with a dynamic amount of in- and outputs", Tags = "")]
	#endregion PluginInfo
	public class PinGroupNAudioMixerNode : IPluginEvaluate, IPartImportsSatisfiedNotification, ISampleProvider, IDisposable
	{
		#region fields & pins
		
		[Input("Gain", DefaultValue = 1.0f)]
		IDiffSpread<float> FGain;
		
		// A spread which contains our inputs
		Spread<IIOContainer<ISpread<ISampleProvider>>> FInputs = new Spread<IIOContainer<ISpread<ISampleProvider>>>();

		[Config("Input Count", DefaultValue = 2, MinValue = 1)]
		IDiffSpread<int> FInputCountIn;

		[Output("Output")]
		ISpread<ISampleProvider> FOutput;

		[Import()]
		IIOFactory FIOFactory;
		
		GainMixingSampleProvider FMix;
		bool FNeedsUpdate = true;
		bool startUp = true;
		
		#endregion fields & pins

		#region pin management
		public void OnImportsSatisfied()
		{
			FInputCountIn.Changed += HandleInputCountChanged;
		}

		private void HandlePinCountChanged<T>(ISpread<int> countSpread, Spread<IIOContainer<T>> pinSpread, Func<int, IOAttribute> ioAttributeFactory) where T : class
		{
			pinSpread.ResizeAndDispose(countSpread[0], i =>
			{
				var ioAttribute = ioAttributeFactory(i + 1);
				return FIOFactory.CreateIOContainer<T>(ioAttribute);
			});
		}

		private void HandleInputCountChanged(IDiffSpread<int> sender)
		{
			HandlePinCountChanged(sender, FInputs, i => new InputAttribute(string.Format("Input {0}", i)));
			FNeedsUpdate = true;
		}
		#endregion
		
		public PinGroupNAudioMixerNode()
		{
			
		}

		public void Dispose()
		{
			
		}

		// Called when data for any output pin is requested.
		public void Evaluate(int SpreadMax)
		{
			if( (FGain.IsChanged || startUp) && FMix != null )
			{
				FMix.gainCount = FGain.SliceCount;
				for(int i = 0; i < Math.Max(FGain.SliceCount, GainMixingSampleProvider.maxInputs); i++)
				{
					FMix.gain[i] = FGain[i];	
				}
				if( startUp ) startUp = false;
			}
			if(FOutput[0] != this)
			{
				FMix = new GainMixingSampleProvider(this.WaveFormat);
				FMix.ReadFully = true;
				FOutput[0] = this;		
			}		
			for( int i = 0; i < FInputs.SliceCount; i++)
			{
				if(FInputs[i].IOObject.IsChanged)
				{
					FNeedsUpdate = true;
				}
			}
			
			if(FNeedsUpdate)
			{
				if(FMix != null)
				{
					FMix.RemoveAllMixerInputs();
				
					for( int i = 0; i < FInputs.SliceCount; i++)
					{
						if(FInputs[i].IOObject[0] != null)
						{
							MixerChannel newChan = new MixerChannel();
							newChan.index = i;
							newChan.sampleProvider = FInputs[i].IOObject[0];
							FMix.AddMixerInput(newChan);
						}
					}			
				}		
				FNeedsUpdate = false;
			}			
		}
		
		public WaveFormat WaveFormat
		{
			get{ return WaveFormat.CreateIeeeFloatWaveFormat(44100, 2); }
		}
		
		public int Read(float[] buffer, int offset, int count)
		{
			return FMix.Read(buffer, offset, count);
		}		
	}
}

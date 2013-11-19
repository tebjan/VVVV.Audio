#region usings
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Reflection;

using NAudio.CoreAudioApi;
using NAudio.Utils;
using NAudio.Wave;
using NAudio.Wave.Asio;
using NAudio.Wave.SampleProviders;
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

	public abstract class AudioNodeBase : IPluginEvaluate, IDisposable, IPartImportsSatisfiedNotification
	{
		[Output("Audio Out", Order = -10)]
		protected Pin<AudioSignal> OutBuffer;
		
		protected List<IDiffSpread> FDiffInputs = new List<IDiffSpread>();
		
		AudioEngine FEngine;
		public virtual void OnImportsSatisfied()
		{
			FEngine = AudioService.Engine;
			
			OutBuffer.Connected += delegate
			{
				CheckOutConnections();
			};
			
			OutBuffer.Disconnected += delegate
			{
				CheckOutConnections();
			};
			
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
		
		private void CheckOutConnections()
		{
			var hasMultiple = (OutBuffer.PluginIO as IPin).GetConnectedPins().Length > 1;
			
			for (int i = 0; i < OutBuffer.SliceCount; i++)
			{
				OutBuffer[i].NeedsBufferCopy = hasMultiple;
			}
		}
		
		//called when data for any output pin is requested
		public abstract void Evaluate(int SpreadMax);
		
		//dispose stuff?
		public virtual void Dispose()
		{
		}
	}
	
	public abstract class GenericMultiChannelAudioSourceNode<T> : AudioNodeBase where T : MultiChannelSignal
	{
		protected ISpread<T> FInternalSignals = new Spread<T>();
		
		//for subclasses
		protected int CalculatedSpreadMax
		{
			get;
			private set;
		}
		
		public override void Evaluate(int SpreadMax)
		{
			CalculatedSpreadMax = GetSpreadMax(SpreadMax);
			FInternalSignals.ResizeAndDispose(CalculatedSpreadMax, GetInstance);
			
			if(AnyInputChanged())
			{
				for(int i=0; i<SpreadMax; i++)
				{
					var audioSignal = FInternalSignals[i];
					
					if(audioSignal == null) 
						audioSignal = GetInstance(i);
					
					if(audioSignal is T)
						SetParameters(i, audioSignal as T);
				}
				
				var outCount = 0;
				for (int i = 0; i < FInternalSignals.SliceCount; i++)
				{
					outCount += FInternalSignals[i].Outputs.SliceCount;
				}
				
				OutBuffer.SliceCount = outCount;
				
				var outSlice = 0;
				for (int i = 0; i < FInternalSignals.SliceCount; i++)
				{
					for (int j = 0; j < FInternalSignals[i].Outputs.SliceCount; j++)
					{
						OutBuffer[outSlice] = FInternalSignals[i].Outputs[j];
						outSlice++;
					}
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
		protected abstract T GetInstance(int i);
		
		/// <summary>
		/// This should set the parameters of the given audio signal class
		/// </summary>
		/// <param name="i">Current index of the output signal spread</param>
		/// <param name="instance">Curretn instance</param>
		protected abstract void SetParameters(int i, T instance);
		
	}
	
	public abstract class GenericMultiChannelAudioSourceNodeWithOutputs<TSignal> : GenericMultiChannelAudioSourceNode<TSignal> where TSignal : MultiChannelSignal
	{
		public override void Evaluate(int SpreadMax)
		{
			base.Evaluate(SpreadMax);
			
			for(int i=0; i<CalculatedSpreadMax; i++)
			{
				var audioSignal = FInternalSignals[i];
				
				if(audioSignal is TSignal)
					SetOutputs(i, audioSignal as TSignal);
			}
		}
		
		/// <summary>
		/// Set the output pins of the node
		/// </summary>
		/// <param name="i">Current slice index</param>
		/// <param name="instance">Current instance</param>
		protected abstract void SetOutputs(int i, TSignal instance);
	}
	
	public abstract class GenericAudioSourceNode<TSignal> : AudioNodeBase where TSignal : AudioSignal
	{
		//for subclasses
		protected int CalculatedSpreadMax
		{
			get;
			private set;
		}
	
		public override void Evaluate(int SpreadMax)
		{
			CalculatedSpreadMax = GetSpreadMax(SpreadMax);
			OutBuffer.ResizeAndDispose(CalculatedSpreadMax, GetInstance);
			
			if(AnyInputChanged())
			{
				for(int i=0; i<CalculatedSpreadMax; i++)
				{
					var audioSignal = OutBuffer[i];
					
					if(audioSignal == null) 
						audioSignal = GetInstance(i);
					
					if(audioSignal is TSignal)
						SetParameters(i, audioSignal as TSignal);
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
		protected abstract AudioSignal GetInstance(int i);
		
		/// <summary>
		/// This should set the parameters of the given audio signal class
		/// </summary>
		/// <param name="i">Current index of the output signal spread</param>
		/// <param name="instance">Curretn instance</param>
		protected abstract void SetParameters(int i, TSignal instance);
	}
	
	public abstract class GenericAudioSourceNodeWithOutputs<TSignal> : GenericAudioSourceNode<TSignal> where TSignal : AudioSignal
	{
		public override void Evaluate(int SpreadMax)
		{
			base.Evaluate(SpreadMax);
			
			for(int i=0; i<CalculatedSpreadMax; i++)
			{
				var audioSignal = OutBuffer[i];
				
				if(audioSignal is TSignal)
					SetOutputs(i, audioSignal as TSignal);
			}
		}
		
		/// <summary>
		/// Set the output pins of the node
		/// </summary>
		/// <param name="i">Current slice index</param>
		/// <param name="instance">Current instance</param>
		protected abstract void SetOutputs(int i, TSignal instance);
	}
}



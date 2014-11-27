/*
 * Created by SharpDevelop.
 * User: TF
 * Date: 27.11.2014
 * Time: 11:05
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Reflection;
using VVVV.Audio;
using VVVV.PluginInterfaces.V2;
using VVVV.PluginInterfaces.V2.NonGeneric;

namespace VVVV.Nodes
{
    public class AutoAudioSignalNode<TSignal> : GenericAudioSourceNode<TSignal> where TSignal : AudioSignal, new()
	{
	    //static pin storage
	    Dictionary<string, IDiffSpread> FInputPins = new Dictionary<string, IDiffSpread>();
	    Dictionary<string, ISpread> FOutputPins = new Dictionary<string, ISpread>();
	    
	    //param to pin
	    Dictionary<SigParamBase, IDiffSpread> FInputPinRelation = new Dictionary<SigParamBase, IDiffSpread>();
	    Dictionary<SigParamBase, ISpread> FOutputPinRelation = new Dictionary<SigParamBase, ISpread>();
	    
	    //create pins
        public override void OnImportsSatisfied()
        {
            base.OnImportsSatisfied();
            
            var tempSig = new TSignal();
            
            var t = tempSig.GetType();
            var flags = BindingFlags.Instance | BindingFlags.Public;

			//Retrieve all FieldInfos
			var fields = t.GetFields(flags);
			
			foreach (var fi in fields)
			{
				if(typeof(SigParamBase).IsAssignableFrom(fi.FieldType))
				{
				    var param = (SigParamBase)fi.GetValue(tempSig);
				    var valType = param.GetValueType();
				    
				    if(!param.IsOutput)
				    {
				        var ia = new InputAttribute(param.Name);
				        var spreadType = typeof(IDiffSpread<>).MakeGenericType(valType);

				        var inPin = (IDiffSpread)FIOFactory.CreateIO(spreadType, ia);
				        FInputPins[param.Name] = inPin;
				        FDiffInputs.Add(inPin);
				    }
				    else
				    {
				        var oa = new OutputAttribute(param.Name);
				        var spreadType = typeof(ISpread<>).MakeGenericType(valType);

				        var outPin = (ISpread)FIOFactory.CreateIO(spreadType, oa);
				        FOutputPins[param.Name] = outPin;
				    }
				}
			}
			
			tempSig.Dispose();
        }
		
		protected override void SetParameters(int i, TSignal instance)
		{
            foreach (var param in instance.InParams) 
            {
                param.SetValue(FInputPinRelation[param][i]);
            }
		}
		
        protected override void SetOutputSliceCount(int sliceCount)
        {
            base.SetOutputSliceCount(sliceCount);
            
            foreach (var pin in FOutputPins.Values) 
            {
                pin.SliceCount = sliceCount;
            }
        }
        
        protected override void SetOutputs(int i, TSignal instance)
        {
            foreach (var param in instance.OutParams) 
            {
                FOutputPinRelation[param][i] = param.GetValue();
            }
        }

        protected override TSignal GetInstance(int i)
		{
            var instance = new TSignal();

            //assign pin relation
            var flags = BindingFlags.Instance | BindingFlags.Public;
			var fields = instance.GetType().GetFields(flags);
			
			foreach (var fi in fields)
			{
				if(typeof(SigParamBase).IsAssignableFrom(fi.FieldType))
				{
				    var param = (SigParamBase)fi.GetValue(instance);
				    
				    if(param.IsOutput)
				    {
				        FOutputPinRelation[param] = FOutputPins[param.Name];
				    }
				    else
				    {
				        FInputPinRelation[param] = FInputPins[param.Name];
				    }
				}
			}
            
			return instance;
		}
        
        protected override void DisposeInstance(AudioSignal instance)
        {
            //remove pin relation
            var flags = BindingFlags.Instance | BindingFlags.Public;
			var fields = instance.GetType().GetFields(flags);
			
			foreach (var fi in fields)
			{
				if(typeof(SigParamBase).IsAssignableFrom(fi.FieldType))
				{
				    var param = (SigParamBase)fi.GetValue(instance);
				    
				    if(param.IsOutput)
				    {
				        FOutputPinRelation.Remove(param);
				    }
				    else
				    {
				        FInputPinRelation.Remove(param);
				    }
				}
			}
			
            base.DisposeInstance(instance);
        }
	}
}

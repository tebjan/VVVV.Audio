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
    public class AutoAudioMultiSignalNode<TMultiSignal> : AutoAudioSignalNode<TMultiSignal> where TMultiSignal : MultiChannelSignal, new()
    {
        
    }
    
    public class AutoAudioSinkSignalNode<TSinkSignal> : AutoAudioSignalNode<TSinkSignal> where TSinkSignal : SinkSignal, new()
    {
        protected override PinVisibility GetOutputVisiblilty()
        {
            return PinVisibility.False;
        }
    }
    
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
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

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
				        
				        if(valType == typeof(double))
				        {
				            ia.DefaultValue = (double)param.GetDefaultValue();
				        }
				        else if(valType == typeof(float))
				        {
				            ia.DefaultValue = (float)param.GetDefaultValue();
				        }
				        else if(valType == typeof(int))
				        {
				            ia.DefaultValue = (int)param.GetDefaultValue();
				        }
				        else if(valType == typeof(long))
				        {
				            ia.DefaultValue = (long)param.GetDefaultValue();
				        }
				        else if(valType == typeof(float[]))
				        {
				            spreadType = typeof(IDiffSpread<>).MakeGenericType(typeof(ISpread<float>));
				        }
				        else if(valType == typeof(int[]))
				        {
				            spreadType = typeof(IDiffSpread<>).MakeGenericType(typeof(ISpread<int>));
				        }
				        else if(valType == typeof(double[]))
				        {
				            spreadType = typeof(IDiffSpread<>).MakeGenericType(typeof(ISpread<double>));
				        }
				        else if(typeof(Enum).IsAssignableFrom(valType))
				        {
				            ia.DefaultEnumEntry = param.GetDefaultValue().ToString();
				        }
				        else
				        {
				            if(param is SigParamBang)
				            {
				                ia.IsBang = true;
				            }
				        }

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
                var inputValue = FInputPinRelation[param][i];
                object val = null;
                
                var floatSpread = inputValue as ISpread<float>;
                if(floatSpread != null)
                {
                    var arr = new float[floatSpread.SliceCount];
                    Array.Copy(floatSpread.Stream.Buffer, arr, floatSpread.SliceCount);
                    val = arr;
                }
                else
                {
                    var doubleSpread = inputValue as ISpread<double>;
                    if(doubleSpread != null)
                    {
                        var arr = new double[doubleSpread.SliceCount];
                        Array.Copy(doubleSpread.Stream.Buffer, arr, doubleSpread.SliceCount);
                        val = arr;
                    }
                    else
                    {
                        var intSpread = inputValue as ISpread<int>;
                        if(intSpread != null)
                        {
                            var arr = new int[intSpread.SliceCount];
                            Array.Copy(intSpread.Stream.Buffer, arr, intSpread.SliceCount);
                            val = arr;
                        }
                        else
                        {
                            val = inputValue;
                        }
                    }
                }
                
                param.SetValue(val);
            }
		}
		
        protected override int GetSpreadMax(int originalSpreadMax)
        {
            var original = base.GetSpreadMax(originalSpreadMax);
            var result = 0;
            foreach(var inPin in FInputPins.Values)
            {
                result = Math.Max(result, inPin.SliceCount);
            }
            
            return Math.Min(result, original);
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
            foreach (var param in AudioSignal.GetParams(instance))
            {
                if(param.IsOutput)
                {
                    FOutputPinRelation[param] = FOutputPins[param.Name];
                }
                else
                {
                    FInputPinRelation[param] = FInputPins[param.Name];
                }
            }
            
			return instance;
		}
        
        protected override void DisposeInstance(AudioSignal instance)
        {
            //remove pin relation
            foreach (var param in AudioSignal.GetParams(instance))
            {
                if(param.IsOutput)
                {
                    FOutputPinRelation.Remove(param);
                }
                else
                {
                    FInputPinRelation.Remove(param);
                }
            }
            
            base.DisposeInstance(instance);
        }
	}
}

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
using System.Runtime.InteropServices;
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
	    Dictionary<SigParamBase, IDiffSpread> FInputPinToParamMap = new Dictionary<SigParamBase, IDiffSpread>();
	    Dictionary<SigParamBase, ISpread> FOutputPinToParamMap = new Dictionary<SigParamBase, ISpread>();
	    
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
                        ia.Order = param.PinOrder;
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
				    else //output
				    {
				        var oa = new OutputAttribute(param.Name);
                        oa.Order = param.PinOrder;
				        var spreadType = typeof(ISpread<>).MakeGenericType(valType);

                        //array types need Spread<Spread<T>>
                        if (valType == typeof(float[]))
                        {
                            spreadType = typeof(ISpread<>).MakeGenericType(typeof(ISpread<float>));
                        }
                        else if (valType == typeof(int[]))
                        {
                            spreadType = typeof(ISpread<>).MakeGenericType(typeof(ISpread<int>));
                        }
                        else if (valType == typeof(double[]))
                        {
                            spreadType = typeof(ISpread<>).MakeGenericType(typeof(ISpread<double>));
                        }

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
                //get slice
                var inputValue = FInputPinToParamMap[param][i];
                
                //check array types and do block copy
                var floatSpread = inputValue as ISpread<float>;
                if(floatSpread != null)
                {
                    var arr = new float[floatSpread.SliceCount];
                    Array.Copy(floatSpread.Stream.Buffer, arr, floatSpread.SliceCount);
                    param.SetValue(arr);
                    continue; //finished
                }

                var doubleSpread = inputValue as ISpread<double>;
                if (doubleSpread != null)
                {
                    var arr = new double[doubleSpread.SliceCount];
                    Array.Copy(doubleSpread.Stream.Buffer, arr, doubleSpread.SliceCount);
                    param.SetValue(arr);
                    continue; //finished
                }

                var intSpread = inputValue as ISpread<int>;
                if (intSpread != null)
                {
                    var arr = new int[intSpread.SliceCount];
                    Array.Copy(intSpread.Stream.Buffer, arr, intSpread.SliceCount);
                    param.SetValue(arr);
                    continue; //finished
                }

                //normal value
                param.SetValue(inputValue);
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
                var inputValue = param.GetValue();

                if (inputValue == null) continue;

                //check array types and do block copy
                var floatArray = inputValue as float[];
                if (floatArray != null)
                {
                    var spread = new Spread<float>(floatArray.Length);
                    Array.Copy(floatArray, spread.Stream.Buffer, floatArray.Length);
                    FOutputPinToParamMap[param][i] = spread;
                    continue; //finished
                }

                var doubleArray = inputValue as double[];
                if (doubleArray != null)
                {
                    var spread = new Spread<float>(doubleArray.Length);
                    Array.Copy(doubleArray, spread.Stream.Buffer, doubleArray.Length);
                    FOutputPinToParamMap[param][i] = spread;
                    continue; //finished
                }

                var intArray = inputValue as int[];
                if (intArray != null)
                {
                    var spread = new Spread<float>(intArray.Length);
                    Array.Copy(intArray, spread.Stream.Buffer, intArray.Length);
                    FOutputPinToParamMap[param][i] = spread;
                    continue; //finished
                }

                FOutputPinToParamMap[param][i] = param.GetValue();
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
                    FOutputPinToParamMap[param] = FOutputPins[param.Name];
                }
                else
                {
                    FInputPinToParamMap[param] = FInputPins[param.Name];
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
                    FOutputPinToParamMap.Remove(param);
                }
                else
                {
                    FInputPinToParamMap.Remove(param);
                }
            }
            
            base.DisposeInstance(instance);
        }
	}
}

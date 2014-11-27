/*
 * Created by SharpDevelop.
 * User: TF
 * Date: 27.11.2014
 * Time: 04:53
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;

namespace VVVV.Audio
{
    /// <summary>
    /// Name and type of an audio signal parameter
    /// </summary>
    public class SignalParameterDecriptor
    {
        public SignalParameterDecriptor(string name, Type type, bool isOutput = false)
        {
            Name = name;
            Type = type;
            IsOutput = false;
        }
        
        public readonly string Name;
        public readonly Type Type;
        public readonly bool IsOutput;
    }
    
    public class SignalParameterDescription : List<SignalParameterDecriptor>
    {
        public void AddInputParam(string name, Type type)
        {
            this.Add(new SignalParameterDecriptor(name, type));
        }
        
        public void AddOutputParam(string name, Type type)
        {
            this.Add(new SignalParameterDecriptor(name, type, true));
        }
    }
    
    public abstract class AbstractSignalParameter
    {
        public AbstractSignalParameter(string name, bool isOutput = false)
        {
            Name = name;
            IsOutput = isOutput;
        }
        
        public readonly string Name;
        public readonly bool IsOutput;
    }
    
    /// <summary>
    /// A parameter of an AudioSignal
    /// </summary>
    public class SigParam<T> : AbstractSignalParameter
    {
        public SigParam(string name, bool isOutput = false)
            : this(name, default(T), isOutput)
        {
        }
        
        public SigParam(string name, T initValue, bool isOutput = false)
            : base(name, isOutput)
        {
            InitialValue = initValue;
            Value = initValue;
        }
        
        public readonly T InitialValue;
        
        public T Value
        {
            get;
            set;
        }
    }
    
    public class SigParamDiff<T> : AbstractSignalParameter
    {
        public SigParamDiff(string name, bool isOutput = false)
            : this(name, default(T), null, isOutput)
        {
        }
        
        public SigParamDiff(string name, T initValue, Action<T> valueChanged = null, bool isOutput = false)
            : base(name, isOutput)
        {
            InitialValue = initValue;
            Value = initValue;
            ValueChanged = valueChanged;
        }
        
        public readonly T InitialValue;
        
        private T FValue;
        public T Value
        {
            get
            {
                return FValue;
            }
            set
            {
                if(!FValue.Equals(value))
                {
                    FValue = value;
                    if(ValueChanged != null)
                        ValueChanged(value);
                }
            }
        }
        
        public Action<T> ValueChanged;
    }
    

    /// <summary>
    /// A parameter of an AudioSignal
    /// </summary>
    public class SigParamSec<T> : AbstractSignalParameter
    {
        public SigParamSec(string name, bool isOutput = false)
            : this(name, default(T), isOutput)
        {
        }
        
        public SigParamSec(string name, T initValue, bool isOutput = false)
            : base(name, isOutput)
        {
            InitialValue = initValue;
            Value = initValue;
        }
        
        public readonly T InitialValue;
        
        public T Value
        {
            get
            {
                T ret;
                GetLatestValue(out ret);
                return ret;
            }
            set
            {
                SetLatestValue(value);
            }
        }
        
        private volatile bool FReading;
		private volatile bool FWriting;
		private T FValueToPass;
		private T FLastValue;
		
        public bool GetLatestValue(out T value)
		{
			var success = false;
			FReading = true;
			if (!FWriting)
			{
				FLastValue = FValueToPass;
				success = true;
			}
			else
			{
				System.Diagnostics.Debug.WriteLine("Could not read");
			}
			
			value = FLastValue;
			FReading = false;
			return success;
		}
		
		protected bool SetLatestValue(T newValue)
		{
			var success = false;
			FWriting = true;
			if (!FReading)
			{
				FValueToPass = newValue;
				success = true;
			}
			else
			{
				System.Diagnostics.Debug.WriteLine("Could not write");
			}
			FWriting = false;
			return success;
		}
    }
    
    
    
//    /// <summary>
//    /// Audio Signal Parameter
//    /// </summary>
//    public class AudioSignalParameter : SignalParameter
//    {
//        public override Type Type 
//        { 
//            get
//            {
//                return typeof(AudioSignal);
//            }
//        }
//        
//        public AudioSignal Value
//        {
//            get;
//            set;
//        }
//    }
}

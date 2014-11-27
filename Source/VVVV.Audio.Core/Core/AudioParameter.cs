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
  
    public abstract class SigParamBase
    {
        public SigParamBase(string name, bool isOutput = false)
        {
            Name = name;
            IsOutput = isOutput;
        }
        
        public readonly string Name;
        public readonly bool IsOutput;
        
        public abstract Type GetValueType();

        public abstract object GetValue();

        public abstract void SetValue(object value);
    }
    
    /// <summary>
    /// A parameter of an AudioSignal
    /// </summary>
    public class SigParam<T> : SigParamBase
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
        
        public override object GetValue()
        {
            return Value;
        }

        public override void SetValue(object value)
        {
            Value = (T)value;
        }

        public override Type GetValueType()
        {
            return typeof(T);
        }

    }
    
    public class SigParamDiff<T> : SigParamBase
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
        
        public override object GetValue()
        {
            return Value;
        }

        public override void SetValue(object value)
        {
            Value = (T)value;
        }
        
        public override Type GetValueType()
        {
            return typeof(T);
        }
    }
    

    /// <summary>
    /// A parameter of an AudioSignal
    /// </summary>
    public class SigParamSec<T> : SigParamBase
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
		
        bool GetLatestValue(out T value)
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
		
		bool SetLatestValue(T newValue)
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
		
		public override object GetValue()
        {
            return Value;
        }
		
		public override void SetValue(object value)
        {
		    Value = (T)value;
        }
		
		public override Type GetValueType()
		{
		    return typeof(T);
		}
    }
}

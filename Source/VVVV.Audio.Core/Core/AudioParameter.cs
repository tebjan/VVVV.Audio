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
    /// Representation of input and output data for AudioSignals
    /// </summary>
    public abstract class SigParamBase
    {
        public readonly string Name;
        public readonly bool IsOutput;
        
        protected SigParamBase(string name, bool isOutput = false)
        {
            Name = name;
            IsOutput = isOutput;
        }
        
        public abstract Type GetValueType();

        public abstract object GetValue();

        public abstract object GetDefaultValue();
        
        public abstract void SetValue(object value);
    }
    
    /// <summary>
    /// Helper for generic params
    /// </summary>
    public abstract class SigParamBaseGeneric<T> : SigParamBase
    {
        public readonly T InitialValue;
        
        protected SigParamBaseGeneric(string name, bool isOutput = false)
            : this(name, default(T), isOutput)
        {
        }
        
        protected SigParamBaseGeneric(string name, T initValue, bool isOutput = false)
            : base(name, isOutput)
        {
            InitialValue = initValue;
        }
        
        //internal value field
        protected T FValue;
        
        public override object GetValue()
        {
            return FValue;
        }
        
        public override object GetDefaultValue()
        {
            return InitialValue;
        }
        
        public override Type GetValueType()
        {
            return typeof(T);
        }
        
        public override void SetValue(object value)
        {
            FValue = (T)value;
        }
    }
    
    /// <summary>
    /// A parameter of an AudioSignal
    /// </summary>
    public class SigParam<T> : SigParamBaseGeneric<T>
    {
        public SigParam(string name, bool isOutput = false)
            : base(name, isOutput)
        {
        }
        
        public SigParam(string name, T initValue, bool isOutput = false)
            : base(name, initValue, isOutput)
        {
            
        }

        public T Value 
        {
            get { return FValue; }
            set { FValue = value; }
        }
    }
    
    /// <summary>
    /// Signal parameter which detects changes
    /// </summary>
    public class SigParamDiff<T> : SigParamBaseGeneric<T>
    {
        //callback for value changes
        public Action<T> ValueChanged;
        
        public SigParamDiff(string name, bool isOutput = false)
            : base(name, isOutput)
        {
        }
        
        public SigParamDiff(string name, T initValue, Action<T> valueChanged = null, bool isOutput = false)
            : base(name, initValue, isOutput)
        {
            Value = initValue;
            ValueChanged = valueChanged;
        }

        public T Value
        {
            get
            {
                return FValue;
            }
            set
            {
                if(value != null && !value.Equals(FValue))
                {
                    FValue = value;
                    if(ValueChanged != null)
                        ValueChanged(value);
                }
                else if(value == null && FValue != null)
                {
                    FValue = default(T);
                }
            }
        }

        public override void SetValue(object value)
        {
            Value = (T)value;
        }
    }
    
    public class SigParamAudio : SigParamDiff<AudioSignal>
    {
        public SigParamAudio(string name, bool isOutput = false)
            : base(name, isOutput)
        {
        }
        
        /// <summary>
        /// Safe read method of the internal audio signal.
        /// Reads silence if the internal audio signal is not set.
        /// </summary>
        /// <param name="buffer">The buffer to fill</param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public void Read(float[] buffer, int offset, int count)
        {
            if(FValue != null)
                FValue.Read(buffer, offset, count);
            else
                buffer.ReadSilence(offset, count);
        }
    }
    
    public class SigParamBang : SigParam<bool>
    {
        public SigParamBang(string name, bool isOutput = false)
            : base(name, isOutput)
        {
        }
    }
    

    ///// <summary>
    ///// A secured out parameter
    ///// </summary>
    //public class SigParamSec<T> : SigParamBaseGeneric<T>
    //{
    //    public SigParamSec(string name, bool isOutput = false)
    //        : base(name, default(T), isOutput)
    //    {
    //    }
        
    //    public SigParamSec(string name, T initValue, bool isOutput = false)
    //        : base(name, initValue, isOutput)
    //    {
    //        Value = initValue;
    //    }

    //    public T Value
    //    {
    //        get
    //        {
    //            T ret;
    //            GetLatestValue(out ret);
    //            return ret;
    //        }
    //        set
    //        {
    //            SetLatestValue(value);
    //        }
    //    }
        
    //    private volatile bool FReading;
    //    private volatile bool FWriting;
    //    private T FValueToPass;
    //    private T FLastValue;
		
    //    bool GetLatestValue(out T value)
    //    {
    //        var success = false;
    //        FReading = true;
    //        if (!FWriting)
    //        {
    //            FLastValue = FValueToPass;
    //            success = true;
    //        }
    //        else
    //        {
    //            System.Diagnostics.Debug.WriteLine("Could not read");
    //        }
			
    //        value = FLastValue;
    //        FReading = false;
    //        return success;
    //    }
		
    //    bool SetLatestValue(T newValue)
    //    {
    //        var success = false;
    //        FWriting = true;
    //        if (!FReading)
    //        {
    //            FValueToPass = newValue;
    //            success = true;
    //        }
    //        else
    //        {
    //            System.Diagnostics.Debug.WriteLine("Could not write");
    //        }
    //        FWriting = false;
    //        return success;
    //    }
		
    //    public override object GetValue()
    //    {
    //        return Value;
    //    }
		
    //    public override void SetValue(object value)
    //    {
    //        Value = (T)value;
    //    }
    //}
}

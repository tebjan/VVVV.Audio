#region usings
using System;


#endregion usings

namespace VVVV.Audio
{
    public class AudioEngineSettings
    {
        private int FBufferSize;
        public int BufferSize
        {
            get
            {
                return FBufferSize;
            }
            
            set
            {
                if (FBufferSize != value)
                {
                    FBufferSize = value;
                    OnBufferSizeChanged();
                }
            }
        }
        
        public event EventHandler BufferSizeChanged;
        
        void OnBufferSizeChanged()
        {
            BufferSizeChanged?.Invoke(this, new EventArgs());
        }
        
        private int FSampleRate;
        public int SampleRate
        {
            get
            {
                return FSampleRate;
            }
            
            set
            {
                if(FSampleRate != value)
                {
                    FSampleRate = value;
                    OnSampleRateChanged();
                }
            }
        }
        
        public event EventHandler SampleRateChanged;
        
        void OnSampleRateChanged()
        {
            SampleRateChanged?.Invoke(this, new EventArgs());
        }
    }
}
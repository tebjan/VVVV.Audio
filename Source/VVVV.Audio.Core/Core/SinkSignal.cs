#region usings
using System;
using System.Linq;
using NAudio.Utils;
#endregion usings

namespace VVVV.Audio
{
    /// <summary>
    /// Interface which the audio engine will call for each buffer when registered
    /// </summary>
    public interface IAudioSink
    {
        void Read(int offset, int count);
    }
    
    /// <summary>
    /// Interface which the audio engine will call before the audio buffer will be processed.
    /// This should be used to do event stuff and automation which prepares for the next buffer.
    /// The Read method will be called in the normal buffer process
    /// </summary>
    public interface INotifyProcess : IAudioSink
    {
        void NotifyProcess(int count);
    }
    
    /// <summary>
    /// Base class for all sink signals which have audio input but no audio output
    /// </summary>
    public class SinkSignal : AudioSignalInput, IAudioSink
    {
        public SinkSignal()
        {
            AudioService.AddSink(this);
        }
        
        protected float[] FInternalBuffer;
        public virtual void Read(int offset, int count)
        {
            FInternalBuffer = BufferHelpers.Ensure(FInternalBuffer, count);
            base.Read(FInternalBuffer, offset, count);
        }
        
        public override void Dispose()
        {
            AudioService.RemoveSink(this);
            base.Dispose();
        }
    }
    
    /// <summary>
    /// Base class for all sink signals which have audio input but no audio output
    /// </summary>
    public class NotifyProccessSinkSignal : AudioSignal, INotifyProcess
    {
        public NotifyProccessSinkSignal()
        {
            AudioService.AddSink(this);
        }
        
        protected float[] FInternalBuffer;
        public void Read(int offset, int count)
        {
            FInternalBuffer = BufferHelpers.Ensure(FInternalBuffer, count);
            base.Read(FInternalBuffer, offset, count);
        }

        public virtual void NotifyProcess(int count)
        {
            //do something in sub class
        }
        
        public override void Dispose()
        {
            AudioService.RemoveSink(this);
            base.Dispose();
        }
    }
}

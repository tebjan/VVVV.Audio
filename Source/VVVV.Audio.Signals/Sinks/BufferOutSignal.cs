#region usings
using System;
#endregion
namespace VVVV.Audio
{
    
    public class BufferOutSignal : SinkSignal
    {
        public CircularBuffer Buffer = new CircularBuffer(512);
        
        public BufferOutSignal(AudioSignal input)
        {
            InputSignal.Value = input;
            Buffer.BufferFilled = BufferFilled;
        }
        
        public float[] BufferOut = new float[1];

        void BufferFilled(float[] buffer)
        {
            //copy the values from the circular buffer into the output array
            Array.Copy(buffer, 0, BufferOut, 0, Math.Min(BufferOut.Length, buffer.Length));
        }
        
        protected override void FillBuffer(float[] buffer, int offset, int count)
        {
            if(BufferOut.Length != Buffer.Size)
                BufferOut = new float[Buffer.Size];
            
            if (InputSignal.Value != null) 
            {
                InputSignal.Read(buffer, offset, count);
                Buffer.Write(buffer, offset, count);
            }
            else 
            {
                buffer.ReadSilence(offset, count);
                Buffer.Write(buffer, offset, count);
            }
        }
    }
}





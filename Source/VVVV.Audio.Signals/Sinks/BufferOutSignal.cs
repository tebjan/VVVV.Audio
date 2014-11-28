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
			FInput = input;
		}
		
		public float[] BufferOut = new float[1];
		
		protected override void FillBuffer(float[] buffer, int offset, int count)
		{
		    if(BufferOut.Length != Buffer.Size)
		        BufferOut = new float[Buffer.Size];
		    
			if (FInput != null) 
			{
				FInput.Read(buffer, offset, count);
				Buffer.Write(buffer, offset, count);
				Buffer.Read(BufferOut, 0, Buffer.Size);
			}
			else 
			{
			    buffer.ReadSilence(offset, count);
			    Buffer.Write(buffer, offset, count);
			    Buffer.Read(BufferOut, 0, Buffer.Size);
			}
		}
	}
}





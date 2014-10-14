#region usings
using System;
#endregion
namespace VVVV.Audio
{
	public class BufferOutSignal : SinkSignal<float[]>
	{
		public BufferOutSignal(AudioSignal input)
		{
			FInput = input;
		}

		protected override void FillBuffer(float[] buffer, int offset, int count)
		{
			if (FInput != null) {
				FInput.Read(buffer, offset, count);
				this.SetLatestValue((float[])buffer.Clone());
			}
			else {
				this.SetLatestValue(new float[1]);
			}
		}
	}
}





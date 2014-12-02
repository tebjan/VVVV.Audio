#region usings
using System;
#endregion
namespace VVVV.Audio
{
	public class LevelMeterSignal : SinkSignal
	{
		public LevelMeterSignal(AudioSignal input)
		{
			InputSignal.Value = input;
		}
		
		public double Max;

		protected override void FillBuffer(float[] buffer, int offset, int count)
		{
			if (InputSignal.Value != null) 
			{
				InputSignal.Read(buffer, offset, count);
				var max = 0.0;
				for (int i = offset; i < count; i++) 
				{
					max = Math.Max(max, Math.Abs(buffer[i]));
				}
				Max = max;
			}
		}
	}
}





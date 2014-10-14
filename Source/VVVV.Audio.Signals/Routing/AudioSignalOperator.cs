#region usings
using System;
using NAudio.Utils;
using VVVV.PluginInterfaces.V2;
#endregion
namespace VVVV.Audio
{
	public abstract class AudioSignalOperator : AudioSignal
	{
		public AudioSignalOperator()
		{
		}

		private ISpread<AudioSignal> FInputs;

		public ISpread<AudioSignal> Inputs {
			get {
				return FInputs;
			}
			set {
				FInputs = value;
			}
		}

		protected abstract void Operation(float[] accumulator, float[] operant, int offset, int count);

		private float[] FTempBuffer = new float[1];

		protected override void FillBuffer(float[] buffer, int offset, int count)
		{
			FTempBuffer = BufferHelpers.Ensure(FTempBuffer, count);
			if (FInputs != null && FInputs.SliceCount > 0) {
				bool first = true;
				for (int slice = 0; slice < FInputs.SliceCount; slice++) {
					if (FInputs[slice] != null) {
						if (first) {
							FInputs[slice].Read(buffer, offset, count);
							first = false;
						}
						else//rest
						 {
							FInputs[slice].Read(FTempBuffer, offset, count);
							Operation(buffer, FTempBuffer, offset, count);
						}
					}
				}
			}
		}
	}
	
		public class AudioSignalMultiplyOperator : AudioSignalOperator
	{
		protected override void Operation(float[] accumulator, float[] operant, int offset, int count)
		{
			for (int i = offset; i < count; i++)
			{
				accumulator[i] *= operant[i];
			}
		} 
	}
	
	public class AudioSignalAddOperator : AudioSignalOperator
	{
		protected override void Operation(float[] accumulator, float[] operant, int offset, int count)
		{
			for (int i = offset; i < count; i++)
			{
				accumulator[i] += operant[i];
			}
		}
	}
	
	public class AudioSignalSubtractOperator : AudioSignalOperator
	{
		protected override void Operation(float[] accumulator, float[] operant, int offset, int count)
		{
			for (int i = offset; i < count; i++)
			{
				accumulator[i] -= operant[i];
			}
		}
	}
}





/*
* Created by SharpDevelop.
* User: Tebjan Halm
* Date: 08.04.2012
* Time: 21:53
*
*/

using System;
using System.Runtime.CompilerServices;

using VVVV.Utils.VMath;
using NAudio.Wave;

namespace VVVV.Nodes
{
	/// <summary>
	/// Class for LUT playback
	/// </summary>
	public class LUTWaveProvider : IWaveProvider
	{
		public float[] LUT
		{
			get;
			private set;
		}
		
		public float[] LUTBuffer
		{
			get;
			private set;
		}
		
		public float Delta;
		
		private float FIndex;
		
		private WaveFormat waveFormat;
		public WaveFormat WaveFormat
		{
			get
			{
				return this.waveFormat;
			}
		}
		
		public LUTWaveProvider() : this(44100, 2)
		{
		}
		
		//constructor
		public LUTWaveProvider(int sampleRate, int channels)
		{
			this.SetWaveFormat(sampleRate, channels);
			LUT = new float[1024];
			LUTBuffer = new float[1024];
			DlyBufferSize = 2*MaxDlyTime;
			DlyBuffer = new float[DlyBufferSize];
		}
		
		public void SetWaveFormat(int sampleRate, int channels)
		{
			this.waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
		}
		
		public void SwapBuffers()
		{
			var tmp = LUT;
			LUT = LUTBuffer;
			LUTBuffer = tmp;
		}
		
		int i = 0;
		int j = 0;
		
		
		float fDlyTime = 0.25f;
		
		public float DelayTime
		{
			get { return fDlyTime; }
			set { fDlyTime = (float)VMath.Clamp(value, 0.0, 1.0); }
		}
		
		int MaxDlyTime = 44100*2;
		int DlyBufferSize;
		float[] DlyBuffer;
		public float DelayAmount;
		
		
		public unsafe int Read(byte[] buffer, int offset, int sampleCount)
		{
			
			var count = sampleCount / 4;
			var luts = LUT.Length;
			
			fixed(float* lut = LUT)
			{
				fixed(byte* outBuff = buffer)
				{
					var buf = (float*)outBuff;
					for (int n = 0; n < count; n+=2)
					{
						
						if( i >= DlyBufferSize ) i = 0;
						
						j = i - (int)(fDlyTime * MaxDlyTime);
						
						if( j < 0 ) j += DlyBufferSize;
						
						var round = (int)FIndex;
						var index = (int)Math.Floor(FIndex);
						var s1 = LUT[index%luts];
						//var s2 = LUT[(index + 1)%luts];
						
						buf[n+offset] = DlyBuffer [i] = s1 + DlyBuffer [j] * DelayAmount;
						buf[n+offset+1] = buf[n+offset]; //just copy to left
						FIndex = (FIndex + Delta)%luts;
						
						i++;
					}
				}
			}
			
			return sampleCount;
		}
	}
	
}

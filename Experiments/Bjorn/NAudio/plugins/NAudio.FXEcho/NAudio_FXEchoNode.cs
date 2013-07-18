#region usings
using System;
using System.ComponentModel.Composition;
using System.Collections.Generic;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using NAudio.Wave;

using VVVV.Core.Logging;
#endregion usings

namespace VVVV.Nodes
{
	#region PluginInfo
	[PluginInfo(Name = "Echo", Category = "NAudio.FX", Help = "Basic template with one value in/out", Tags = "", AutoEvaluate = true)]
	#endregion PluginInfo
	public class NAudio_FXEchoNode : IDisposable, IPluginEvaluate, ISampleProvider
	{
		#region fields & pins
		[Input("Input")]
		ISpread<ISampleProvider> FInStream;

		[Output("Output")]
		ISpread<ISampleProvider> FOutput;

		[Import()]
		ILogger FLogger;
		#endregion fields & pins

		private EffectStream effect = null;

		public NAudio_FXEchoNode()
		{
		}

		public void Dispose()
		{

		}
		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			FOutput.SliceCount = SpreadMax;

			if (effect != null) {
				if (FInStream[0] != effect.SourceStream) {
					effect.SourceStream = FInStream[0];
				}
			} else if (FInStream[0] != null) {
				effect = new EffectStream(FInStream[0], 20000, 0.5f);
			}
			if (FOutput[0] != this) {
				FOutput[0] = this;
			}
			//FLogger.Log(LogType.Debug, "hi tty!");
		}

		public WaveFormat WaveFormat {
			get { return WaveFormat.CreateIeeeFloatWaveFormat(44100, 2); }
		}

		public int Read(float[] buffer, int offset, int count)
		{
			return effect.Read(buffer, offset, count);
		}

		public class EffectStream : ISampleProvider
		{
			private Queue<float> samples;

			public int EchoLength { get; set; }

			public float EchoFactor { get; set; }
			public ISampleProvider SourceStream { get; set; }

			public EffectStream(ISampleProvider stream, int length, float factor)
			{
				this.SourceStream = stream;
				this.EchoLength = length;
				this.EchoFactor = factor;
				this.samples = new Queue<float>();

				for (int i = 0; i < length; i++) {
					this.samples.Enqueue(0f);
				}
			}
			public WaveFormat WaveFormat {
				get { return WaveFormat.CreateIeeeFloatWaveFormat(44100, 2); }
			}
			public int Read(float[] buffer, int offset, int count)
			{
				#region sample setup
				int read = SourceStream.Read(buffer, offset, count);

				for (int i = 0; i < read; i++) {
					float sample = buffer[i];

					#endregion

					#region FX processing


					samples.Enqueue(sample);
					sample = sample + EchoFactor * samples.Dequeue();

					//clip range
					sample = Math.Min(1f, Math.Max(-1f, sample));

					#endregion

					#region sample copy and exit
					buffer[i] = sample;

				}

				return read;
				#endregion
			}
		}
	}
}

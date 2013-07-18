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
	[PluginInfo(Name = "TestEffect", Category = "NAudio.FX", Help = "Basic template with one value in/out", Tags = "", AutoEvaluate = true)]
	#endregion PluginInfo
	public class NAudio_FXTestEffectNode : IDisposable, IPluginEvaluate, ISampleProvider
	{
		#region fields & pins
		[Input("Input")]
		ISpread<ISampleProvider> FInStream;

		[Input("c1", DefaultValue = 0.5f)]
		IDiffSpread<float> Fc1;

		[Input("c2", DefaultValue = 0.3f)]
		IDiffSpread<float> Fc2;

		[Input("c3", DefaultValue = 0.2f)]
		IDiffSpread<float> Fc3;

		[Output("Output")]
		ISpread<ISampleProvider> FOutput;

		[Import()]
		ILogger FLogger;
		#endregion fields & pins

		private EffectStream effect = null;

		public NAudio_FXTestEffectNode()
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
				if (Fc1.IsChanged)
					effect.c1 = Fc1[0];
				if (Fc2.IsChanged)
					effect.c2 = Fc2[0];
				if (Fc3.IsChanged)
					effect.c3 = Fc3[0];

				if (FInStream[0] != effect.SourceStream) {
					effect.SourceStream = FInStream[0];
				}
			} else if (FInStream[0] != null) {
				effect = new EffectStream(FInStream[0], 2000, 0.5f, Fc1[0], Fc2[0], Fc3[0]);
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
			private int channel;
			private Queue<float> samples;
			//max 8 channels...
			private float[] samples_t_minus_1;
			private float[] samples_t_minus_2;
			public float c1 { get; set; }
			public float c2 { get; set; }
			public float c3 { get; set; }

			public int EchoLength { get; set; }

			public float EchoFactor { get; set; }
			public ISampleProvider SourceStream { get; set; }

			public EffectStream(ISampleProvider stream, int length, float factor, float C1, float C2, float C3)
			{
				this.SourceStream = stream;
				this.channel = 0;
				this.EchoLength = length;
				this.EchoFactor = factor;
				this.samples = new Queue<float>();

				this.c1 = C1;
				this.c2 = C2;
				this.c3 = C3;

				samples_t_minus_1 = new float[8];
				samples_t_minus_2 = new float[8];
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

					channel = (channel + 1) % WaveFormat.Channels;



					#endregion

					#region FX processing


					sample = c1 * sample + c2 * samples_t_minus_1[channel] + c3 * samples_t_minus_2[channel];
					samples.Enqueue(sample);
					sample = sample * (1f - (EchoFactor / 2f)) + EchoFactor * samples.Dequeue();
					//STRANGERANDOM hack
					//float del = samples.Dequeue();
					//sample = sample + (sample % del) / 4 - del * 2;

					//clip range
					sample = Math.Min(1f, Math.Max(-1f, sample));
					samples_t_minus_2[channel] = samples_t_minus_1[channel];
					samples_t_minus_1[channel] = sample;

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

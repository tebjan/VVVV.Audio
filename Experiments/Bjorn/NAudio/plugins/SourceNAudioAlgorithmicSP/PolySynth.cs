using System;
using System.ComponentModel.Composition;
using System.Collections.Generic;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using NAudio.Wave;
using System.CodeDom.Compiler;
using System.Text;

namespace VVVV.Nodes
{
	public static class SynthHelper
	{
		public static double noteToFrequency(int note)
		{
			return 440 * 2 * Math.Exp((note - 69) / 12);
		}
	}

	public class polySynthAlgorithmic : WaveStream
	{
		private Type _generatorType;

		public double[] BufferedA;
		public double[] BufferedB;
		public double[] BufferedC;
		public double[] BufferedD;
		public int bufferCount;

		//fixedsize array to create objects at init time
		voice[] voices;

		//keep track of active voices inorder to not
		//have to iterate over the whole voice array
		LinkedList<int> activeVoices;

		ADSRStages adsr;

		public polySynthAlgorithmic(ADSRStages adsrInit, int maxVoices)
		{
			this.BufferedA = new double[320];
			this.BufferedB = new double[320];
			this.BufferedC = new double[320];
			this.BufferedD = new double[320];
			this.bufferCount = 0;

			this.adsr = adsrInit;
			activeVoices = new LinkedList<int>();

			voices = new voice[maxVoices];

			for (int note = 0; note < maxVoices; note++) {
				voices[note] = new voice(adsrInit, SynthHelper.noteToFrequency(note));
				voices[note].reset();

			}
		}

		//midinote
		public void noteOn(int note, double velocity)
		{
			if ((note < 0) || (note > 127))
				return;

			activeVoices.AddLast(note);
		}

		public void noteRelease(int note)
		{
			voices[note].released = true;
		}

		public void allNotesOff()
		{

		}

		public override long Position { get; set; }
		public override long Length {
			get { return long.MaxValue; }
		}
		public override WaveFormat WaveFormat {
			get {
				//!!!! change to float format
				WaveFormat wf = new WaveFormat(44100, 32, 2);
				//WaveFormat wf = new WaveFormat(8000, 8, 1);
				return wf;
			}
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			/*int samples = count;
			int index = 0;
			
			double accumulator = 0.0;
			
			double increment = 0.0;

			if(samples > 0)
			{
				increment = bufferCount / samples;
			}
			
			for(int i = 0; i < samples; i++)
			{
				index = (int) accumulator;
				
				if(_generatorType != null)
				{

				buffer[i] =  (byte) _generatorType.GetMethod("Compute").Invoke(null, new object[] {t, BufferedA[index], BufferedB[index], BufferedC[index], VMath.Lerp(BufferedD[index], BufferedD[index + 1], accumulator - index) });

				accumulator += increment;
				t++;
				}
				else
				{
					buffer[i] = 127;
				}
			}
			
			bufferCount = 0;
			*/
			return count;
		}

		//Copy&Pasted from: http://www.david-gouveia.com/one-line-algorithmic-music-in-xna/
		//and then adapted to this example
/*public void SetVariables(double A, double B, double C, double D)
		{
			a = A;
			b = B;
			c = C;
			d = D;
		}*/
				public string SetAlgorithm(string algorithm)
		{
			// Ignore if the user wrote nothing
			if (String.IsNullOrEmpty(algorithm)) {
				_generatorType = null;
				return "Empty";

			} else {
				// Create code string
				StringBuilder codeBuilder = new StringBuilder();
				codeBuilder.AppendLine("using System;");
				codeBuilder.AppendLine("namespace VVVV.Nodes {");
				codeBuilder.AppendLine("    public static class ComputeAlgo {");
				codeBuilder.AppendLine("    \tprivate static double Sin(long a) { return Math.Sin(a); }");
				codeBuilder.AppendLine("    \tprivate static double Sin(double a) { return Math.Sin(a); }");
				//codeBuilder.AppendLine("    	private static double Tone(double t, double freq) { return Sin(t/((8000/freq)/(Math.PI*2))); }");
				codeBuilder.AppendLine("    \tprivate static double Tone(double t, double freq) { return Sin((t * 2 * Math.PI * freq)/8000); }");
				codeBuilder.AppendLine("    \tprivate static double fmOp(long time, double volume, double pitch)\r\n        \t\t\t\t\t\t\t\t\t\t{ return volume*Math.Sin(((double)time)/((8000/pitch)/(Math.PI*2))); }");
				codeBuilder.AppendLine("        public static byte Compute(long t, double A, double B, double C, double D) { return (byte)(" + algorithm + "); }");
				codeBuilder.AppendLine("    }");
				codeBuilder.AppendLine("}");
				string code = codeBuilder.ToString();

				// Compile code string in memory
				CodeDomProvider codeDomProvider = CodeDomProvider.CreateProvider("C#");
				CompilerParameters compileParams = new CompilerParameters {
					GenerateExecutable = false,
					GenerateInMemory = true
				};
				CompilerResults compilerResults = codeDomProvider.CompileAssemblyFromSource(compileParams, new[] { code });

				// On error display message and exit
				if (compilerResults.Errors.HasErrors) {
					_generatorType = null;
					return GetErrorMessage(compilerResults);
				} else {
					// Otherwise store a Type reference to the assembly we compiled
					// Using reflection we can call the Generate method from this Type

					_generatorType = compilerResults.CompiledAssembly.GetType("VVVV.Nodes.ComputeAlgo");
					return "Ok";
				}
			}
		}

		private static string GetErrorMessage(CompilerResults compilerResults)
		{
			StringBuilder errorBuilder = new StringBuilder();
			foreach (CompilerError compilerError in compilerResults.Errors)
				errorBuilder.AppendLine(compilerError.ErrorText);
			return errorBuilder.ToString();
		}

	}
}

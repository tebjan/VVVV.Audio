#region usings
using System;
using System.ComponentModel.Composition;
using System.Collections.Generic;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;
using VVVV.Audio;

using NAudio.Wave;
using NAudio.Wave.Asio;
using NAudio.CoreAudioApi;
using NAudio.Wave.SampleProviders;
using NAudio.Utils;


using VVVV.Core.Logging;
#endregion usings

namespace VVVV.Nodes
{
	public class WaveRecorderSignal : SinkSignal<int>
	{
        WaveFileWriter FWriter;

		public WaveRecorderSignal()
		{
		}

        private string FFileName;

        public string Filename
        {
            get { return FFileName; }
            set 
            {
                if (!string.IsNullOrWhiteSpace(value) && FFileName != value)
                {
                    FFileName = value;
                    if (FWriter != null)
                    {
                        FWriter.Close();
                        FWriter.Dispose();
                    }
                    FWriter = new WaveFileWriter(FFileName, new WaveFormat(WaveFormat.SampleRate, 16, 1));
                    SamplesWritten = 0;
                }
            }
        }

        SampleToWaveProvider16 FWave16Provider;
        protected override void InputWasSet(AudioSignal newInput)
        {
            FWave16Provider = new SampleToWaveProvider16(newInput);
        }

        byte[] FByteBuffer = new byte[1];
		protected override void FillBuffer(float[] buffer, int offset, int count)
		{
            if (Write && FInput != null && FWriter != null)
            {
                var byteCount = count * 2;

                if (FByteBuffer.Length < byteCount)
                    FByteBuffer = new byte[byteCount];

                //read bytes from input
                FWave16Provider.Read(FByteBuffer, 0, byteCount);

                //write to disk
                FWriter.Write(FByteBuffer, 0, byteCount);

                SamplesWritten += count;
            }
		}

        public double SamplesWritten { get; protected set; }

        public bool Write { get; set; }

        public override void Dispose()
        {
            if (FWriter != null)
            {
                FWriter.Close();
                FWriter.Dispose();
                FWriter = null;
            }
            base.Dispose();
        }
    }
	
	
	[PluginInfo(Name = "Recorder", Category = "Audio", Version = "Sink", Help = "Records audio to disk", Tags = "Writer, File, Wave")]
	public class WaveRecorderNode : GenericAudioSinkNodeWithOutputs<WaveRecorderSignal, int>
	{

        [Input("Write")]
        IDiffSpread<bool> FWriteIn;

        [Input("Filename", DefaultString = "MyNextBigHit", StringType = StringType.Filename, FileMask = ".wav")]
        IDiffSpread<string> FNameIn;

        [Output("Samples Written")]
        ISpread<double> FLevelOut;

        protected override void SetOutputs(int i, WaveRecorderSignal instance)
        {
            FLevelOut[i] = instance.SamplesWritten;
        }

        protected override void SetOutputSliceCount(int sliceCount)
        {
            FLevelOut.SliceCount = sliceCount;
        }

        protected override WaveRecorderSignal GetInstance(int i)
        {
            return new WaveRecorderSignal();
        }

        protected override void SetParameters(int i, WaveRecorderSignal instance)
        {
            instance.Input = FInputs[i];
            instance.Filename = FNameIn[i];
            instance.Write = FWriteIn[i];
        }

        //dont forget to close the files and write the headers
        public override void Dispose()
        {
            foreach (var item in FSignals)
            {
                if (item != null)
                    item.Dispose();
            }
            base.Dispose();
        }
    }
}



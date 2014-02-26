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
                else
                {
                	SamplesWritten = 0;
                	FFlushCounter = 0;
                }
            }
        }

        SampleToWaveProvider16 FWave16Provider;
        protected override void InputWasSet(AudioSignal newInput)
        {
            FWave16Provider = new SampleToWaveProvider16(newInput);
        }

        byte[] FByteBuffer = new byte[1];
        int FFlushCounter = 0;
        bool FLastWriteState;
		protected override void FillBuffer(float[] buffer, int offset, int count)
		{
            if (Write && FInput != null && FWriter != null)
            {
                var byteCount = count * 2;

                if (FByteBuffer.Length < byteCount)
                    FByteBuffer = new byte[byteCount];

                //read bytes from input
                FWave16Provider.Read(FByteBuffer, 0, byteCount);

                //write to stream
                FWriter.Write(FByteBuffer, 0, byteCount);

                SamplesWritten += count;
                FFlushCounter += count;
                if(FFlushCounter >= 32768)
                {
                	FWriter.Flush();
                	FFlushCounter = 0;
                }
                FLastWriteState = true;
            }
            else
            {
            	FFlushCounter = 0;
            	if(FLastWriteState)
            	{
            		FWriter.Flush();
            		FLastWriteState = false;
            	}
            }
		}

        public int SamplesWritten { get; protected set; }
	
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
	
	
	[PluginInfo(Name = "Writer", Category = "VAudio", Version = "Sink", Help = "Records audio to disk", Tags = "file, wave, record")]
	public class WaveWriterNode : GenericAudioSinkNodeWithOutputs<WaveRecorderSignal, int>
	{

        [Input("Write")]
        public IDiffSpread<bool> FWriteIn;

        [Input("Filename", DefaultString = "", StringType = StringType.Filename, FileMask = ".wav")]
        public IDiffSpread<string> FNameIn;

        [Output("Samples Written")]
        public ISpread<int> FSamplesWrittenOut;

        protected override void SetOutputs(int i, WaveRecorderSignal instance)
        {
            FSamplesWrittenOut[i] = instance.SamplesWritten;
        }

        protected override void SetOutputSliceCount(int sliceCount)
        {
            FSamplesWrittenOut.SliceCount = sliceCount;
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



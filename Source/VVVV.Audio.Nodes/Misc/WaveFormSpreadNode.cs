#region usings
using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using VVVV.Audio;
using VVVV.Core.Logging;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

#endregion usings

namespace VVVV.Nodes
{
    
    
    #region PluginInfo
    [PluginInfo(Name = "WaveForm", Category = "Spreads", Help = "Gets a block max representation of an audio file", Tags = "VAudio")]
    #endregion PluginInfo
    public class WaveFormSpreadNode : GenericMultiAudioSourceNode<WaveFormSignal>
    {
        #region fields & pins
        [Input("Start Time")]
        public IDiffSpread<double> FLoopStart;
        
        [Input("End Time")]
        public IDiffSpread<double> FLoopEnd;

        [Input("Min Value", DefaultValue = 0.01)]
        public IDiffSpread<float> FMinValueIn;
        
        [Input("Convert to Mono")]
        public IDiffSpread<bool> FConvertToMonoIn;
        
        [Input("Spread Count", DefaultValue = 1)]
        public IDiffSpread<int> FSpreadCount;
        
        [Input("Filename", StringType = StringType.Filename, FileMask="Audio File (*.wav, *.mp3, *.aiff, *.m4a)|*.wav;*.mp3;*.aiff;*.m4a")]
        public IDiffSpread<string> FFilename;
        
        [Output("Wave Form")]
        public ISpread<ISpread<double>> FWaveFormOut;
        
        [Output("Duration")]
        public ISpread<double> FDurationOut;
        
        [Output("Sample Rate")]
        public ISpread<int> FSampleRateOut;

        [Output("Channels")]
        public ISpread<int> FChannelsOut;
        
        [Output("Uncompressed Format")]
        public ISpread<string> FFileFormatOut;
        #endregion fields & pins
        

        protected override PinVisibility GetOutputVisiblilty()
        {
            return PinVisibility.False;
        }
        
        protected override async void SetParameters(int i, WaveFormSignal instance)
        {
            if(FFilename.IsChanged)
            {
                instance.OpenFile(FFilename[i]);
                
                if (instance.FAudioFile == null)
                {
                    FDurationOut[i] = 0;
                    FSampleRateOut[i] = 0;
                    FChannelsOut[i] = 0;
                    FFileFormatOut[i] = "";
                }
                else
                {
                    var duration = instance.FAudioFile.TotalTime.TotalSeconds;
                    instance.StartTime = VMath.Clamp(FLoopStart[i], 0, duration);
                    instance.EndTime = VMath.Clamp(FLoopEnd[i], 0, duration);
                    
                    instance.Loop = instance.StartTime < instance.EndTime;
                    
                    SetOutputSliceCount(CalculatedSpreadMax);
                    
                    FDurationOut[i] = duration;
                    FChannelsOut[i] = instance.FAudioFile.OriginalFileFormat.Channels;
                    FSampleRateOut[i] = instance.FAudioFile.OriginalFileFormat.SampleRate;
                    FFileFormatOut[i] = instance.FAudioFile.OriginalFileFormat.ToString();
                }
                
            }
            
            if (instance.FAudioFile == null) return;
            
            if(FLoopStart.IsChanged || FLoopEnd.IsChanged)
            {
                var duration = instance.FAudioFile.TotalTime.TotalSeconds;
                instance.StartTime = VMath.Clamp(FLoopStart[i], 0, duration);
                instance.EndTime = VMath.Clamp(FLoopEnd[i], 0, duration);
                
                instance.Loop = instance.StartTime < instance.EndTime;
            }
            
            if(FSpreadCount.IsChanged)
            {
                instance.SpreadCount = FSpreadCount[i];
            }
            
            if(FMinValueIn.IsChanged)
            {
                instance.MinValue = FMinValueIn[i];
            }
            
            if(FConvertToMonoIn.IsChanged)
            {
                instance.ToMono = FConvertToMonoIn[i];
            }
            
            //do the calculation
            instance.ReadIntoSpreadAsync();
        }
        
        
        protected override void SetOutputSliceCount(int sliceCount)
        {
            
            FWaveFormOut.SliceCount = 0;
            
            FDurationOut.SliceCount = sliceCount;
            FChannelsOut.SliceCount = sliceCount;
            FSampleRateOut.SliceCount = sliceCount;
            FFileFormatOut.SliceCount = sliceCount;
        }
        
        protected override void SetOutputs(int i, WaveFormSignal instance)
        {
            if(instance.FAudioFile == null)
            {
                FWaveFormOut.Add(new Spread<double>(0));
            }
            else
            {
                for (int channel = 0; channel < instance.WaveFormSpread.SliceCount; channel++)
                {
                    FWaveFormOut.Add(instance.WaveFormSpread[channel]);
                }
            }
        }

        protected override WaveFormSignal GetInstance(int i)
        {
            return new WaveFormSignal();
        }
    }
    
}

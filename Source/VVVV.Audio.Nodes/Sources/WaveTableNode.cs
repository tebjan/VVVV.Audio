#region usings
using System;
using System.ComponentModel.Composition;
using System.Collections.Generic;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;
using VVVV.Audio;


using VVVV.Core.Logging;
#endregion usings

namespace VVVV.Nodes
{
    
    #region PluginInfo
    [PluginInfo(Name = "WaveTable", Category = "VAudio", Version = "Source", Help = "Generates an audio signal from a wave table", Tags = "LUT, Synthesis", AutoEvaluate = true)]
    #endregion PluginInfo
    public class WaveTableNode : GenericAudioSourceNode<WaveTableSignal>
    {
        #region fields & pins
        [Input("Table")]
        public IDiffSpread<ISpread<float>> FTableIn;
        
        [Input("Frequency", DefaultValue = 440)]
        public IDiffSpread<float> FFreqIn;
        
        [Input("Window Function")]
        public IDiffSpread<WindowFunction> FWindowFuncIn;    
    
        [Import()]
        public ILogger FLogger;
        
        float[] FWindow;
        
        #endregion fields & pins
        
        protected override int GetSpreadMax(int originalSpreadMax)
        {
            if(originalSpreadMax == 0) return 0;
            var max = Math.Max(FTableIn.SliceCount, FFreqIn.SliceCount);
            return max = Math.Max(max, FWindowFuncIn.SliceCount);
        }
        
        protected override void SetParameters(int i, WaveTableSignal instance)
        {
            SetParameters(i, instance, false);
        }
        
        protected void SetParameters(int i, WaveTableSignal instance, bool created)
        {
            
            instance.Frequency = FFreqIn[i];
        
            if(FTableIn.IsChanged || FWindowFuncIn.IsChanged || created)
            {
                var table = FTableIn[i];

                if(table.SliceCount != instance.LUTBuffer.Length)
                {
                    instance.LUTBuffer = new float[table.SliceCount];
                    FWindow = AudioUtils.CreateWindowFloat(instance.LUTBuffer.Length, FWindowFuncIn[i]);
                }

                //setup new window
                if(FWindowFuncIn.IsChanged || created)
                {
                    FWindow = AudioUtils.CreateWindowFloat(instance.LUTBuffer.Length, FWindowFuncIn[i]);
                }
                
                //FLogger.Log(LogType.Debug, "LUT");
                for(int j=0; j<instance.LUTBuffer.Length; j++)
                {
                    instance.LUTBuffer[j] = FTableIn[i][j] * FWindow[j];
                }
                
                instance.SwapBuffers();
            }
        }
        
        protected override WaveTableSignal GetInstance(int i)
        {
            var instance = new WaveTableSignal();
            SetParameters(i, instance, true);
            return instance;
        }
    }
}



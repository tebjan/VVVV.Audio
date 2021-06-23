#region usings
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

using VVVV.Audio;
using VVVV.Core.Logging;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

#endregion usings

namespace VVVV.Nodes
{    
    
    
    [PluginInfo(Name = "CreateBuffer", Category = "VAudio", Help = "Creates a buffer which can be used to write and read samples", AutoEvaluate = true, Tags = "record")]
    public class CreateBufferNode : IPluginEvaluate, IDisposable
    {
        #pragma warning disable 0649
        [Input("Buffer ID", DefaultString = "")]
        IDiffSpread<string> FNameIn;
        
        [Input("Size", DefaultValue = 1024)]
        IDiffSpread<int> FSizeIn;
    
        #pragma warning restore
        
        //called when data for any output pin is requested
        public void Evaluate(int SpreadMax)
        {
            if(FNameIn.IsChanged || FSizeIn.IsChanged)
            {
                var storage = AudioService.BufferStorage;
                for (int i = 0; i < FNameIn.SliceCount; i++)
                {
                    var key = FNameIn[i];
                    if (!string.IsNullOrEmpty(key))
                    {
                        if(storage.ContainsKey(key))
                        {
                            if(storage[key].Length != FSizeIn[i]) //resize?
                            {
                                storage.SetBuffer(key, new float[FSizeIn[i]]);
                            }
                        }
                        else
                        {
                            storage.SetBuffer(key, new float[FSizeIn[i]]);
                        }
                    }
                }
                
                UpdateEnum();
                
                //delete buffers?
                foreach (var key in storage.Keys) 
                {
                    if(!FNameIn.Contains(key))
                    {
                        storage.RemoveBuffer(key);
                    }
                }
            }
        }
        
        void UpdateEnum()
        {
            var bufferKeys = AudioService.BufferStorage.Keys.ToArray();
            
            if (bufferKeys.Length > 0)
            {
                EnumManager.UpdateEnum("AudioBufferStorageKeys", bufferKeys[0], bufferKeys);
            }
            else
            {
                bufferKeys = new string[]{"No Buffers Created yet"};
                EnumManager.UpdateEnum("AudioBufferStorageKeys", bufferKeys[0], bufferKeys);
            }
        }
        
        public void Dispose()
        {
            foreach (var key in FNameIn) 
            {
                AudioService.BufferStorage.Remove(key);
            }
            
        }
    }
}



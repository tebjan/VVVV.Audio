/*
 * Created by SharpDevelop.
 * User: TF
 * Date: 31.12.2014
 * Time: 19:35
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using VVVV.Utils.VMath;

namespace VVVV.Audio
{
    /// <summary>
    /// Description of ValueToAudioSignal.
    /// </summary>
    public class ValueToAudioSignal : AudioSignal
    {
        SigParam<float> FValue = new SigParam<float>("Value");
        SigParam<float> FAlpha = new SigParam<float>("Smoothing");
        
        public ValueToAudioSignal()
        {
        }
        
        float FLastValue;
        protected override void FillBuffer(float[] buffer, int offset, int count)
        {
            var alpha = (float)VMath.Clamp(FAlpha.Value, 0, 1);
            for (int i = 0; i < count; i++) 
            {
                buffer[i] = FLastValue = alpha * FLastValue + (1-alpha) * FValue.Value;
            }
        }
    }
    
}

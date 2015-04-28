/*
 * Created by SharpDevelop.
 * User: TF
 * Date: 31.12.2014
 * Time: 19:35
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;

namespace VVVV.Audio
{
    /// <summary>
    /// Description of ValueToAudioSignal.
    /// </summary>
    public class ValueToAudioSignal : AudioSignal
    {
        SigParam<float> FValue = new SigParam<float>("Value");
        
        public ValueToAudioSignal()
        {
        }
        
        protected override void FillBuffer(float[] buffer, int offset, int count)
        {
            for (int i = 0; i < count; i++) 
            {
                buffer[i] = FValue.Value;
            }
        }
    }
}

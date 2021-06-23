/*
 * Created by SharpDevelop.
 * User: TF
 * Date: 28.04.2015
 * Time: 02:35
 * 
 */
using System;
using VVVV.Audio.Utils;

namespace VVVV.Audio
{
    /// <summary>
    /// Description of ValueToAudioSignal.
    /// </summary>
    public class OnePoleLowPassSignal : AudioSignal
    {
        SigParamAudio FAudioIn = new SigParamAudio("Input");
        SigParam<float> FAlpha = new SigParam<float>("Alpha");

        public OnePoleLowPassSignal()
        {
        }

        float FLastValue;
        
        protected override void FillBuffer(float[] buffer, int offset, int count)
        {
            var alpha = (float)MathUtils.Clamp(FAlpha.Value, 0, 1);
            FAudioIn.Read(buffer, offset, count);
            for (int i = 0; i < count; i++) 
            {
                buffer[i] = FLastValue = alpha * FLastValue + (1-alpha) * buffer[i];
            }
        }
    }
}



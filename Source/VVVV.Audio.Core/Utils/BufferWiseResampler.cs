/*
 * Created by SharpDevelop.
 * User: TF
 * Date: 21.07.2014
 * Time: 18:40
 * 
 */


using System;
namespace VVVV.Audio
{
    public class BufferWiseResampler
    {
        
        public void Resample(float[] source, float[] dest)
        {
            var factor = source.Length / (double)dest.Length;
            
            for (int i = 0; i < dest.Length; i++)
            {
                var index = (int)Math.Truncate(i*factor);
                dest[i] = source[index];
            }
        }
        
        public void ResampleChannel(float[] source, float[] dest, int sourceSamples, int destSamples, int channel, int totalChannels)
        {
            var factor = sourceSamples / (double)(destSamples);
            
            for (int i = 0; i < destSamples; i++)
            {
                var index = (int)Math.Min(i*factor, sourceSamples) ;
                dest[i] = source[channel + index*totalChannels];
            }
        }

        public void ResampleDeinterleave(float[] source, float[][] dest, int sourceSamples, int destSamples, int channels)
        {
        
            for (int i = 0; i < channels; i++)
            {
                ResampleChannel(source, dest[i], sourceSamples, destSamples, i, channels);
            }
        }
    }
    
}
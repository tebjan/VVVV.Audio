/*
 * Created by SharpDevelop.
 * User: TF
 * Date: 24.06.2015
 * Time: 02:35
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using FlareTic.Core.Audio;

namespace VVVV.Audio
{
    public class GistSignal : SinkSignal
    {
        Gist FGist;
        
        void InitGist()
        {
            if(FGist != null)
            {
                FGist.Dispose();
            }
            
            FGist = new Gist(SampleRate, BufferSize);
        }
        
        protected CircularBuffer FRingBuffer = new CircularBuffer(512);
        
        SigParam<float[]> FFT = new SigParam<float[]>("Spectrum", true);
        
        protected override void Engine_SampleRateChanged(object sender, EventArgs e)
        {
            base.Engine_SampleRateChanged(sender, e);
            
            InitGist();
        }
        
        protected override void Engine_BufferSizeChanged(object sender, EventArgs e)
        {
            base.Engine_BufferSizeChanged(sender, e);
            
            InitGist();
        }
        
        protected override void FillBuffer(float[] buffer, int offset, int count)
        {
            base.FillBuffer(buffer, offset, count);
        }
        
        public override void Dispose()
        {
            if(FGist != null)
            {
                FGist.Dispose();
            }
            
            base.Dispose();
        }
    }
}
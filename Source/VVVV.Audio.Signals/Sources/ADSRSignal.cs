#region usings
using System;
using VVVV.Audio.Utils;

#endregion
namespace VVVV.Audio
{
    public class ADSRSignal : AudioSignal
    {
        enum EnvelopStage
        {
            Off = 0,
            Attack,
            Decay,
            Sustain,
            Release
        }
        
        //inputs
        SigParamAudio Enable = new SigParamAudio("Trigger");
        SigParam<float> Attack = new SigParam<float>("Attack", 0.1f);
        SigParam<float> Decay = new SigParam<float>("Decay", 0.1f);
        SigParam<float> Sustain = new SigParam<float>("Sustain", 0.1f);
        SigParam<float> Slope = new SigParam<float>("Slope", 0.0f);
        SigParam<float> Release = new SigParam<float>("Release", 0.5f);
        SigParam<float> Min = new SigParam<float>("Min", 0.0f);
        SigParam<float> Max = new SigParam<float>("Max", 1.0f);
        
        //output
        SigParam<int> CurrentStage = new SigParam<int>("Current Stage", true);
        
        public ADSRSignal()
        {
        }
        
        double CalcExpCoeff(double levelBegin, double levelEnd, int releaseTime) 
        {
            return 1.0 + (Math.Log(levelEnd) - Math.Log(levelBegin)) / Math.Max(0, releaseTime);
        }
        
        int CalcExpSamples(double levelBegin, double levelEnd, double multiplier) 
        {
            return (int)Math.Abs((Math.Log(levelEnd) - Math.Log(levelBegin)) / multiplier);
        }
        
        double FMultiplier;
        double FCurrentLevel;
        EnvelopStage FCurrentStage;
        int FCurrentSampleIndex;
        int FNextStageSampleIndex;
        const double CMinimumLevel = 0.0001;
        void EnterStage(EnvelopStage newStage) 
        {
            FCurrentStage = newStage;
            CurrentStage.Value = (int)FCurrentStage;
            FCurrentSampleIndex = 0;

            switch (FCurrentStage)
            {
                case EnvelopStage.Off:
                    FNextStageSampleIndex = 0;
                    FCurrentLevel = 0.0;
                    FMultiplier = 1.0;
                    break;
                case EnvelopStage.Attack:
                    FNextStageSampleIndex = (int)(Math.Max(Attack.Value, 0.0f) * SampleRate);
                    FCurrentLevel = CMinimumLevel;
                    FMultiplier = CalcExpCoeff(FCurrentLevel, 1.0, FNextStageSampleIndex);
                    break;
                case EnvelopStage.Decay:
                    FNextStageSampleIndex = (int)(Decay.Value * SampleRate);
                    FCurrentLevel = 1.0;
                    FMultiplier = CalcExpCoeff(FCurrentLevel, Math.Max(Math.Max(Sustain.Value, 0.0f), CMinimumLevel), FNextStageSampleIndex);
                    break;
                case EnvelopStage.Sustain:
                    FCurrentLevel = Sustain.Value;
                    FMultiplier = 1 + Slope.Value * 0.0001;
                    FNextStageSampleIndex = 0;
                    break;
                case EnvelopStage.Release:                    
                    FNextStageSampleIndex = (int)(Math.Max(Release.Value, 0.0f) * SampleRate);
                    // We could go from ATTACK/DECAY to RELEASE,
                    // so we're not changing currentLevel here.
                    FMultiplier = CalcExpCoeff(FCurrentLevel, CMinimumLevel, FNextStageSampleIndex);
                    break;
            }
        }
        
        float[] FEnableBuffer = new float[1];
        float FLastEnabled;
        float FEnabledLevel;
        protected override void FillBuffer(float[] buffer, int offset, int count)
        {
            if (FEnableBuffer.Length < count) 
            {
                FEnableBuffer = new float[count];
            }
            
            Enable.Read(FEnableBuffer, offset, count);
            
            
            for(int i=0; i < count; i++)
            {
                var enabled = FEnableBuffer[i];
                if(enabled != FLastEnabled)
                {
                    if(enabled > 0 && FLastEnabled <= 0.0f)
                    {
                        EnterStage(EnvelopStage.Attack);
                        FEnabledLevel = enabled;
                    }
                    else if (enabled <= 0.0f)
                    {
                        EnterStage(EnvelopStage.Release);
                    }
                }
                
                
                if(FCurrentStage == EnvelopStage.Sustain)
                {
                    FCurrentLevel *= FMultiplier;
                    FCurrentLevel = MathUtils.Clamp(FCurrentLevel, 0, 1);
                }
                else if (FCurrentStage != EnvelopStage.Off)
                {
                    if (FCurrentSampleIndex == FNextStageSampleIndex)
                    {
                        var newStage = (EnvelopStage) (((int)FCurrentStage + 1) % 5);
                        EnterStage(newStage);
                    }
                    FCurrentLevel *= FMultiplier;
                    FCurrentSampleIndex++;
                }
                
                buffer[i] = (float)(FCurrentLevel * FEnabledLevel * (Max.Value - Min.Value) + Min.Value);
                
                FLastEnabled = enabled;
            }
        }
    }
}





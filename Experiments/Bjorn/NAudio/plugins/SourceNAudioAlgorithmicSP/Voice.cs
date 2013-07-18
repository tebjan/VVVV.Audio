using System;
using System.ComponentModel.Composition;
using System.Collections.Generic;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

namespace VVVV.Nodes
{

	public enum ADSRState
	{
		FREE,
		A,
		D,
		S,
		R
	}

	public struct ADSRStage
	{
		public int ticks;
		//time?
		public double amplitude;
	}

	public struct ADSRStages
	{
		public ADSRStage aStage;
		public ADSRStage dStage;
		public ADSRStage sStage;
		public ADSRStage rStage;
	}
	public class voice
	{
		long t;
		double frequency;
		double envelope;

		double velocity;

		ADSRStages adsr;


		public ADSRState state { get; set; }

		public bool released { get; set; }


		public voice(ADSRStages adsrInit, double freq)
		{
			this.adsr = adsrInit;
			this.frequency = freq;

			this.state = ADSRState.FREE;
		}

		public void reset()
		{
			state = ADSRState.FREE;
			t = 0;
			envelope = 0;
			velocity = 0;
			released = false;
		}

		public void update(out long tOut, out double envOut)
		{
			tOut = t;
			envOut = envelope;

			t++;

			switch (state) {
				//case ADSRState.FREE:
				//	break;

				case ADSRState.A:
					envelope = VMath.Map(t, 0, adsr.aStage.ticks, 0.0, 1.0, TMapMode.Clamp);
					if (envelope >= 1.0) {
						state = ADSRState.D;
					}
					break;
				case ADSRState.D:
					envelope = VMath.Map(t, 0, adsr.dStage.ticks, 0.0, 1.0, TMapMode.Clamp);
					if (envelope >= 1.0) {
						state = ADSRState.S;
					}
					break;
				case ADSRState.S:
					envelope = VMath.Map(t, 0, adsr.sStage.ticks, 0.0, 1.0, TMapMode.Clamp);
					if (envelope >= 1.0) {
						state = ADSRState.R;
					}
					break;
				case ADSRState.R:
					envelope = VMath.Map(t, 0, adsr.rStage.ticks, 0.0, 1.0, TMapMode.Clamp);
					if (envelope >= 1.0) {
						state = ADSRState.FREE;
						t = 0;
					}
					break;
			}
		}
	}
}

#region usings
using System;
using System.Collections.Generic;
using System.Linq;
using VVVV.PluginInterfaces.V2;
#endregion
namespace VVVV.Audio
{
    public class MultiChannelInputSignal : MultiChannelSignal
    {
		/// <summary>
		/// The input signal
		/// </summary>
		public IReadOnlyList<AudioSignal> Input {
			get {
				return FInput;
			}
			set {
				if (FInput != value) {
					FInput = value;
					InputWasSet(value);
				}
			}
		}

		/// <summary>
		/// Override in sub class to know when the input has changed
		/// </summary>
		/// <param name="newInput"></param>
		protected virtual void InputWasSet(IReadOnlyList<AudioSignal> newInput)
		{
		}

		protected IReadOnlyList<AudioSignal> FInput;
    }
}





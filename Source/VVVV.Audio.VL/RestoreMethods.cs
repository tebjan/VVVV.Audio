using VL.Core;
using VL.Lib.VAudio;
using VVVV.Audio.Signals;

namespace VL.Lib
{
    // Importer will look for static VL.Lib.RestoreMethods in each assembly in order to setup
    // restore methods used by runtime to do state restoration.
    public static class RestoreMethods
    {

        public static AudioSignalRegion<TNew, TInNew, TOutNew> Restore<TOld, TNew, TInOld, TInNew, TOutOld, TOutNew>(AudioSignalRegion<TOld, TInOld, TOutOld> value, IStateRestorer restorer)
            where TOld : class
            where TNew : class
        {
            TNew newState = (TNew)restorer.Restore(value.State, typeof(TNew));
            var newRenderer = new AudioSignalRegion<TNew, TInNew, TOutNew>(value.PerBufferSignal ?? new BufferCallerSignal());
            newRenderer.State = newState;
            value.Dispose();
            return newRenderer;
        }

        public static AudioBufferLoop<TNew, TInNew> Restore<TOld, TNew, TInOld, TInNew>(AudioBufferLoop<TOld, TInOld> value, IStateRestorer restorer)
        {
            var newState = (TNew)restorer.Restore(value.State, typeof(TNew));
            var newLoop = new AudioBufferLoop<TNew, TInNew>(value.SampleClock);
            newLoop.State = newState;
            value.Dispose();
            return newLoop;
        }

    }
}

using System;
using static System.Math;

namespace VVVV.Audio.Utils
{
    public sealed class MathUtils
    {
        /// <summary>
        /// Clamp function, clamps a floating point value into the range [min..max]
        /// </summary>
        /// <param name="x"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static float Clamp(float x, float min, float max)
        {
            float minTemp = Min(min, max);
            float maxTemp = Max(min, max);
            return Min(Max(x, minTemp), maxTemp);
        }

        /// <summary>
        /// Clamp function, clamps a floating point value into the range [min..max]
        /// </summary>
        /// <param name="x"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static double Clamp(double x, double min, double max)
        {
            double minTemp = Min(min, max);
            double maxTemp = Max(min, max);
            return Min(Max(x, minTemp), maxTemp);
        }
    }
}

using System;

namespace VVVV.Audio.Utils
{
    public sealed class MathUtils
    {
        /// <summary>
        /// Min function
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns>Smaller value of the two input parameters</returns>
        public static float Min(float a, float b)
        {
            return a < b ? a : b;
        }

        /// <summary>
        /// Max function
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns>Greater value of the two input parameters</returns>
        public static float Max(float a, float b)
        {
            return a > b ? a : b;
        }

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
            double minTemp = Math.Min(min, max);
            double maxTemp = Math.Max(min, max);
            return Math.Min(Math.Max(x, minTemp), maxTemp);
        }
    }
}

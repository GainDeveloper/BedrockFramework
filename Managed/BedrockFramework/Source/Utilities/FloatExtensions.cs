using UnityEngine;

namespace BedrockFramework.Utilities
{
    public static class FloatExtensions
    {
		public static float Remap (this float value, float from1, float to1, float from2, float to2) {
			return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
		}

        public static int Wrap(this int x, int x_min, int x_max)
        {
            return (((x - x_min) % (x_max - x_min)) + (x_max - x_min)) % (x_max - x_min) + x_min;
        }
    }
}
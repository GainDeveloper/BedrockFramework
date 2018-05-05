using UnityEngine;
using System;

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

        public static float Wrap(this float x, float x_min, float x_max)
        {
            return (((x - x_min) % (x_max - x_min)) + (x_max - x_min)) % (x_max - x_min) + x_min;
        }

        //
        // Float to byte mapping.
        //

        public static byte ZeroOneToByte(this float x)
        {
            return (byte)(((x / 1f) * 255));
        }

        public static byte MinusOneOneToByte(this float x)
        {
            return (byte)(((x / 1f) * 127) + 127);
        }

        public static float ZeroOneToFloat(this byte x)
        {
            return ((float)x / 255);
        }

        public static float MinusOneOneToFloat(this byte x)
        {
            return (((float)x - 127) / 127);
        }
    }
}
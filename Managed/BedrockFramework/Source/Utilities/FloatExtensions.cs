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

        public static byte[] ToByteArray(this bool[] bools)
        {
            byte[] byteArray = new byte[BoolArraySizeToByteArraySize(bools.Length)];

            for (int i = 0; i < bools.Length; i++)
            {
                int currentByte = (i / 8);
                int currentByteIndex = (i % 8);

                if (bools[i])
                    byteArray[currentByte] |= (byte)(1 << (7 - currentByteIndex));
            }

            return byteArray;
        }

        public static bool[] ToBoolArray(this byte[] bytes)
        {
            bool[] boolArray = new bool[bytes.Length * 8];


            for (int bIndex = 0; bIndex < bytes.Length; bIndex++)
            {
                int baseBoolIndex = (bIndex + 1) * 8;

                for (int i = 0; i < 8; i++)
                    boolArray[baseBoolIndex - i - 1] = (bytes[bIndex] & (1 << i)) == 0 ? false : true;
            }

            return boolArray;
        }

        public static int BoolArraySizeToByteArraySize(this int boolCount)
        {
            return Mathf.CeilToInt((float)boolCount / 8);
        }

        public static bool ArrayContainsValue<T> (this T[] array, T value) where T : IEquatable<T>
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i].Equals(value))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
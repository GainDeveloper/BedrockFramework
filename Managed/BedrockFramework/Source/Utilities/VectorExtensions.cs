using UnityEngine;

namespace BedrockFramework.Utilities
{
    public static class VectorExtensions
    {
        public static Vector3 Round (this Vector3 vector, int decimalPoint = 1)
        {
            vector *= decimalPoint;
            vector = new Vector3(Mathf.Round(vector.x), Mathf.Round(vector.y), Mathf.Round(vector.z));
            return vector / decimalPoint;
        }
    }
}
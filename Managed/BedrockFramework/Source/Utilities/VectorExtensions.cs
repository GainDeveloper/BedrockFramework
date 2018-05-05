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

        public static byte[] Vector3ToByteArray (this Vector3 vector)
        {
            byte[] vector3Bytes = new byte[3];
            vector3Bytes[0] = vector.x.MinusOneOneToByte();
            vector3Bytes[1] = vector.y.MinusOneOneToByte();
            vector3Bytes[2] = vector.z.MinusOneOneToByte();
            return vector3Bytes;
        }

        public static void ByteArrayToVector3 (this byte[] array, out Vector3 vector)
        {
            vector.x = (float)array[0].MinusOneOneToFloat();
            vector.y = (float)array[1].MinusOneOneToFloat();
            vector.z = (float)array[2].MinusOneOneToFloat();
        }

        public static Vector3 ByteArrayToVector3(this byte[] array)
        {
            Vector3 toReturn;
            ByteArrayToVector3(array, out toReturn);
            return toReturn;
        }
    }
}
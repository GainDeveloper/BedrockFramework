using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace BedrockFramework.Utilities
{
    public static class SerializedPropertyExtensions
    {

        public static bool ArrayContains(this SerializedProperty sp, Object obj)
        {
            return ArrayIndexOf(sp, obj) > -1;
        }

        public static void ArrayDeleteElement(this SerializedProperty sp, Object obj)
        {
            int i = ArrayIndexOf(sp, obj);

            if (i > -1)
            {
                sp.DeleteArrayElementAtIndex(i);
                sp.DeleteArrayElementAtIndex(i);
            }
        }

        public static void ArrayAddElement(this SerializedProperty sp, Object obj)
        {
            int i = sp.arraySize;

            sp.InsertArrayElementAtIndex(i);
            sp.GetArrayElementAtIndex(i).objectReferenceValue = obj;
        }

        public static int ArrayIndexOf(this SerializedProperty sp, Object obj)
        {
            for (int i = 0; i < sp.arraySize; i++)
            {
                if (sp.GetArrayElementAtIndex(i).objectReferenceValue == obj)
                    return i;
            }
            return -1;
        }

    }
}
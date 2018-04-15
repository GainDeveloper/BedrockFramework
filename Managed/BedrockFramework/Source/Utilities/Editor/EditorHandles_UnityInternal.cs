/********************************************************           
BEDROCKFRAMEWORK : https://github.com/GainDeveloper/BedrockFramework
From : https://gist.github.com/MattRix/9205bc62d558fef98045

Exposes Unity's mesh intersection tests.
********************************************************/

using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Reflection;

[InitializeOnLoad]
public static class EditorHandles_UnityInternal {
    static Type type_HandleUtility;
    static MethodInfo meth_IntersectRayMesh;
 
    static EditorHandles_UnityInternal() {
        var editorTypes = typeof(Editor).Assembly.GetTypes();
 
        type_HandleUtility = editorTypes.FirstOrDefault(t => t.Name == "HandleUtility");
        meth_IntersectRayMesh = type_HandleUtility.GetMethod("IntersectRayMesh",
                                                              BindingFlags.Static | BindingFlags.NonPublic);
    }
 
    public static bool IntersectRayMesh(Ray ray, MeshFilter meshFilter, out RaycastHit hit) {
        return IntersectRayMesh(ray, meshFilter.mesh, meshFilter.transform.localToWorldMatrix, out hit);
    }
 
    public static bool IntersectRayMesh(Ray ray, Mesh mesh, Matrix4x4 matrix, out RaycastHit hit) {
        var parameters = new object[] { ray, mesh, matrix, null };
        bool result = (bool)meth_IntersectRayMesh.Invoke(null, parameters);
        hit = (RaycastHit)parameters[3];
        return result;
    }
}
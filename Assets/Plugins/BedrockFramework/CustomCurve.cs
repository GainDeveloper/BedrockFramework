using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

namespace BedrockFramework.CustomLine
{
    public class CustomCurve : MonoBehaviour
    {
        [Serializable]
        public struct CurvePoint
        {
            public Vector3 curvePoint;
            public Vector3 curveTangent;

            public CurvePoint(Vector3 pos, Vector3 tang)
            {
                curvePoint = pos;
                curveTangent = tang;
            }
        }

        public CurvePoint[] points;

        public void Reset()
        {
            points = new CurvePoint[] {
            new CurvePoint(new Vector3(1f, 0f, 0f), new Vector3(1f, 0f, 0f)),
            new CurvePoint(new Vector3(5f, 0f, 0f), new Vector3(1f, 0f, 0f)),
            };

            cachedDistanceToT = null;
        }

        public int CurveCount
        {
            get
            {
                return (points.Length - 1);
            }
        }


        private SortedList<float, float> cachedDistanceToT;
        public void CacheDistanceToT(int stepsPerCurve = 10)
        {
            cachedDistanceToT = new SortedList<float, float>();

            int numSteps = (stepsPerCurve * CurveCount);

            float tStepSize = 1f / numSteps;
            float length = 0;

            Vector3 lastPos = Vector3.zero;
            for (int i = 0; i < numSteps + 1; i++)
            {
                float t = i * tStepSize;

                Vector3 iPos = GetPoint(t);

                if (i != 0)
                    length += Vector3.Distance(lastPos, iPos);
                lastPos = iPos;

                cachedDistanceToT[length] = t;
            }
        }

        public float DistanceToT(float distace)
        {
            if (cachedDistanceToT == null)
                CacheDistanceToT();

            //TODO: Maybe don't need a sorted list. Use a tuple in a list and find any value above distance. Use this and find the previous then lerp between them.

            return 0;
        }

        public float CurveLength()
        {
            if (cachedDistanceToT == null)
                CacheDistanceToT();
            return cachedDistanceToT.Keys.Last();
        }

        public Vector3 GetPoint(float t)
        {
            int i;
            if (t >= 1f)
            {
                t = 1f;
                i = points.Length - 2;
            }
            else {
                t = Mathf.Clamp01(t) * CurveCount;
                i = (int)t;
                t -= i;
            }
            return transform.TransformPoint(Bezier.GetPoint(points[i].curvePoint, points[i].curvePoint + points[i].curveTangent, points[i+1].curvePoint - points[i+1].curveTangent, points[i+1].curvePoint, t));
        }

        public Vector3 GetVelocity(float t)
        {
            int i;
            if (t >= 1f)
            {
                t = 1f;
                i = points.Length - 2;
            }
            else {
                t = Mathf.Clamp01(t) * CurveCount;
                i = (int)t;
                t -= i;
            }
            return transform.TransformPoint(Bezier.GetFirstDerivative(points[i].curvePoint, points[i].curvePoint + points[i].curveTangent, points[i+1].curvePoint - points[i+1].curveTangent, points[i+1].curvePoint, t)) -
                transform.position;
        }

        public Vector3 GetDirection(float t)
        {
            return GetVelocity(t).normalized;
        }

        public Vector3 GetNormal(float t)
        {
            Vector3 tangent = GetDirection(t);
            Vector3 biNormal = Vector3.Cross(Vector3.up, tangent).normalized;
            return Vector3.Cross(tangent, biNormal);
        }

        public CurvePoint NewCurvePoint()
        {
            CurvePoint point = points[points.Length - 1];
            return new CurvePoint(point.curvePoint + point.curveTangent * 0.5f, point.curveTangent);
        }

        public static class Bezier
        {
            public static Vector3 GetPoint(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
            {
                t = Mathf.Clamp01(t);
                float oneMinusT = 1f - t;
                return
                    oneMinusT * oneMinusT * oneMinusT * p0 +
                    3f * oneMinusT * oneMinusT * t * p1 +
                    3f * oneMinusT * t * t * p2 +
                    t * t * t * p3;
            }

            public static Vector3 GetFirstDerivative(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
            {
                t = Mathf.Clamp01(t);
                float oneMinusT = 1f - t;
                return
                    3f * oneMinusT * oneMinusT * (p1 - p0) +
                    6f * oneMinusT * t * (p2 - p1) +
                    3f * t * t * (p3 - p2);
            }


        }
    }
}

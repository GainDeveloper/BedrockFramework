using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

namespace BedrockFramework.CustomLine
{
    [EditorOnlyComponent, DisallowMultipleComponent]
    [AddComponentMenu("BedrockFramework/Curve")]
    public class CustomCurve : MonoBehaviour
    {
        public CurvePoint[] points;
        private DistaceT[] cachedDistanceToT;

        public Action OnCurveModified = delegate {};

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

        struct DistaceT
        {
            public float distance, t;

            public DistaceT(float newDistance, float newT)
            {
                distance = newDistance;
                t = newT;
            }
        }

        public void Reset()
        {
            points = new CurvePoint[] {
            new CurvePoint(new Vector3(1f, 0f, 0f), new Vector3(1f, 0f, 0f)),
            new CurvePoint(new Vector3(5f, 0f, 0f), new Vector3(1f, 0f, 0f)),
            };

            CurveModified();
        }

        /// 
        /// 
        /// 

        public int CurveCount
        {
            get
            {
                return (points.Length - 1);
            }
        }

        public float IndexToT(int index)
        {
            if (index == 0)
                return 0;

            return (float)index / (points.Length - 1);
        }


        public void CurveModified()
        {
            CacheDistanceToT();
            OnCurveModified();
        }

        /// 
        /// 
        /// 

        public void CacheDistanceToT(int stepsPerCurve = 10)
        {
            int numSteps = (stepsPerCurve * CurveCount);
            cachedDistanceToT = new DistaceT[numSteps + 1];

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

                cachedDistanceToT[i] = new DistaceT(length, t);
            }
        }

        /// 
        /// 
        /// 

        public float DistanceToT(float distance)
        {
            if (cachedDistanceToT == null)
                CacheDistanceToT();

            int startIndex = cachedDistanceToT.Length - 2;

            for (int i = 1; i < cachedDistanceToT.Length-1; i++)
            {
                if (cachedDistanceToT[i].distance > distance)
                {
                    startIndex = i - 1;
                    break;
                }
                    
            }

            float distanceDiff = cachedDistanceToT[startIndex + 1].distance - cachedDistanceToT[startIndex].distance;
            float tLerpValue = (distance - cachedDistanceToT[startIndex].distance) / distanceDiff;

            return Mathf.Lerp(cachedDistanceToT[startIndex].t, cachedDistanceToT[startIndex + 1].t, tLerpValue);
        }

        public float CurveLength()
        {
            if (cachedDistanceToT == null)
                CacheDistanceToT();
            return cachedDistanceToT[cachedDistanceToT.Length - 1].distance;
        }

        public Vector3 GetPoint(float t, bool worldSpace = true)
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

            Vector3 pos = Bezier.GetPoint(points[i].curvePoint, points[i].curvePoint + points[i].curveTangent, points[i + 1].curvePoint - points[i + 1].curveTangent, points[i + 1].curvePoint, t);

            if (!worldSpace)
                return pos;

            return transform.TransformPoint(pos);
        }

        public Vector3 GetVelocity(float t, bool worldSpace = true)
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

            Vector3 velocity = Bezier.GetFirstDerivative(points[i].curvePoint, points[i].curvePoint + points[i].curveTangent, points[i + 1].curvePoint - points[i + 1].curveTangent, points[i + 1].curvePoint, t);

            if (!worldSpace)
                return velocity;

            return transform.TransformPoint(velocity) -
                transform.position;
        }

        public Vector3 GetDirection(float t, bool worldSpace = true)
        {
            return GetVelocity(t, worldSpace).normalized;
        }

        public Vector3 GetNormal(float t, bool worldSpace = true)
        {
            Vector3 tangent = GetDirection(t, worldSpace);
            Vector3 biNormal = Vector3.Cross(Vector3.up, tangent).normalized;
            return Vector3.Cross(tangent, biNormal);
        }

        public Vector3 GetBiNormal(float t, bool worldSpace = true)
        {
            Vector3 tangent = GetDirection(t, worldSpace);
            return Vector3.Cross(tangent, Vector3.up).normalized;
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

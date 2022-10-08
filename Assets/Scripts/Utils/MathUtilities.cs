using System;
using Unity.Mathematics;
using UnityEngine;

namespace Utils
{

    /// <summary>
    /// Collection of utility math operations.
    /// </summary>
    public static class MathUtilities
    {
        public const float Deg2Rad = 0.01745329f;
        public const float Epsilon = 0.001f;

        /// <summary>
        /// Determines if the two float values are equal to each other within the range of the epsilon.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="epsilon"></param>
        /// <returns></returns>
        public static bool FloatEquals(float a, float b)
        {
            return Mathf.Abs(a - b) <= Epsilon;
        }

        public static bool IsNan(this float3 value)
        {
            return float.IsNaN(value.x) || float.IsNaN(value.y) || float.IsNaN(value.z);
        }
        
        public static double AngleBetween(float2 vector1, float2 vector2)
        {
            double sin = vector1.x * vector2.y - vector2.x * vector1.y;  
            double cos = vector1.x * vector2.x + vector1.y * vector2.y;

            return math.atan2(sin, cos) * (180 / math.PI);
        }
        
        /// <summary>
        /// Returns true if the specified float value is <c>0.0</c> (or within the given epsilon).
        /// </summary>
        /// <param name="a"></param>
        /// <param name="epsilon"></param>
        /// <returns></returns>
        public static bool IsZero(this float a)
        {
            return FloatEquals(a, 0.0f);
        }

        /// <summary>
        /// Returns true if all components of the specified vector are <c>0.0</c> (or within the given epsilon).
        /// </summary>
        /// <param name="v"></param>
        /// <param name="epsilon"></param>
        /// <returns></returns>
        public static bool IsZero(this float3 v)
        {
            return (IsZero(v.x) && IsZero(v.y) && IsZero(v.z));
        }

        /// <summary>
        /// Sets the components of the specified vector to <c>0.0</c> if they are less than the given epsilon.
        /// </summary>
        /// <param name="vec"></param>
        /// <param name="epsilon"></param>
        /// <returns></returns>
        public static float3 ZeroOut(this float3 vec)
        {
            return vec.SqrMagnitude() < Epsilon ? float3.zero : vec;
        }
        
        public static float ZeroOut(this float v)
        {
            return math.abs(v) < Epsilon ? 0 : v;
        }

        public static bool IsEqualTo(this float3 f, float3 to)
        {
            return f.x.IsApproximately(to.x) && f.y.IsApproximately(to.y) && f.z.IsApproximately(to.z);
        }

        public static bool IsApproximately(this float a, float b)
        {
            return math.abs(a - b) < Epsilon;
        }

        public static float2 ToFloat2(this float3 f)
        {
            return new float2(f.x, f.z);
        }

        public static float SqrMagnitude(this float2 v)
        {
            return (float) (v.x * (double) v.x + v.y * (double) v.y);
        }
        
        public static float SqrMagnitude(this float3 v) => (float) ((double) v.x * v.x + (double) v.y * v.y + (double) v.z * v.z);
        public static float Length(this float3 v) => math.length(v);

        public static float3 GetForwardVectorFromRotation(quaternion rotation)

        {            
            float num1 = rotation.value.x * 2f;
            float num2 = rotation.value.y * 2f;
            float num3 = rotation.value.z * 2f;
            float num4 = rotation.value.x * num1;
            float num5 = rotation.value.y * num2;
            float num6 = rotation.value.z * num3;
            float num7 = rotation.value.x * num2;
            float num8 = rotation.value.x * num3;
            float num9 = rotation.value.y * num3;
            float num10 = rotation.value.w * num1;
            float num11 = rotation.value.w * num2;
            float num12 = rotation.value.w * num3;
            float3 vector3;
            float3 point = math.forward();
            vector3.x = (float) ((1.0 - (num5 + (double) num6)) * point.x + (num7 - (double) num12) * point.y + (num8 + (double) num11) * point.z);
            vector3.y = (float) ((num7 + (double) num12) * point.x + (1.0 - (num4 + (double) num6)) * point.y + (num9 - (double) num10) * point.z);
            vector3.z = (float) ((num8 - (double) num11) * point.x + (num9 + (double) num10) * point.y + (1.0 - (num4 + (double) num5)) * point.z);
            return vector3;
        }
        
        
        public static float3 ToEulerAngles(this quaternion q)
        {
            float3 angles = float3.zero;

            // roll / x
            double sinr_cosp = 2 * (q.value.w * q.value.x + q.value.y * q.value.z);
            double cosr_cosp = 1 - 2 * (q.value.x * q.value.x + q.value.y * q.value.y);
            angles.x = (float)math.atan2(sinr_cosp, cosr_cosp);

            // pitch / y
            double sinp = 2 * (q.value.w * q.value.y - q.value.z * q.value.x);
            if (Math.Abs(sinp) >= 1)
            {
                angles.y = sinp >= 0 ? math.PI / 2 : -math.PI / 2 ;
            }
            else
            {
                angles.y = (float)math.asin(sinp);
            }

            // yaw / z
            double siny_cosp = 2 * (q.value.w * q.value.z + q.value.x * q.value.y);
            double cosy_cosp = 1 - 2 * (q.value.y * q.value.y + q.value.z * q.value.z);
            angles.z = (float)math.atan2(siny_cosp, cosy_cosp);

            return angles;
        }
    }
}

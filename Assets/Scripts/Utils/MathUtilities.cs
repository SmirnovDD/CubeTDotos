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
        public static bool FloatEquals(float a, float b, float epsilon = 0.000001f)
        {
            return Mathf.Abs(a - b) <= epsilon;
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
        public static bool IsZero(float a, float epsilon = 0.0000001f)
        {
            return FloatEquals(a, 0.0f, epsilon);
        }

        /// <summary>
        /// Returns true if all components of the specified vector are <c>0.0</c> (or within the given epsilon).
        /// </summary>
        /// <param name="v"></param>
        /// <param name="epsilon"></param>
        /// <returns></returns>
        public static bool IsZero(float3 v, float epsilon = 0.000001f)
        {
            return (IsZero(v.x, epsilon) && IsZero(v.y, epsilon) && IsZero(v.z, epsilon));
        }

        /// <summary>
        /// Sets the components of the specified vector to <c>0.0</c> if they are less than the given epsilon.
        /// </summary>
        /// <param name="vec"></param>
        /// <param name="epsilon"></param>
        /// <returns></returns>
        public static float3 ZeroOut(float3 vec, float epsilon = 0.001f)
        {
            vec.x = math.abs(vec.x) < epsilon ? 0.0f : vec.x;
            vec.y = math.abs(vec.y) < epsilon ? 0.0f : vec.y;
            vec.z = math.abs(vec.z) < epsilon ? 0.0f : vec.z;

            return vec;
        }
    }
}

using UnityEngine;
using System.Runtime.CompilerServices;

namespace BDArmory.Extensions
{
    public static class VectorExtensions
    {
        /// <summary>
        /// Project a vector onto a plane defined by the plane normal (pre-normalized).
        /// 
        /// This implementation assumes that the plane normal is already normalized,
        /// skipping such checks and normalization that Vector3.ProjectOnPlane does,
        /// which gives a speed-up by a factor of approximately 1.7.
        /// </summary>
        /// <param name="vector">The vector to project.</param>
        /// <param name="planeNormal">The plane normal (pre-normalized).</param>
        /// <returns>The projected vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ProjectOnPlanePreNormalized(this Vector3 vector, Vector3 planeNormal)
        {
            var dot = Vector3.Dot(vector, planeNormal);
            return new Vector3(
                vector.x - planeNormal.x * dot,
                vector.y - planeNormal.y * dot,
                vector.z - planeNormal.z * dot);
        }

        /// <summary>
        /// Overload for Vector3d, returns Vector3.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ProjectOnPlanePreNormalized(this Vector3d vector, Vector3 planeNormal)
        {
            var dot = Vector3.Dot(vector, planeNormal);
            return new Vector3(
                (float)vector.x - planeNormal.x * dot,
                (float)vector.y - planeNormal.y * dot,
                (float)vector.z - planeNormal.z * dot);
        }

        /// <summary>
        /// Project a vector onto a plane defined by the plane normal (not-necessarily normalized).
        /// 
        /// This implementation is the same as the Unity reference implementation,
        /// but with an extra optimisation to reduce the number of division operations to 1.
        /// </summary>
        /// <param name="vector">The vector to project.</param>
        /// <param name="planeNormal">The plane normal.</param>
        /// <returns>The projected vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ProjectOnPlane(this Vector3 vector, Vector3 planeNormal)
        {
            var sqrMag = Vector3.Dot(planeNormal, planeNormal);
            if (sqrMag < Mathf.Epsilon) return vector;
            var dotNorm = Vector3.Dot(vector, planeNormal) / sqrMag;
            return new Vector3(
                vector.x - planeNormal.x * dotNorm,
                vector.y - planeNormal.y * dotNorm,
                vector.z - planeNormal.z * dotNorm);
        }

        /// <summary>
        /// Overload for Vector3d, returns Vector3.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ProjectOnPlane(this Vector3d vector, Vector3 planeNormal)
        {
            var sqrMag = Vector3.Dot(planeNormal, planeNormal);
            if (sqrMag < Mathf.Epsilon) return vector;
            var dotNorm = Vector3.Dot(vector, planeNormal) / sqrMag;
            return new Vector3(
                (float)vector.x - planeNormal.x * dotNorm,
                (float)vector.y - planeNormal.y * dotNorm,
                (float)vector.z - planeNormal.z * dotNorm);
        }
    }
}
using UnityEngine;

namespace Momentum
{
    /// <summary>Small math helpers shared across systems.</summary>
    public static class MathUtil
    {
        /// <summary>Returns a random direction inside a cone of the given half-angle (degrees) around dir.</summary>
        public static Vector3 RandomInCone(Vector3 dir, float halfAngleDeg)
        {
            if (halfAngleDeg <= 0.0001f) return dir.normalized;
            dir.Normalize();
            // Uniform distribution over the spherical cap.
            float cosMax = Mathf.Cos(halfAngleDeg * Mathf.Deg2Rad);
            float z = Random.Range(cosMax, 1f);
            float phi = Random.Range(0f, Mathf.PI * 2f);
            float r = Mathf.Sqrt(Mathf.Max(0f, 1f - z * z));
            var local = new Vector3(r * Mathf.Cos(phi), r * Mathf.Sin(phi), z);
            return Quaternion.LookRotation(dir) * local;
        }

        /// <summary>
        /// Closest point on an infinite line (origin + t * axis) to a ray. Used by the editor gizmo.
        /// Returns the parameter t along the axis.
        /// </summary>
        public static float ClosestParameterOnLineToRay(Vector3 lineOrigin, Vector3 lineDir, Ray ray)
        {
            Vector3 d1 = lineDir.normalized;
            Vector3 d2 = ray.direction.normalized;
            Vector3 r = lineOrigin - ray.origin;
            float a = Vector3.Dot(d1, d1);
            float b = Vector3.Dot(d1, d2);
            float e = Vector3.Dot(d2, d2);
            float c = Vector3.Dot(d1, r);
            float f = Vector3.Dot(d2, r);
            float denom = a * e - b * b;
            if (Mathf.Abs(denom) < 1e-6f) return 0f;
            return (b * f - c * e) / denom;
        }

        public static float SnapTo(float value, float step)
        {
            if (step <= 0.0001f) return value;
            return Mathf.Round(value / step) * step;
        }

        public static Vector3 SnapTo(Vector3 v, float step)
        {
            return new Vector3(SnapTo(v.x, step), SnapTo(v.y, step), SnapTo(v.z, step));
        }

        public static Vector3 Horizontal(Vector3 v)
        {
            return new Vector3(v.x, 0f, v.z);
        }

        /// <summary>Exponential smoothing factor that is framerate independent.</summary>
        public static float Damp(float sharpness, float dt)
        {
            return 1f - Mathf.Exp(-sharpness * dt);
        }

        /// <summary>Wraps an angle into the -180..180 range.</summary>
        public static float WrapAngle(float angle)
        {
            angle %= 360f;
            if (angle > 180f) angle -= 360f;
            if (angle < -180f) angle += 360f;
            return angle;
        }

        /// <summary>Predicts where a target will be for a projectile of the given speed (first-order lead).</summary>
        public static Vector3 PredictPosition(Vector3 shooter, Vector3 targetPos, Vector3 targetVel, float projectileSpeed, float leadFactor = 1f)
        {
            if (projectileSpeed <= 0.01f) return targetPos;
            float dist = Vector3.Distance(shooter, targetPos);
            float t = dist / projectileSpeed;
            return targetPos + targetVel * t * leadFactor;
        }
    }
}

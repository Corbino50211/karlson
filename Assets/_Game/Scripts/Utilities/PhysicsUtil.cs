using UnityEngine;

namespace Momentum
{
    /// <summary>Allocation-free physics query helpers.</summary>
    public static class PhysicsUtil
    {
        static readonly RaycastHit[] hitBuffer = new RaycastHit[32];
        static readonly Collider[] overlapBuffer = new Collider[64];

        public static Collider[] OverlapBuffer => overlapBuffer;

        /// <summary>
        /// Sweeps a ray or sphere and returns the closest hit that does not belong to ignoreRoot.
        /// </summary>
        public static bool SweepClosest(Vector3 origin, Vector3 direction, float distance, float radius, int mask,
            Transform ignoreRoot, out RaycastHit closest, QueryTriggerInteraction triggers = QueryTriggerInteraction.Ignore)
        {
            return SweepClosest(origin, direction, distance, radius, mask, ignoreRoot, null, out closest, triggers);
        }

        /// <summary>Same as above with a second hierarchy to ignore (e.g. the projectile itself and its owner).</summary>
        public static bool SweepClosest(Vector3 origin, Vector3 direction, float distance, float radius, int mask,
            Transform ignoreRoot, Transform ignoreRoot2, out RaycastHit closest, QueryTriggerInteraction triggers = QueryTriggerInteraction.Ignore)
        {
            closest = default;
            if (distance <= 0f) return false;
            direction.Normalize();
            int count = radius > 0.0001f
                ? Physics.SphereCastNonAlloc(origin, radius, direction, hitBuffer, distance, mask, triggers)
                : Physics.RaycastNonAlloc(origin, direction, hitBuffer, distance, mask, triggers);

            float best = float.MaxValue;
            bool found = false;
            for (int i = 0; i < count; i++)
            {
                var h = hitBuffer[i];
                if (h.collider == null) continue;
                if (ignoreRoot != null && h.collider.transform.IsChildOf(ignoreRoot)) continue;
                if (ignoreRoot2 != null && h.collider.transform.IsChildOf(ignoreRoot2)) continue;
                // Sphere casts that start overlapping report distance 0 and point zero; treat as hit at origin.
                if (h.distance <= 0f && h.point == Vector3.zero)
                {
                    h.point = origin;
                    h.normal = -direction;
                }
                if (h.distance < best)
                {
                    best = h.distance;
                    closest = h;
                    found = true;
                }
            }
            return found;
        }

        /// <summary>
        /// Raycasts all hits along a ray sorted by distance into the shared buffer. Returns the count.
        /// </summary>
        public static int RaycastAllSorted(Ray ray, float distance, int mask, out RaycastHit[] results)
        {
            int count = Physics.RaycastNonAlloc(ray, hitBuffer, distance, mask, QueryTriggerInteraction.Ignore);
            // insertion sort (small counts)
            for (int i = 1; i < count; i++)
            {
                var key = hitBuffer[i];
                int j = i - 1;
                while (j >= 0 && hitBuffer[j].distance > key.distance)
                {
                    hitBuffer[j + 1] = hitBuffer[j];
                    j--;
                }
                hitBuffer[j + 1] = key;
            }
            results = hitBuffer;
            return count;
        }

        public static int OverlapSphere(Vector3 center, float radius, int mask, QueryTriggerInteraction triggers = QueryTriggerInteraction.Ignore)
        {
            return Physics.OverlapSphereNonAlloc(center, radius, overlapBuffer, mask, triggers);
        }
    }
}

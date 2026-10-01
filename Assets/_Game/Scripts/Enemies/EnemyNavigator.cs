using UnityEngine;
using UnityEngine.AI;

namespace Momentum.Enemies
{
    /// <summary>
    /// Path helper for Rigidbody-driven enemies. Uses the runtime-baked NavMesh when available
    /// (NavMesh.CalculatePath) and falls back to direct steering when there is no NavMesh.
    /// Enemies stay physical (explosions and knockback work naturally) while still navigating.
    /// </summary>
    public class EnemyNavigator
    {
        readonly NavMeshPath path = new NavMeshPath();
        readonly Vector3[] corners = new Vector3[32];
        int cornerCount;
        int cornerIndex;
        float nextRepath;
        Vector3 lastDestination;

        public float RepathInterval { get; set; } = 0.4f;
        public bool HasPath => cornerCount > 0;

        public Vector3 GetSteerTarget(Vector3 from, Vector3 destination)
        {
            if (Time.time >= nextRepath || (destination - lastDestination).sqrMagnitude > 9f)
            {
                Repath(from, destination);
            }
            if (cornerCount == 0) return destination;

            while (cornerIndex < cornerCount && HorizontalDistance(from, corners[cornerIndex]) < 0.9f)
            {
                cornerIndex++;
            }
            if (cornerIndex >= cornerCount) return destination;
            return corners[cornerIndex];
        }

        void Repath(Vector3 from, Vector3 destination)
        {
            nextRepath = Time.time + RepathInterval + Random.Range(0f, 0.2f);
            lastDestination = destination;
            cornerCount = 0;
            cornerIndex = 0;
            if (!NavMeshBakerProxy.HasNavMesh) return;
            if (!NavMesh.SamplePosition(from, out NavMeshHit a, 2.5f, NavMesh.AllAreas)) return;
            if (!NavMesh.SamplePosition(destination, out NavMeshHit b, 5f, NavMesh.AllAreas)) return;
            if (NavMesh.CalculatePath(a.position, b.position, NavMesh.AllAreas, path) && path.status != NavMeshPathStatus.PathInvalid)
            {
                cornerCount = path.GetCornersNonAlloc(corners);
                cornerIndex = cornerCount > 1 ? 1 : 0;
            }
        }

        public void Clear()
        {
            cornerCount = 0;
            cornerIndex = 0;
            nextRepath = 0f;
        }

        static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }

    /// <summary>Tiny indirection so enemies can ask whether a NavMesh exists without depending on the level system.</summary>
    public static class NavMeshBakerProxy
    {
        public static bool HasNavMesh { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            HasNavMesh = false;
        }
    }
}

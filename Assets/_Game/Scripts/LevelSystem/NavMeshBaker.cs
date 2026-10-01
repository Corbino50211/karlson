using System.Collections.Generic;
using Momentum.Enemies;
using UnityEngine;
using UnityEngine.AI;

namespace Momentum.Levels
{
    /// <summary>
    /// Builds a NavMesh at runtime from the level's static colliders (works for generated campaign scenes
    /// and custom JSON levels alike, no editor baking required). Moving platforms and dynamic props are
    /// excluded. Enemies query it through EnemyNavigator.
    /// </summary>
    public class NavMeshBaker : MonoBehaviour
    {
        public static NavMeshBaker Instance { get; private set; }

        [SerializeField] int agentTypeId;
        [SerializeField] float boundsPadding = 10f;

        NavMeshDataInstance dataInstance;
        NavMeshData data;

        public bool HasNavMesh => dataInstance.valid;

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            Clear();
            if (Instance == this) Instance = null;
        }

        /// <summary>Collects sources under root (or the whole scene when null) and builds the NavMesh.</summary>
        public bool Bake(Transform root)
        {
            Clear();
            int mask = (1 << Layers.Default) | (1 << Layers.Environment) | (1 << Layers.Grapple);
            var sources = new List<NavMeshBuildSource>();
            var markups = new List<NavMeshBuildMarkup>();
            Bounds bounds = ComputeBounds(root);

            if (root != null) NavMeshBuilder.CollectSources(root, mask, NavMeshCollectGeometry.PhysicsColliders, 0, markups, sources);
            else NavMeshBuilder.CollectSources(bounds, mask, NavMeshCollectGeometry.PhysicsColliders, 0, markups, sources);

            sources.RemoveAll(s =>
            {
                var c = s.component as Collider;
                if (c == null) return false;
                if (c.isTrigger) return true;
                if (c.GetComponentInParent<MovingPlatform>() != null) return true;
                var rb = c.attachedRigidbody;
                return rb != null && !rb.isKinematic;
            });

            if (sources.Count == 0)
            {
                NavMeshBakerProxy.HasNavMesh = false;
                return false;
            }

            var settings = NavMesh.GetSettingsByID(agentTypeId);
            data = NavMeshBuilder.BuildNavMeshData(settings, sources, bounds, Vector3.zero, Quaternion.identity);
            if (data == null)
            {
                NavMeshBakerProxy.HasNavMesh = false;
                return false;
            }
            dataInstance = NavMesh.AddNavMeshData(data);
            NavMeshBakerProxy.HasNavMesh = dataInstance.valid;
            return dataInstance.valid;
        }

        public void Clear()
        {
            if (dataInstance.valid) dataInstance.Remove();
            dataInstance = default;
            data = null;
            NavMeshBakerProxy.HasNavMesh = false;
        }

        Bounds ComputeBounds(Transform root)
        {
            var colliders = root != null ? root.GetComponentsInChildren<Collider>() : FindObjectsOfType<Collider>();
            bool has = false;
            Bounds b = new Bounds(Vector3.zero, Vector3.one * 50f);
            foreach (var c in colliders)
            {
                if (c == null || c.isTrigger) continue;
                if (!has)
                {
                    b = c.bounds;
                    has = true;
                }
                else
                {
                    b.Encapsulate(c.bounds);
                }
            }
            b.Expand(boundsPadding);
            return b;
        }
    }
}

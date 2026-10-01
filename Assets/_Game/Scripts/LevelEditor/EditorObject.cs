using Momentum.Levels;
using UnityEngine;

namespace Momentum.LevelEditor
{
    /// <summary>Links an editor preview instance to its LevelObjectData entry.</summary>
    public class EditorObject : MonoBehaviour
    {
        public int Id { get; private set; }
        public LevelObjectData Data { get; private set; }
        public LevelObjectDef Definition { get; private set; }

        public void Init(LevelObjectData data)
        {
            Data = data;
            Id = data.id;
            Definition = LevelObjectCatalog.Get(data.objectType);
        }

        /// <summary>World bounds of the visible object (renderers, falling back to colliders).</summary>
        public Bounds GetBounds()
        {
            bool has = false;
            var b = new Bounds(transform.position, Vector3.one * 0.5f);
            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                if (r is LineRenderer || r is ParticleSystemRenderer || r is TrailRenderer) continue;
                if (!r.enabled) continue;
                if (!has)
                {
                    b = r.bounds;
                    has = true;
                }
                else b.Encapsulate(r.bounds);
            }
            if (!has)
            {
                foreach (var c in GetComponentsInChildren<Collider>())
                {
                    if (!has)
                    {
                        b = c.bounds;
                        has = true;
                    }
                    else b.Encapsulate(c.bounds);
                }
            }
            return b;
        }

        /// <summary>Applies the data transform to the instance (used during live gizmo drags).</summary>
        public void SyncTransform()
        {
            transform.SetPositionAndRotation(Data.position, Quaternion.Euler(Data.rotation));
            if (Definition != null && Definition.scalable)
            {
                transform.localScale = new Vector3(Mathf.Max(0.05f, Data.scale.x), Mathf.Max(0.05f, Data.scale.y), Mathf.Max(0.05f, Data.scale.z));
            }
        }
    }
}

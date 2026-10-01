using UnityEngine;

namespace Momentum
{
    /// <summary>Implemented by components that need to reset themselves when spawned from / returned to a pool.</summary>
    public interface IPoolable
    {
        void OnSpawned();
        void OnDespawned();
    }

    /// <summary>Marks an instance as belonging to a pool. Added automatically by PoolManager.</summary>
    [DisallowMultipleComponent]
    public class PooledObject : MonoBehaviour
    {
        public GameObject SourcePrefab { get; set; }
        public bool IsSpawned { get; set; }

        /// <summary>Returns this object to its pool.</summary>
        public void Despawn()
        {
            PoolManager.Despawn(gameObject);
        }
    }
}

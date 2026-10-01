using UnityEngine;

namespace Momentum.Enemies
{
    /// <summary>Spawns enemies by type from the PrefabRegistry (used by bosses, waves and the level loader).</summary>
    public static class EnemySpawner
    {
        public static EnemyBase Spawn(EnemyType type, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            var registry = PrefabRegistry.Instance;
            var prefab = registry != null ? registry.GetEnemyPrefab(type) : null;
            if (prefab == null)
            {
                Debug.LogWarning($"[Momentum] No prefab registered for enemy type {type}. Run the setup tool.");
                return null;
            }
            var go = Object.Instantiate(prefab, position, rotation, parent);
            var spawnFx = registry.pickupBurst;
            if (spawnFx != null) PoolManager.Spawn(spawnFx, position + Vector3.up, Quaternion.identity);
            return go.GetComponent<EnemyBase>();
        }

        public static bool TryParseType(string value, out EnemyType type)
        {
            return System.Enum.TryParse(value, true, out type);
        }
    }
}

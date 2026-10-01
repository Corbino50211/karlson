using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Momentum
{
    /// <summary>
    /// Simple prefab-keyed object pool for frequently spawned objects (projectiles, impacts, explosions,
    /// tracers). Objects are returned automatically on scene change.
    /// </summary>
    public class PoolManager : MonoBehaviour
    {
        public static PoolManager Instance { get; private set; }

        readonly Dictionary<GameObject, Stack<PooledObject>> pools = new Dictionary<GameObject, Stack<PooledObject>>();
        readonly HashSet<PooledObject> active = new HashSet<PooledObject>();
        readonly List<PooledObject> scratch = new List<PooledObject>();
        readonly List<IPoolable> poolableScratch = new List<IPoolable>();
        Transform root;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            root = new GameObject("Pool").transform;
            root.SetParent(transform, false);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                Instance = null;
            }
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Single) DespawnAll();
        }

        /// <summary>Spawns (or reuses) an instance of the prefab. Falls back to Instantiate when no pool exists.</summary>
        public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null) return null;
            if (Instance == null) return Instantiate(prefab, position, rotation);
            return Instance.SpawnInternal(prefab, position, rotation);
        }

        public static T Spawn<T>(T prefab, Vector3 position, Quaternion rotation) where T : Component
        {
            if (prefab == null) return null;
            var go = Spawn(prefab.gameObject, position, rotation);
            return go != null ? go.GetComponent<T>() : null;
        }

        /// <summary>Returns an object to its pool, or destroys it if it was not pooled.</summary>
        public static void Despawn(GameObject go)
        {
            if (go == null) return;
            var pooled = go.GetComponent<PooledObject>();
            if (pooled == null || Instance == null)
            {
                Destroy(go);
                return;
            }
            Instance.ReturnInternal(pooled);
        }

        public static void Prewarm(GameObject prefab, int count)
        {
            if (prefab == null || Instance == null) return;
            var list = new List<GameObject>();
            for (int i = 0; i < count; i++) list.Add(Spawn(prefab, Vector3.down * 1000f, Quaternion.identity));
            foreach (var go in list) Despawn(go);
        }

        /// <summary>Returns every active pooled object (used on scene change and when leaving editor test mode).</summary>
        public static void DespawnAll()
        {
            if (Instance == null) return;
            Instance.scratch.Clear();
            Instance.scratch.AddRange(Instance.active);
            foreach (var p in Instance.scratch)
            {
                if (p != null) Instance.ReturnInternal(p);
            }
            Instance.active.Clear();
        }

        GameObject SpawnInternal(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (!pools.TryGetValue(prefab, out var stack))
            {
                stack = new Stack<PooledObject>();
                pools[prefab] = stack;
            }

            PooledObject item = null;
            while (stack.Count > 0 && item == null)
            {
                item = stack.Pop();
            }

            if (item == null)
            {
                var go = Instantiate(prefab, position, rotation, root);
                item = go.GetComponent<PooledObject>();
                if (item == null) item = go.AddComponent<PooledObject>();
                item.SourcePrefab = prefab;
            }
            else
            {
                item.transform.SetPositionAndRotation(position, rotation);
                item.gameObject.SetActive(true);
            }

            item.IsSpawned = true;
            active.Add(item);
            item.GetComponentsInChildren(true, poolableScratch);
            foreach (var p in poolableScratch) p.OnSpawned();
            return item.gameObject;
        }

        void ReturnInternal(PooledObject item)
        {
            if (!item.IsSpawned) return;
            item.IsSpawned = false;
            active.Remove(item);
            item.GetComponentsInChildren(true, poolableScratch);
            foreach (var p in poolableScratch) p.OnDespawned();
            item.gameObject.SetActive(false);
            if (item.transform.parent != root) item.transform.SetParent(root, false);
            if (item.SourcePrefab != null)
            {
                if (!pools.TryGetValue(item.SourcePrefab, out var stack))
                {
                    stack = new Stack<PooledObject>();
                    pools[item.SourcePrefab] = stack;
                }
                stack.Push(item);
            }
            else
            {
                Destroy(item.gameObject);
            }
        }
    }
}

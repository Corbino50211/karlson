using System;
using Momentum.Bosses;
using Momentum.Enemies;
using UnityEngine;

namespace Momentum.Levels
{
    public enum LevelBuildMode
    {
        /// <summary>Fully functional objects (gameplay).</summary>
        Play,
        /// <summary>Inert previews for the level editor (no AI, no physics, editor markers visible).</summary>
        Edit
    }

    /// <summary>
    /// Instantiates level objects from LevelObjectData using the generated prefabs in the PrefabRegistry.
    /// Shared by the runtime LevelLoader, the in-game level editor and the editor-time campaign generator
    /// (which swaps in PrefabUtility through InstantiateOverride so scenes keep prefab links).
    /// </summary>
    public static class LevelObjectFactory
    {
        /// <summary>Optional instantiation override (prefab, parent) used by editor tooling.</summary>
        public static Func<GameObject, Transform, GameObject> InstantiateOverride;

        public static GameObject Create(LevelObjectData data, Transform parent, LevelBuildMode mode)
        {
            if (data == null) return null;
            var def = LevelObjectCatalog.Get(data.objectType);
            if (def == null)
            {
                Debug.LogWarning($"[Momentum] Unknown level object type '{data.objectType}'.");
                return null;
            }

            var registry = PrefabRegistry.Instance;
            GameObject prefab = ResolvePrefab(data, def, registry);
            Quaternion rotation = Quaternion.Euler(data.rotation);
            GameObject go;
            if (prefab != null)
            {
                go = Spawn(prefab, parent, data.position, rotation);
            }
            else
            {
                go = CreateFallback(def, parent);
                go.transform.SetPositionAndRotation(data.position, rotation);
            }
            go.name = def.type + "_" + data.id;
            if (def.scalable) go.transform.localScale = SafeScale(data.scale);

            if (def.type == "BossTrigger") SpawnBoss(go, data, registry, mode);

            foreach (var configurable in go.GetComponentsInChildren<ILevelObjectConfigurable>(true))
            {
                configurable.ApplyLevelProperties(data);
            }

            if (mode == LevelBuildMode.Edit) MakeInert(go);
            return go;
        }

        static GameObject Spawn(GameObject prefab, Transform parent, Vector3 position, Quaternion rotation)
        {
            if (InstantiateOverride != null)
            {
                var go = InstantiateOverride(prefab, parent);
                go.transform.SetPositionAndRotation(position, rotation);
                return go;
            }
            return UnityEngine.Object.Instantiate(prefab, position, rotation, parent);
        }

        static Vector3 SafeScale(Vector3 s)
        {
            return new Vector3(Mathf.Max(0.05f, Mathf.Abs(s.x)), Mathf.Max(0.05f, Mathf.Abs(s.y)), Mathf.Max(0.05f, Mathf.Abs(s.z)));
        }

        static GameObject ResolvePrefab(LevelObjectData data, LevelObjectDef def, PrefabRegistry registry)
        {
            if (registry == null) return null;
            if (def.type == "EnemySpawn")
            {
                if (EnemySpawner.TryParseType(data.GetString("enemy", "Gunner"), out EnemyType type))
                {
                    var enemy = registry.GetEnemyPrefab(type);
                    if (enemy != null) return enemy;
                }
            }
            return registry.GetLevelObjectPrefab(def.type);
        }

        static void SpawnBoss(GameObject triggerObject, LevelObjectData data, PrefabRegistry registry, LevelBuildMode mode)
        {
            if (registry == null) return;
            if (!Enum.TryParse(data.GetString("boss", "Warden"), true, out BossType bossType)) bossType = BossType.Warden;
            var prefab = registry.GetBossPrefab(bossType);
            if (prefab == null)
            {
                Debug.LogWarning($"[Momentum] No boss prefab registered for {bossType}.");
                return;
            }
            var bossGo = Spawn(prefab, triggerObject.transform, triggerObject.transform.position, triggerObject.transform.rotation);
            bossGo.name = "Boss_" + bossType;
            var trigger = triggerObject.GetComponent<BossArenaTrigger>();
            var boss = bossGo.GetComponent<BossBase>();
            if (trigger != null && boss != null) trigger.SetBoss(boss);
        }

        /// <summary>Disables gameplay behaviour so the object can be safely previewed/edited.</summary>
        public static void MakeInert(GameObject go)
        {
            foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb == null) continue;
                mb.enabled = false;
            }
            foreach (var rb in go.GetComponentsInChildren<Rigidbody>(true))
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }
            foreach (var audio in go.GetComponentsInChildren<AudioSource>(true)) audio.enabled = false;
            foreach (var ps in go.GetComponentsInChildren<ParticleSystem>(true)) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        static GameObject CreateFallback(LevelObjectDef def, Transform parent)
        {
            var go = GameObject.CreatePrimitive(def.type == "Pillar" ? PrimitiveType.Cylinder : PrimitiveType.Cube);
            go.transform.SetParent(parent, false);
            go.layer = Layers.Environment;
            var mat = MaterialLibrary.Resolve("White");
            if (mat != null) go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            if (def.category != LevelObjectCatalog.Geometry)
            {
                var col = go.GetComponent<Collider>();
                if (col != null) col.isTrigger = true;
                go.transform.localScale = Vector3.one * 0.8f;
            }
            return go;
        }
    }
}

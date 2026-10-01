using System;
using System.Collections.Generic;
using Momentum.Bosses;
using Momentum.Enemies;
using Momentum.Weapons;
using UnityEngine;

namespace Momentum
{
    /// <summary>
    /// Holds references to every generated prefab the game needs at runtime. Populated automatically by
    /// Tools > Parkour FPS > Setup Complete Game. Systems look prefabs up here instead of using
    /// scene references, so levels (including custom JSON levels) need no manual wiring.
    /// </summary>
    [CreateAssetMenu(menuName = "Momentum/Prefab Registry", fileName = "PrefabRegistry")]
    public class PrefabRegistry : ScriptableObject
    {
        [Serializable]
        public class LevelObjectPrefab
        {
            public string type;
            public GameObject prefab;
        }

        [Serializable]
        public class EnemyPrefab
        {
            public EnemyType type;
            public GameObject prefab;
        }

        [Serializable]
        public class BossPrefab
        {
            public BossType type;
            public GameObject prefab;
        }

        [Header("Player")]
        public GameObject player;

        [Header("Weapons")]
        public List<WeaponDefinition> weapons = new List<WeaponDefinition>();

        [Header("Enemies & Bosses")]
        public List<EnemyPrefab> enemies = new List<EnemyPrefab>();
        public List<BossPrefab> bosses = new List<BossPrefab>();

        [Header("Level Objects (used by the level loader and editor)")]
        public List<LevelObjectPrefab> levelObjects = new List<LevelObjectPrefab>();

        [Header("Projectiles")]
        public GameObject enemyBullet;
        public GameObject enemyPellet;
        public GameObject rocket;
        public GameObject grenade;
        public GameObject missile;
        public GameObject rock;
        public GameObject energyOrb;

        [Header("Effects")]
        public GameObject muzzleFlash;
        public GameObject impactEnvironment;
        public GameObject impactEnemy;
        public GameObject explosion;
        public GameObject smallExplosion;
        public GameObject tracer;
        public GameObject railTrail;
        public GameObject enemyDeath;
        public GameObject debrisChunk;
        public GameObject glassShard;
        public GameObject telegraphDisc;
        public GameObject telegraphLine;
        public GameObject shockwave;
        public GameObject laserBeam;
        public GameObject pickupBurst;
        public GameObject checkpointBurst;
        public GameObject weakPoint;

        [Header("UI")]
        public GameObject hud;
        public GameObject pauseMenu;
        public GameObject resultsScreen;
        public GameObject mainMenu;
        public GameObject levelEditorUI;

        Dictionary<string, GameObject> levelObjectLookup;
        Dictionary<string, WeaponDefinition> weaponLookup;

        public static PrefabRegistry Instance => GameConfig.Instance != null ? GameConfig.Instance.prefabRegistry : null;

        void OnEnable()
        {
            levelObjectLookup = null;
            weaponLookup = null;
        }

        public void InvalidateCaches()
        {
            levelObjectLookup = null;
            weaponLookup = null;
        }

        public GameObject GetLevelObjectPrefab(string type)
        {
            if (string.IsNullOrEmpty(type)) return null;
            if (levelObjectLookup == null)
            {
                levelObjectLookup = new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);
                foreach (var entry in levelObjects)
                {
                    if (entry != null && !string.IsNullOrEmpty(entry.type) && entry.prefab != null)
                    {
                        levelObjectLookup[entry.type] = entry.prefab;
                    }
                }
            }
            levelObjectLookup.TryGetValue(type, out var prefab);
            return prefab;
        }

        public void SetLevelObjectPrefab(string type, GameObject prefab)
        {
            var existing = levelObjects.Find(e => e != null && string.Equals(e.type, type, StringComparison.OrdinalIgnoreCase));
            if (existing != null) existing.prefab = prefab;
            else levelObjects.Add(new LevelObjectPrefab { type = type, prefab = prefab });
            levelObjectLookup = null;
        }

        public GameObject GetEnemyPrefab(EnemyType type)
        {
            foreach (var entry in enemies)
            {
                if (entry != null && entry.type == type) return entry.prefab;
            }
            return null;
        }

        public void SetEnemyPrefab(EnemyType type, GameObject prefab)
        {
            var existing = enemies.Find(e => e != null && e.type == type);
            if (existing != null) existing.prefab = prefab;
            else enemies.Add(new EnemyPrefab { type = type, prefab = prefab });
        }

        public GameObject GetBossPrefab(BossType type)
        {
            foreach (var entry in bosses)
            {
                if (entry != null && entry.type == type) return entry.prefab;
            }
            return null;
        }

        public void SetBossPrefab(BossType type, GameObject prefab)
        {
            var existing = bosses.Find(e => e != null && e.type == type);
            if (existing != null) existing.prefab = prefab;
            else bosses.Add(new BossPrefab { type = type, prefab = prefab });
        }

        public WeaponDefinition GetWeapon(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (weaponLookup == null)
            {
                weaponLookup = new Dictionary<string, WeaponDefinition>(StringComparer.OrdinalIgnoreCase);
                foreach (var def in weapons)
                {
                    if (def != null && !string.IsNullOrEmpty(def.id)) weaponLookup[def.id] = def;
                }
            }
            weaponLookup.TryGetValue(id, out var result);
            return result;
        }
    }
}

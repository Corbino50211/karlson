using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Momentum.Bosses;
using Momentum.Enemies;
using Momentum.Levels;
using Momentum.PlayerSystems;
using Momentum.Weapons;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Momentum.EditorTools
{
    /// <summary>
    /// Tools > Parkour FPS > Validate Project. Checks layers, tags, input axes, the GameConfig and its
    /// registries, every generated prefab (missing scripts, missing references, required components),
    /// weapon/enemy definitions, scenes and Build Settings. Prints one readable report to the Console.
    /// </summary>
    public static class ProjectValidator
    {
        class Report
        {
            public readonly List<string> errors = new List<string>();
            public readonly List<string> warnings = new List<string>();
            public int checks;

            public void Check(bool ok, string message, bool warningOnly = false)
            {
                checks++;
                if (ok) return;
                if (warningOnly) warnings.Add(message);
                else errors.Add(message);
            }
        }

        /// <summary>Runs every check. Returns the number of errors.</summary>
        public static int Validate(bool showDialog)
        {
            var r = new Report();
            try
            {
                CheckLayersAndTags(r);
                CheckInput(r);
                var config = AssetDatabase.LoadAssetAtPath<GameConfig>(GamePaths.GameConfigAsset);
                r.Check(config != null, "GameConfig asset missing at " + GamePaths.GameConfigAsset + " (run Setup Complete Game).");
                if (config != null)
                {
                    CheckConfig(r, config);
                    if (config.prefabRegistry != null) CheckRegistry(r, config.prefabRegistry);
                    if (config.materialLibrary != null) CheckMaterials(r, config.materialLibrary);
                    if (config.levelRegistry != null) CheckLevels(r, config.levelRegistry);
                }
                CheckScenes(r, config);
            }
            catch (Exception e)
            {
                r.errors.Add("Validator crashed: " + e.Message);
                Debug.LogException(e);
            }

            var sb = new StringBuilder();
            sb.AppendLine($"[Momentum Validate] {r.checks} checks, {r.errors.Count} error(s), {r.warnings.Count} warning(s).");
            foreach (var e in r.errors) sb.AppendLine("  ERROR: " + e);
            foreach (var w in r.warnings) sb.AppendLine("  WARNING: " + w);
            if (r.errors.Count > 0) Debug.LogError(sb.ToString());
            else if (r.warnings.Count > 0) Debug.LogWarning(sb.ToString());
            else Debug.Log(sb.ToString() + "  Everything looks good.");

            if (showDialog)
            {
                string summary = r.errors.Count == 0
                    ? (r.warnings.Count == 0 ? "No problems found." : r.warnings.Count + " warning(s). See the Console for details.")
                    : r.errors.Count + " error(s) and " + r.warnings.Count + " warning(s). See the Console for details.\n\nMost problems are fixed by running Tools > Parkour FPS > Setup Complete Game.";
                EditorUtility.DisplayDialog("MOMENTUM - Validate Project", summary, "OK");
            }
            return r.errors.Count;
        }

        // ------------------------------------------------------------------ Project settings

        static void CheckLayersAndTags(Report r)
        {
            for (int i = 0; i < Layers.CustomLayerIndices.Length; i++)
            {
                int index = Layers.CustomLayerIndices[i];
                string expected = Layers.CustomLayerNames[i];
                string actual = LayerMask.LayerToName(index);
                r.Check(actual == expected, $"Layer {index} should be '{expected}' but is '{actual}'.");
            }
            var tags = InternalEditorUtility.tags;
            foreach (var tag in Layers.CustomTags)
            {
                r.Check(tags.Contains(tag), $"Tag '{tag}' is missing.");
            }
        }

        static void CheckInput(Report r)
        {
            foreach (var axis in new[] { "Horizontal", "Vertical", "Mouse X", "Mouse Y", "Mouse ScrollWheel", "Submit", "Cancel" })
            {
                r.Check(ProjectConfigurator.HasInputAxis(axis), $"Input axis '{axis}' is missing (Edit > Project Settings > Input Manager).");
            }
        }

        static void CheckConfig(Report r, GameConfig config)
        {
            r.Check(config.prefabRegistry != null, "GameConfig.prefabRegistry is not assigned.");
            r.Check(config.materialLibrary != null, "GameConfig.materialLibrary is not assigned.");
            r.Check(config.levelRegistry != null, "GameConfig.levelRegistry is not assigned.");
            r.Check(config.audioLibrary != null, "GameConfig.audioLibrary is not assigned (procedural audio will still work).", true);
            r.Check(config.defaultMovementSettings != null, "GameConfig.defaultMovementSettings is not assigned.");
            r.Check(config.defaultCameraSettings != null, "GameConfig.defaultCameraSettings is not assigned.");
            r.Check(!string.IsNullOrEmpty(config.gameTitle), "GameConfig.gameTitle is empty.", true);
            r.Check(Resources.Load<GameConfig>(GameConfig.ResourcePath) != null, "GameConfig cannot be loaded from Resources/" + GameConfig.ResourcePath + ".");
        }

        // ------------------------------------------------------------------ Prefabs

        static void CheckRegistry(Report r, PrefabRegistry reg)
        {
            CheckPrefab(r, reg.player, "Player", typeof(PlayerController), typeof(PlayerMovement), typeof(PlayerCamera), typeof(WeaponManager), typeof(Rigidbody), typeof(CapsuleCollider));
            if (reg.player != null)
            {
                r.Check(reg.player.GetComponentInChildren<Camera>(true) != null, "Player prefab has no Camera.");
                r.Check(reg.player.layer == Layers.Player, "Player prefab is not on the Player layer.");
            }

            r.Check(reg.weapons.Count >= 8, $"Prefab registry lists {reg.weapons.Count} weapons (expected 8).");
            foreach (var w in reg.weapons)
            {
                if (w == null)
                {
                    r.Check(false, "Prefab registry has an empty weapon slot.");
                    continue;
                }
                r.Check(!string.IsNullOrEmpty(w.id), $"Weapon '{w.name}' has no id.");
                CheckPrefab(r, w.viewModelPrefab, $"Weapon '{w.id}' view model", typeof(WeaponBase));
                CheckPrefab(r, w.worldModelPrefab, $"Weapon '{w.id}' world model");
                bool needsProjectile = w.viewModelPrefab != null && w.viewModelPrefab.GetComponent<ProjectileWeapon>() != null;
                if (needsProjectile) CheckPrefab(r, w.projectilePrefab, $"Weapon '{w.id}' projectile");
                r.Check(w.magazineSize > 0, $"Weapon '{w.id}' has a magazine size of 0.");
                r.Check(w.fireRate > 0f, $"Weapon '{w.id}' has a fire rate of 0.");
            }
            foreach (var id in LevelObjectCatalog.WeaponIds)
            {
                r.Check(reg.GetWeapon(id) != null, $"No weapon definition with id '{id}' (used by level pickups).");
            }

            foreach (EnemyType type in Enum.GetValues(typeof(EnemyType)))
            {
                var prefab = reg.GetEnemyPrefab(type);
                CheckPrefab(r, prefab, "Enemy " + type, typeof(EnemyBase), typeof(EnemyHealth), typeof(Rigidbody));
                if (prefab != null)
                {
                    var enemy = prefab.GetComponent<EnemyBase>();
                    if (enemy != null)
                    {
                        var so = new SerializedObject(enemy);
                        r.Check(so.FindProperty("definition").objectReferenceValue != null, $"Enemy {type} has no EnemyDefinition assigned.");
                    }
                    r.Check(prefab.layer == Layers.Enemy, $"Enemy {type} prefab is not on the Enemy layer.");
                }
            }
            foreach (BossType type in Enum.GetValues(typeof(BossType)))
            {
                CheckPrefab(r, reg.GetBossPrefab(type), "Boss " + type, typeof(BossBase), typeof(BossHealth));
            }
            CheckPrefab(r, reg.weakPoint, "Boss weak point", typeof(BossWeakPoint));

            foreach (var def in LevelObjectCatalog.All)
            {
                if (def.type == "EnemySpawn") continue;
                CheckPrefab(r, reg.GetLevelObjectPrefab(def.type), "Level object '" + def.type + "'");
            }

            var effects = new (GameObject prefab, string name)[]
            {
                (reg.enemyBullet, "Enemy bullet"), (reg.enemyPellet, "Enemy pellet"), (reg.rocket, "Rocket"), (reg.grenade, "Grenade"),
                (reg.missile, "Missile"), (reg.rock, "Rock"), (reg.energyOrb, "Energy orb"), (reg.muzzleFlash, "Muzzle flash"),
                (reg.impactEnvironment, "Impact (environment)"), (reg.impactEnemy, "Impact (enemy)"), (reg.explosion, "Explosion"),
                (reg.smallExplosion, "Small explosion"), (reg.tracer, "Tracer"), (reg.railTrail, "Rail trail"), (reg.enemyDeath, "Enemy death"),
                (reg.debrisChunk, "Debris chunk"), (reg.glassShard, "Glass shard"), (reg.telegraphDisc, "Telegraph disc"),
                (reg.telegraphLine, "Telegraph line"), (reg.shockwave, "Shockwave"), (reg.laserBeam, "Laser beam"),
                (reg.pickupBurst, "Pickup burst"), (reg.checkpointBurst, "Checkpoint burst")
            };
            foreach (var (prefab, name) in effects) CheckPrefab(r, prefab, name);

            var ui = new (GameObject prefab, string name)[]
            {
                (reg.hud, "HUD"), (reg.pauseMenu, "Pause menu"), (reg.resultsScreen, "Results screen"), (reg.mainMenu, "Main menu"), (reg.levelEditorUI, "Level editor UI")
            };
            foreach (var (prefab, name) in ui)
            {
                if (prefab == null) r.Check(false, name + " UI prefab is not generated (the UI will be built in code at runtime).", true);
                else CheckPrefab(r, prefab, name + " UI");
            }
        }

        static void CheckPrefab(Report r, GameObject prefab, string label, params Type[] required)
        {
            r.Check(prefab != null, label + " prefab is missing.");
            if (prefab == null) return;
            foreach (var type in required)
            {
                r.Check(prefab.GetComponent(type) != null, $"{label} prefab ({prefab.name}) has no {type.Name} component.");
            }
            foreach (var t in prefab.GetComponentsInChildren<Transform>(true))
            {
                int missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
                r.Check(missing == 0, $"{label} prefab ({prefab.name}) has {missing} missing script(s) on '{t.name}'.");
                foreach (var c in t.GetComponents<Component>())
                {
                    if (c == null) continue;
                    CheckMissingReferences(r, c, label, prefab.name, t.name);
                }
            }
        }

        static void CheckMissingReferences(Report r, Component c, string label, string prefabName, string objectName)
        {
            var so = new SerializedObject(c);
            var p = so.GetIterator();
            while (p.NextVisible(true))
            {
                if (p.propertyType != SerializedPropertyType.ObjectReference) continue;
                if (p.objectReferenceValue == null && p.objectReferenceInstanceIDValue != 0)
                {
                    r.Check(false, $"{label} prefab ({prefabName}) has a missing reference: {c.GetType().Name}.{p.propertyPath} on '{objectName}'.");
                }
            }
        }

        // ------------------------------------------------------------------ Materials & levels

        static void CheckMaterials(Report r, MaterialLibrary lib)
        {
            foreach (var key in MaterialLibrary.GeometryKeys) r.Check(lib.Get(key) != null, $"Material library has no '{key}' material.");
            foreach (var key in MaterialLibrary.SkyboxKeys) r.Check(lib.GetSkybox(key) != null, $"Material library has no '{key}' skybox.", true);
            r.Check(lib.rope != null, "Material library: rope material missing.");
            r.Check(lib.laser != null, "Material library: laser material missing.");
            r.Check(lib.gizmoX != null && lib.gizmoY != null && lib.gizmoZ != null, "Material library: editor gizmo materials missing.");
        }

        static void CheckLevels(Report r, LevelRegistry registry)
        {
            r.Check(registry.campaign.Count == CampaignLevels.All.Length, $"Level registry lists {registry.campaign.Count} levels (expected {CampaignLevels.All.Length}).");
            var buildScenes = new HashSet<string>(EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => System.IO.Path.GetFileNameWithoutExtension(s.path)));
            var ids = new HashSet<string>();
            foreach (var level in registry.campaign)
            {
                if (level == null)
                {
                    r.Check(false, "Level registry has an empty entry.");
                    continue;
                }
                r.Check(ids.Add(level.id), $"Duplicate level id '{level.id}'.");
                r.Check(!string.IsNullOrEmpty(level.sceneName), $"Level '{level.displayName}' has no scene name.");
                r.Check(buildScenes.Contains(level.sceneName), $"Level '{level.displayName}': scene '{level.sceneName}' is not in Build Settings.");
                r.Check(level.rankTimes.IsValid, $"Level '{level.displayName}' has invalid rank times (S <= A <= B <= C required).");
            }
        }

        static void CheckScenes(Report r, GameConfig config)
        {
            var paths = new List<string> { GamePaths.MainMenuScene, GamePaths.LevelEditorScene, GamePaths.CustomLevelScene };
            foreach (var e in CampaignLevels.All) paths.Add(SceneGenerator.CampaignScenePath(e));
            var build = EditorBuildSettings.scenes;
            foreach (var path in paths)
            {
                bool exists = AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null;
                r.Check(exists, "Scene missing: " + path);
                if (exists) r.Check(build.Any(s => s.path == path && s.enabled), "Scene not enabled in Build Settings: " + path);
            }
            if (build.Length > 0) r.Check(build[0].path == GamePaths.MainMenuScene, "The main menu should be the first scene in Build Settings.", true);
            if (config != null)
            {
                var names = new HashSet<string>(build.Select(s => System.IO.Path.GetFileNameWithoutExtension(s.path)));
                r.Check(names.Contains(config.mainMenuScene), $"GameConfig.mainMenuScene '{config.mainMenuScene}' is not in Build Settings.");
                r.Check(names.Contains(config.levelEditorScene), $"GameConfig.levelEditorScene '{config.levelEditorScene}' is not in Build Settings.");
                r.Check(names.Contains(config.customLevelScene), $"GameConfig.customLevelScene '{config.customLevelScene}' is not in Build Settings.");
            }
        }
    }
}

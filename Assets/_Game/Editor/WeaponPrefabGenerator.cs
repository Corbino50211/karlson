using System.Collections.Generic;
using Momentum.Audio;
using Momentum.Weapons;
using UnityEditor;
using UnityEngine;

namespace Momentum.EditorTools
{
    /// <summary>
    /// Creates the eight WeaponDefinition assets (stats) and their procedural first-person view models
    /// and world models (used by pickups).
    /// </summary>
    public static class WeaponPrefabGenerator
    {
        class Spec
        {
            public string id;
            public string name;
            public int slot;
            public Color accent;
            public SoundId sound;
            public System.Action<WeaponDefinition> configure;
        }

        static MaterialLibrary lib;

        static readonly Spec[] Specs =
        {
            new Spec { id = "pistol", name = "Pistol", slot = 1, accent = new Color(0.3f, 0.85f, 1f), sound = SoundId.Pistol, configure = d =>
            {
                d.fireMode = WeaponFireMode.SemiAuto; d.damage = 24f; d.fireRate = 5.5f; d.spread = 0.5f; d.aimSpread = 0.1f;
                d.magazineSize = 12; d.maxReserve = 999; d.startingReserve = 999; d.infiniteReserve = true; d.reloadTime = 1.0f; d.pickupAmmo = 0;
                d.recoilPitch = 1.6f; d.recoilYaw = 0.4f; d.viewKick = 0.05f; d.viewKickRotation = 7f; d.cameraShake = 0.05f;
                d.targetKnockback = 2.5f; d.altFire = AltFireMode.Zoom; d.zoomFovMultiplier = 0.75f; d.speedSpread = 0.15f;
            }},
            new Spec { id = "smg", name = "SMG", slot = 2, accent = new Color(1f, 0.85f, 0.2f), sound = SoundId.SMG, configure = d =>
            {
                d.fireMode = WeaponFireMode.FullAuto; d.damage = 11f; d.fireRate = 15f; d.spread = 2.2f; d.aimSpread = 0.9f;
                d.magazineSize = 36; d.maxReserve = 288; d.startingReserve = 144; d.reloadTime = 1.4f; d.pickupAmmo = 72;
                d.recoilPitch = 0.6f; d.recoilYaw = 0.5f; d.viewKick = 0.025f; d.viewKickRotation = 2.5f; d.cameraShake = 0.025f;
                d.targetKnockback = 1.2f; d.tracerChance = 0.5f; d.altFire = AltFireMode.Zoom; d.zoomFovMultiplier = 0.8f; d.speedSpread = 0.3f;
            }},
            new Spec { id = "shotgun", name = "Shotgun", slot = 3, accent = new Color(1f, 0.45f, 0.1f), sound = SoundId.Shotgun, configure = d =>
            {
                d.fireMode = WeaponFireMode.SemiAuto; d.damage = 9f; d.pelletsPerShot = 10; d.fireRate = 1.35f; d.spread = 6f; d.aimSpread = 6f;
                d.magazineSize = 6; d.maxReserve = 48; d.startingReserve = 24; d.reloadTime = 1.6f; d.pickupAmmo = 12;
                d.recoilPitch = 5f; d.recoilYaw = 1f; d.viewKick = 0.14f; d.viewKickRotation = 16f; d.cameraShake = 0.2f;
                d.targetKnockback = 5f; d.selfKnockback = 14f; d.groundedSelfKnockbackMultiplier = 1f; d.surfaceBoostRange = 4f; d.surfaceBoostMultiplier = 1.15f;
                d.altFire = AltFireMode.DoubleShot; d.tracerChance = 0.6f; d.moveSpeedMultiplier = 0.97f; d.range = 120f; d.speedSpread = 0f;
            }},
            new Spec { id = "rifle", name = "Assault Rifle", slot = 4, accent = new Color(0.35f, 1f, 0.45f), sound = SoundId.Rifle, configure = d =>
            {
                d.fireMode = WeaponFireMode.FullAuto; d.damage = 17f; d.fireRate = 9.5f; d.spread = 1.3f; d.aimSpread = 0.25f;
                d.magazineSize = 30; d.maxReserve = 240; d.startingReserve = 120; d.reloadTime = 1.7f; d.pickupAmmo = 60;
                d.recoilPitch = 1.1f; d.recoilYaw = 0.45f; d.viewKick = 0.035f; d.viewKickRotation = 3.5f; d.cameraShake = 0.04f;
                d.targetKnockback = 2f; d.tracerChance = 0.7f; d.altFire = AltFireMode.Zoom; d.zoomFovMultiplier = 0.55f; d.moveSpeedMultiplier = 0.96f; d.speedSpread = 0.25f;
            }},
            new Spec { id = "revolver", name = "Revolver", slot = 5, accent = new Color(1f, 0.3f, 0.25f), sound = SoundId.Revolver, configure = d =>
            {
                d.fireMode = WeaponFireMode.SemiAuto; d.damage = 62f; d.fireRate = 2.2f; d.spread = 0.15f; d.aimSpread = 0.05f;
                d.magazineSize = 6; d.maxReserve = 48; d.startingReserve = 24; d.reloadTime = 1.9f; d.pickupAmmo = 12;
                d.recoilPitch = 5.5f; d.recoilYaw = 1.2f; d.viewKick = 0.1f; d.viewKickRotation = 22f; d.cameraShake = 0.12f;
                d.targetKnockback = 8f; d.selfKnockback = 1.5f; d.altFire = AltFireMode.Fan; d.fanShots = 3; d.fanInterval = 0.08f; d.fanSpread = 3f; d.speedSpread = 0.1f;
            }},
            new Spec { id = "rocket", name = "Rocket Launcher", slot = 6, accent = new Color(1f, 0.55f, 0.1f), sound = SoundId.RocketLaunch, configure = d =>
            {
                d.fireMode = WeaponFireMode.SemiAuto; d.damage = 40f; d.fireRate = 1.1f; d.spread = 0f; d.aimSpread = 0f;
                d.magazineSize = 4; d.maxReserve = 24; d.startingReserve = 12; d.reloadTime = 2f; d.pickupAmmo = 6;
                d.recoilPitch = 3.5f; d.recoilYaw = 0.5f; d.viewKick = 0.15f; d.viewKickRotation = 8f; d.cameraShake = 0.15f;
                d.projectileSpeed = 38f; d.projectileGravity = 0f; d.projectileLifetime = 6f;
                d.explosionRadius = 5f; d.explosionDamage = 85f; d.explosionForce = 19f; d.selfDamageMultiplier = 0.08f; d.playerExplosionForceMultiplier = 1.1f;
                d.altFire = AltFireMode.Detonate; d.moveSpeedMultiplier = 0.92f; d.tracerChance = 0f; d.speedSpread = 0f;
            }},
            new Spec { id = "grenade", name = "Grenade Launcher", slot = 7, accent = new Color(0.4f, 1f, 0.5f), sound = SoundId.GrenadeLaunch, configure = d =>
            {
                d.fireMode = WeaponFireMode.SemiAuto; d.damage = 35f; d.fireRate = 1.5f; d.spread = 0.5f; d.aimSpread = 0.5f;
                d.magazineSize = 6; d.maxReserve = 36; d.startingReserve = 18; d.reloadTime = 2.2f; d.pickupAmmo = 6;
                d.recoilPitch = 2.5f; d.recoilYaw = 0.4f; d.viewKick = 0.1f; d.viewKickRotation = 7f; d.cameraShake = 0.1f;
                d.projectileSpeed = 25f; d.projectileGravity = 0f; d.grenadeFuse = 2.2f;
                d.explosionRadius = 4.5f; d.explosionDamage = 75f; d.explosionForce = 17f; d.selfDamageMultiplier = 0.08f; d.playerExplosionForceMultiplier = 1f;
                d.altFire = AltFireMode.Detonate; d.moveSpeedMultiplier = 0.94f; d.tracerChance = 0f; d.speedSpread = 0f;
            }},
            new Spec { id = "railgun", name = "Railgun", slot = 8, accent = new Color(0.6f, 0.45f, 1f), sound = SoundId.Railgun, configure = d =>
            {
                d.fireMode = WeaponFireMode.SemiAuto; d.damage = 130f; d.fireRate = 0.8f; d.spread = 0f; d.aimSpread = 0f;
                d.magazineSize = 4; d.maxReserve = 24; d.startingReserve = 12; d.reloadTime = 2.3f; d.pickupAmmo = 6;
                d.recoilPitch = 8f; d.recoilYaw = 1.5f; d.viewKick = 0.18f; d.viewKickRotation = 18f; d.cameraShake = 0.25f;
                d.targetKnockback = 15f; d.selfKnockback = 6f; d.penetration = 4; d.trailWidth = 0.16f; d.range = 400f;
                d.tracerColor = new Color(0.6f, 0.5f, 1f); d.altFire = AltFireMode.Zoom; d.zoomFovMultiplier = 0.35f; d.moveSpeedMultiplier = 0.9f; d.speedSpread = 0f;
            }}
        };

        public static List<WeaponDefinition> Generate(PrefabRegistry registry, MaterialLibrary library)
        {
            EditorUtil.Require(registry, "PrefabRegistry");
            EditorUtil.Require(library, "MaterialLibrary");
            lib = library;
            EditorUtil.EnsureFolder(GamePaths.WeaponDefinitions);
            EditorUtil.EnsureFolder(GamePaths.WeaponPrefabs);
            EditorUtil.EnsureFolder(GamePaths.WeaponMaterials);
            var result = new List<WeaponDefinition>();

            foreach (var spec in Specs)
            {
                var def = EditorUtil.LoadOrCreate<WeaponDefinition>(GamePaths.WeaponDefinitions + "/" + spec.id + ".asset", out bool created);
                if (created)
                {
                    def.id = spec.id;
                    def.displayName = spec.name;
                    def.slot = spec.slot;
                    def.accentColor = spec.accent;
                    def.fireSound = spec.sound;
                    def.tracerColor = new Color(1f, 0.85f, 0.5f);
                    spec.configure(def);
                }

                var accent = AccentMaterial(spec.id, def.accentColor);
                def.viewModelPrefab = BuildViewModel(spec.id, def, accent);
                def.worldModelPrefab = BuildWorldModel(spec.id, accent);
                if (spec.id == "rocket") def.projectilePrefab = registry.rocket;
                else if (spec.id == "grenade") def.projectilePrefab = registry.grenade;
                EditorUtility.SetDirty(def);
                result.Add(def);
            }

            registry.weapons = result;
            registry.InvalidateCaches();
            EditorUtility.SetDirty(registry);
            return result;
        }

        static Material AccentMaterial(string id, Color color)
        {
            string path = GamePaths.WeaponMaterials + "/Weapon_" + id + "_Accent.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_Color", color);
            mat.SetFloat("_Glossiness", 0.5f);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * 1.4f);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static GameObject BuildViewModel(string id, WeaponDefinition def, Material accent)
        {
            var root = new GameObject("Weapon_" + id);
            WeaponBase weapon;
            switch (id)
            {
                case "shotgun": weapon = root.AddComponent<ShotgunWeapon>(); break;
                case "rocket":
                    weapon = root.AddComponent<ProjectileWeapon>();
                    EditorUtil.Set(weapon, "grenadeLauncher", false);
                    break;
                case "grenade":
                    weapon = root.AddComponent<ProjectileWeapon>();
                    EditorUtil.Set(weapon, "grenadeLauncher", true);
                    break;
                case "railgun": weapon = root.AddComponent<RailgunWeapon>(); break;
                default: weapon = root.AddComponent<HitscanWeapon>(); break;
            }

            var model = new GameObject("Model");
            model.transform.SetParent(root.transform, false);
            model.transform.localPosition = new Vector3(0.22f, -0.21f, 0.36f);
            var muzzle = BuildModel(id, model.transform, accent);

            var flash = new GameObject("MuzzleFlash");
            flash.transform.SetParent(muzzle, false);
            var ps = EffectPrefabGenerator.AddParticles(flash, new EffectPrefabGenerator.ParticleSpec
            {
                burst = 6, lifetime = new Vector2(0.03f, 0.06f), speed = new Vector2(0.5f, 2f), size = new Vector2(0.08f, 0.18f),
                colorA = new Color(1f, 0.85f, 0.4f), colorB = def.accentColor, shape = ParticleSystemShapeType.Cone, angle = 15f, radius = 0.01f,
                additive = true, duration = 0.08f, endSize = 0.4f
            });
            var psMain = ps.main;
            psMain.playOnAwake = false;
            psMain.simulationSpace = ParticleSystemSimulationSpace.Local;

            var lightGo = new GameObject("MuzzleLight");
            lightGo.transform.SetParent(muzzle, false);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = Color.Lerp(new Color(1f, 0.7f, 0.3f), def.accentColor, 0.3f);
            light.range = 5f;
            light.intensity = 2.5f;
            light.shadows = LightShadows.None;
            light.enabled = false;

            EditorUtil.Set(weapon, "definition", def);
            EditorUtil.Set(weapon, "muzzle", muzzle);
            EditorUtil.Set(weapon, "muzzleFlash", ps);
            EditorUtil.Set(weapon, "muzzleLight", light);

            EditorUtil.SetLayerRecursive(root, Layers.ViewModel);
            EditorUtil.DisableShadows(root);
            return EditorUtil.SavePrefab(root, GamePaths.WeaponPrefabs + "/Weapon_" + id + ".prefab");
        }

        static GameObject BuildWorldModel(string id, Material accent)
        {
            var root = new GameObject("WeaponWorld_" + id);
            var model = new GameObject("Model");
            model.transform.SetParent(root.transform, false);
            model.transform.localScale = Vector3.one * 1.8f;
            model.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            BuildModel(id, model.transform, accent);
            return EditorUtil.SavePrefab(root, GamePaths.WeaponPrefabs + "/WeaponWorld_" + id + ".prefab");
        }

        /// <summary>Builds the primitive weapon model under parent. Returns the muzzle transform.</summary>
        static Transform BuildModel(string id, Transform parent, Material accent)
        {
            var body = lib.Get("WeaponBody");
            var metal = lib.Get("WeaponMetal");
            Vector3 muzzle;
            var p = parent;
            const float barrel = 90f;

            switch (id)
            {
                case "pistol":
                    EditorUtil.Box("Slide", p, metal, new Vector3(0f, 0.03f, 0.05f), new Vector3(0.06f, 0.07f, 0.24f));
                    EditorUtil.Box("Frame", p, body, new Vector3(0f, -0.02f, 0.03f), new Vector3(0.055f, 0.045f, 0.2f));
                    EditorUtil.Box("Grip", p, body, new Vector3(0f, -0.09f, -0.04f), new Vector3(0.05f, 0.13f, 0.07f), new Vector3(15f, 0f, 0f));
                    EditorUtil.Box("Stripe", p, accent, new Vector3(0f, 0.068f, 0.05f), new Vector3(0.062f, 0.012f, 0.18f));
                    muzzle = new Vector3(0f, 0.03f, 0.18f);
                    break;
                case "smg":
                    EditorUtil.Box("Body", p, body, new Vector3(0f, 0f, 0.05f), new Vector3(0.07f, 0.1f, 0.34f));
                    EditorUtil.Cylinder("Barrel", p, metal, new Vector3(0f, 0.015f, 0.26f), new Vector3(0.035f, 0.08f, 0.035f), new Vector3(barrel, 0f, 0f));
                    EditorUtil.Box("Mag", p, metal, new Vector3(0f, -0.12f, 0.08f), new Vector3(0.04f, 0.17f, 0.06f));
                    EditorUtil.Box("Grip", p, body, new Vector3(0f, -0.1f, -0.06f), new Vector3(0.05f, 0.12f, 0.06f), new Vector3(12f, 0f, 0f));
                    EditorUtil.Box("Stock", p, body, new Vector3(0f, 0f, -0.18f), new Vector3(0.04f, 0.06f, 0.16f));
                    EditorUtil.Box("Stripe", p, accent, new Vector3(0f, 0.052f, 0.05f), new Vector3(0.072f, 0.02f, 0.2f));
                    muzzle = new Vector3(0f, 0.015f, 0.35f);
                    break;
                case "shotgun":
                    EditorUtil.Box("Receiver", p, body, new Vector3(0f, 0f, -0.02f), new Vector3(0.08f, 0.1f, 0.26f));
                    EditorUtil.Cylinder("BarrelL", p, metal, new Vector3(-0.022f, 0.025f, 0.33f), new Vector3(0.035f, 0.24f, 0.035f), new Vector3(barrel, 0f, 0f));
                    EditorUtil.Cylinder("BarrelR", p, metal, new Vector3(0.022f, 0.025f, 0.33f), new Vector3(0.035f, 0.24f, 0.035f), new Vector3(barrel, 0f, 0f));
                    EditorUtil.Box("Pump", p, accent, new Vector3(0f, -0.035f, 0.28f), new Vector3(0.075f, 0.05f, 0.16f));
                    EditorUtil.Box("Stock", p, body, new Vector3(0f, -0.03f, -0.25f), new Vector3(0.06f, 0.09f, 0.22f));
                    muzzle = new Vector3(0f, 0.025f, 0.58f);
                    break;
                case "rifle":
                    EditorUtil.Box("Body", p, body, new Vector3(0f, 0f, 0.05f), new Vector3(0.07f, 0.12f, 0.5f));
                    EditorUtil.Cylinder("Barrel", p, metal, new Vector3(0f, 0.02f, 0.42f), new Vector3(0.03f, 0.14f, 0.03f), new Vector3(barrel, 0f, 0f));
                    EditorUtil.Box("Mag", p, metal, new Vector3(0f, -0.13f, 0.12f), new Vector3(0.045f, 0.18f, 0.08f), new Vector3(-12f, 0f, 0f));
                    EditorUtil.Box("Scope", p, metal, new Vector3(0f, 0.095f, 0.05f), new Vector3(0.05f, 0.05f, 0.16f));
                    EditorUtil.Box("Lens", p, accent, new Vector3(0f, 0.095f, 0.135f), new Vector3(0.04f, 0.04f, 0.01f));
                    EditorUtil.Box("Stock", p, body, new Vector3(0f, -0.02f, -0.28f), new Vector3(0.05f, 0.1f, 0.2f));
                    EditorUtil.Box("Stripe", p, accent, new Vector3(0f, 0.062f, 0.2f), new Vector3(0.072f, 0.015f, 0.22f));
                    muzzle = new Vector3(0f, 0.02f, 0.57f);
                    break;
                case "revolver":
                    EditorUtil.Box("Frame", p, body, new Vector3(0f, 0.01f, 0f), new Vector3(0.05f, 0.08f, 0.16f));
                    EditorUtil.Cylinder("Drum", p, metal, new Vector3(0f, 0.012f, 0.01f), new Vector3(0.08f, 0.04f, 0.08f), new Vector3(barrel, 0f, 0f));
                    EditorUtil.Cylinder("Barrel", p, metal, new Vector3(0f, 0.03f, 0.17f), new Vector3(0.032f, 0.12f, 0.032f), new Vector3(barrel, 0f, 0f));
                    EditorUtil.Cylinder("Ring", p, accent, new Vector3(0f, 0.03f, 0.26f), new Vector3(0.04f, 0.01f, 0.04f), new Vector3(barrel, 0f, 0f));
                    EditorUtil.Box("Grip", p, body, new Vector3(0f, -0.08f, -0.08f), new Vector3(0.045f, 0.12f, 0.06f), new Vector3(25f, 0f, 0f));
                    muzzle = new Vector3(0f, 0.03f, 0.3f);
                    break;
                case "rocket":
                    EditorUtil.Cylinder("Tube", p, body, new Vector3(0f, 0.02f, 0.1f), new Vector3(0.16f, 0.42f, 0.16f), new Vector3(barrel, 0f, 0f));
                    EditorUtil.Cylinder("FrontRing", p, accent, new Vector3(0f, 0.02f, 0.5f), new Vector3(0.19f, 0.03f, 0.19f), new Vector3(barrel, 0f, 0f));
                    EditorUtil.Cylinder("BackRing", p, accent, new Vector3(0f, 0.02f, -0.3f), new Vector3(0.19f, 0.03f, 0.19f), new Vector3(barrel, 0f, 0f));
                    EditorUtil.Box("Grip", p, body, new Vector3(0f, -0.12f, 0.05f), new Vector3(0.05f, 0.12f, 0.06f));
                    EditorUtil.Box("Sight", p, metal, new Vector3(-0.06f, 0.12f, 0.15f), new Vector3(0.03f, 0.06f, 0.06f));
                    muzzle = new Vector3(0f, 0.02f, 0.55f);
                    break;
                case "grenade":
                    EditorUtil.Cylinder("Drum", p, metal, new Vector3(0f, -0.02f, 0.05f), new Vector3(0.17f, 0.08f, 0.17f), new Vector3(barrel, 0f, 0f));
                    EditorUtil.Cylinder("Barrel", p, body, new Vector3(0f, 0.02f, 0.28f), new Vector3(0.1f, 0.2f, 0.1f), new Vector3(barrel, 0f, 0f));
                    EditorUtil.Cylinder("Ring", p, accent, new Vector3(0f, 0.02f, 0.46f), new Vector3(0.12f, 0.02f, 0.12f), new Vector3(barrel, 0f, 0f));
                    EditorUtil.Box("Body", p, body, new Vector3(0f, 0.05f, -0.05f), new Vector3(0.07f, 0.08f, 0.22f));
                    EditorUtil.Box("Grip", p, body, new Vector3(0f, -0.1f, -0.08f), new Vector3(0.05f, 0.12f, 0.06f), new Vector3(12f, 0f, 0f));
                    muzzle = new Vector3(0f, 0.02f, 0.5f);
                    break;
                default: // railgun
                    EditorUtil.Box("Body", p, body, new Vector3(0f, 0f, 0.08f), new Vector3(0.08f, 0.1f, 0.6f));
                    EditorUtil.Box("RailL", p, accent, new Vector3(-0.035f, 0.065f, 0.2f), new Vector3(0.015f, 0.02f, 0.55f));
                    EditorUtil.Box("RailR", p, accent, new Vector3(0.035f, 0.065f, 0.2f), new Vector3(0.015f, 0.02f, 0.55f));
                    for (int i = 0; i < 3; i++)
                    {
                        EditorUtil.Cylinder("Coil" + i, p, metal, new Vector3(0f, 0f, 0.1f + i * 0.12f), new Vector3(0.11f, 0.02f, 0.11f), new Vector3(barrel, 0f, 0f));
                    }
                    EditorUtil.Cylinder("Barrel", p, metal, new Vector3(0f, 0.01f, 0.48f), new Vector3(0.04f, 0.12f, 0.04f), new Vector3(barrel, 0f, 0f));
                    EditorUtil.Box("Stock", p, body, new Vector3(0f, -0.03f, -0.3f), new Vector3(0.05f, 0.1f, 0.18f));
                    muzzle = new Vector3(0f, 0.01f, 0.62f);
                    break;
            }

            var muzzleGo = new GameObject("Muzzle");
            muzzleGo.transform.SetParent(parent, false);
            muzzleGo.transform.localPosition = muzzle;
            return muzzleGo.transform;
        }
    }
}

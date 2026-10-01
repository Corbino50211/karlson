using Momentum.Bosses;
using UnityEngine;

namespace Momentum.EditorTools
{
    /// <summary>
    /// Builds the four boss prefabs (The Warden, The Sentinel, The Juggernaut, The Core) and the weak point
    /// used in The Core's final stage. Bosses are kinematic bodies driven by their BossBase subclass; attack
    /// tables are filled with each boss's defaults so they can be tuned in the prefab afterwards.
    /// </summary>
    public static class BossPrefabGenerator
    {
        static MaterialLibrary lib;
        static PrefabRegistry reg;

        public static void Generate(PrefabRegistry registry, MaterialLibrary library)
        {
            lib = library;
            reg = registry;
            EditorUtil.EnsureFolder(GamePaths.BossPrefabs);

            registry.weakPoint = EditorUtil.SavePrefab(WeakPoint(), GamePaths.BossPrefabs + "/BossWeakPoint.prefab");
            Save(BossType.Warden, Warden());
            Save(BossType.Sentinel, Sentinel());
            Save(BossType.Juggernaut, Juggernaut());
            Save(BossType.Core, Core());
        }

        static void Save(BossType type, GameObject root)
        {
            var prefab = EditorUtil.SavePrefab(root, GamePaths.BossPrefabs + "/Boss_" + type + ".prefab");
            reg.SetBossPrefab(type, prefab);
        }

        static Material M(string key) => lib.Get(key);

        static GameObject BossRoot(string name)
        {
            var root = EditorUtil.Create(name, null, Layers.Enemy);
            root.tag = Tags.Boss;
            var rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            root.AddComponent<BossHealth>();
            root.AddComponent<HitFlash>();
            return root;
        }

        static T AddBoss<T>(GameObject root, Transform visual, float health) where T : BossBase
        {
            var boss = root.AddComponent<T>();
            boss.PopulateDefaultAttacks();
            EditorUtil.Set(boss, "maxHealth", health);
            EditorUtil.Set(boss, "visualRoot", visual);
            EditorUtil.Set(root.GetComponent<BossHealth>(), "maxHealth", health);
            return boss;
        }

        static void CriticalHitbox(Transform parent, string name, Vector3 localPos, float radius, float multiplier, Health owner)
        {
            var go = EditorUtil.Create(name, parent, Layers.Enemy);
            go.transform.localPosition = localPos;
            var col = go.AddComponent<SphereCollider>();
            col.radius = radius;
            var hitbox = go.AddComponent<Hitbox>();
            EditorUtil.Set(hitbox, "owner", owner);
            EditorUtil.Set(hitbox, "damageMultiplier", multiplier);
            EditorUtil.Set(hitbox, "critical", true);
        }

        // ------------------------------------------------------------------ Weak point

        static GameObject WeakPoint()
        {
            var go = EditorUtil.Create("BossWeakPoint", null, Layers.Enemy);
            go.tag = Tags.Enemy;
            var col = go.AddComponent<SphereCollider>();
            col.radius = 0.85f;
            var health = go.AddComponent<Health>();
            EditorUtil.Set(health, "maxHealth", 160f);
            go.AddComponent<HitFlash>();
            go.AddComponent<BossWeakPoint>();
            EditorUtil.Sphere("Core", go.transform, M("BossCrimson"), Vector3.zero, Vector3.one * 1.2f);
            var spin = EditorUtil.Create("Cage", go.transform, Layers.Enemy);
            var s = spin.AddComponent<SpinAndBob>();
            EditorUtil.Set(s, "spinSpeed", 120f);
            EditorUtil.Set(s, "bobHeight", 0.25f);
            EditorUtil.Visual("RingA", spin.transform, MeshAssetGenerator.Torus, M("BossDark"), Vector3.zero, Vector3.one * 0.9f, new Vector3(90f, 0f, 0f));
            EditorUtil.Visual("RingB", spin.transform, MeshAssetGenerator.Torus, M("BossDark"), Vector3.zero, Vector3.one * 0.9f, new Vector3(0f, 0f, 90f));
            for (int i = 0; i < 4; i++)
            {
                float a = i * 90f;
                Vector3 dir = Quaternion.Euler(0f, a, 0f) * Vector3.forward;
                EditorUtil.Visual("Spike" + i, spin.transform, MeshAssetGenerator.Cone, M("BossDark"), dir * 0.65f, new Vector3(0.25f, 0.5f, 0.25f), Quaternion.FromToRotation(Vector3.up, dir).eulerAngles);
            }
            var glow = EditorUtil.Create("Glow", go.transform, Layers.Enemy);
            var light = glow.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.15f, 0.2f);
            light.range = 6f;
            light.intensity = 2f;
            light.shadows = LightShadows.None;
            return go;
        }

        // ------------------------------------------------------------------ The Warden

        static GameObject Warden()
        {
            var root = BossRoot("Boss_Warden");
            var capsule = root.AddComponent<CapsuleCollider>();
            capsule.radius = 1.3f;
            capsule.height = 4.0f;
            capsule.center = new Vector3(0f, 2.0f, 0f);
            var health = root.GetComponent<BossHealth>();

            var v = EditorUtil.Create("Visual", root.transform, Layers.Enemy).transform;
            var body = M("BossBody");
            var dark = M("BossDark");
            var glow = M("BossCrimson");
            EditorUtil.Box("LegL", v, dark, new Vector3(-0.6f, 0.9f, 0f), new Vector3(0.7f, 1.8f, 0.9f));
            EditorUtil.Box("LegR", v, dark, new Vector3(0.6f, 0.9f, 0f), new Vector3(0.7f, 1.8f, 0.9f));
            EditorUtil.Box("Pelvis", v, body, new Vector3(0f, 1.95f, 0f), new Vector3(1.9f, 0.5f, 1.2f));
            EditorUtil.Box("Torso", v, body, new Vector3(0f, 3.0f, 0f), new Vector3(2.4f, 1.7f, 1.6f));
            EditorUtil.Box("ChestPlate", v, glow, new Vector3(0f, 3.15f, 0.81f), new Vector3(1.2f, 0.5f, 0.06f));
            EditorUtil.Box("ShoulderL", v, dark, new Vector3(-1.55f, 3.7f, 0f), new Vector3(0.9f, 0.7f, 1.4f));
            EditorUtil.Box("ShoulderR", v, dark, new Vector3(1.55f, 3.7f, 0f), new Vector3(0.9f, 0.7f, 1.4f));
            EditorUtil.Box("ArmL", v, body, new Vector3(-1.6f, 2.6f, 0.2f), new Vector3(0.6f, 1.6f, 0.7f));
            EditorUtil.Box("FistL", v, dark, new Vector3(-1.6f, 1.65f, 0.3f), new Vector3(0.85f, 0.7f, 0.85f));
            // Right arm is a shotgun cannon.
            EditorUtil.Box("ArmR", v, body, new Vector3(1.6f, 2.8f, 0.3f), new Vector3(0.7f, 0.7f, 1.4f));
            EditorUtil.Visual("CannonA", v, EditorUtil.CylinderMesh, M("WeaponMetal"), new Vector3(1.45f, 2.85f, 1.4f), new Vector3(0.3f, 0.6f, 0.3f), new Vector3(90f, 0f, 0f));
            EditorUtil.Visual("CannonB", v, EditorUtil.CylinderMesh, M("WeaponMetal"), new Vector3(1.75f, 2.85f, 1.4f), new Vector3(0.3f, 0.6f, 0.3f), new Vector3(90f, 0f, 0f));
            var muzzle = EditorUtil.Create("ShotgunMuzzle", v, Layers.Enemy);
            muzzle.transform.localPosition = new Vector3(1.6f, 2.85f, 2.05f);
            // Head with a glowing visor.
            EditorUtil.Box("Head", v, body, new Vector3(0f, 4.25f, 0.15f), new Vector3(0.9f, 0.75f, 0.9f));
            EditorUtil.Box("Visor", v, glow, new Vector3(0f, 4.3f, 0.61f), new Vector3(0.7f, 0.16f, 0.06f));
            // Missile pod on the back.
            EditorUtil.Box("Pod", v, dark, new Vector3(0f, 4.0f, -1.0f), new Vector3(1.6f, 0.9f, 0.7f));
            for (int i = 0; i < 3; i++)
            {
                EditorUtil.Visual("PodTube" + i, v, EditorUtil.CylinderMesh, glow, new Vector3(-0.5f + i * 0.5f, 4.48f, -1.0f), new Vector3(0.28f, 0.05f, 0.28f));
            }
            var pod = EditorUtil.Create("MissilePod", v, Layers.Enemy);
            pod.transform.localPosition = new Vector3(0f, 4.7f, -1.0f);

            CriticalHitbox(root.transform, "HeadHitbox", new Vector3(0f, 4.35f, 0.2f), 0.55f, 1.6f, health);

            var boss = AddBoss<WardenBoss>(root, v, 2600f);
            EditorUtil.Set(boss, "shotgunMuzzle", muzzle.transform);
            EditorUtil.Set(boss, "missilePod", pod.transform);
            return root;
        }

        // ------------------------------------------------------------------ The Sentinel

        static GameObject Sentinel()
        {
            var root = BossRoot("Boss_Sentinel");
            var sphere = root.AddComponent<SphereCollider>();
            sphere.radius = 2.1f;
            sphere.center = new Vector3(0f, 2.4f, 0f);
            var health = root.GetComponent<BossHealth>();

            var v = EditorUtil.Create("Visual", root.transform, Layers.Enemy).transform;
            var center = new Vector3(0f, 2.4f, 0f);
            EditorUtil.Sphere("Shell", v, M("BossBody"), center, Vector3.one * 4.2f);
            EditorUtil.Visual("Equator", v, EditorUtil.CylinderMesh, M("BossDark"), center, new Vector3(4.3f, 0.12f, 4.3f));

            var eye = EditorUtil.Create("Eye", v, Layers.Enemy);
            eye.transform.localPosition = center;
            EditorUtil.Sphere("Socket", eye.transform, M("BossDark"), new Vector3(0f, 0f, 1.45f), new Vector3(1.9f, 1.9f, 1.2f));
            EditorUtil.Sphere("Iris", eye.transform, M("BossCrimson"), new Vector3(0f, 0f, 1.85f), new Vector3(1.2f, 1.2f, 0.7f));
            EditorUtil.Sphere("Pupil", eye.transform, M("BossDark"), new Vector3(0f, 0f, 2.15f), new Vector3(0.5f, 0.5f, 0.25f));
            CriticalHitbox(eye.transform, "EyeHitbox", new Vector3(0f, 0f, 1.95f), 0.6f, 1.6f, health);

            var ringA = EditorUtil.Create("RingA", v, Layers.Enemy);
            ringA.transform.localPosition = center;
            EditorUtil.Visual("Ring", ringA.transform, MeshAssetGenerator.Torus, M("BossDark"), Vector3.zero, Vector3.one * 3.0f, new Vector3(0f, 0f, 90f));
            for (int i = 0; i < 4; i++)
            {
                Vector3 p = Quaternion.Euler(i * 90f, 0f, 0f) * new Vector3(0f, 3f, 0f);
                EditorUtil.Sphere("Node" + i, ringA.transform, M("BossCrimson"), p, Vector3.one * 0.45f);
            }
            var ringB = EditorUtil.Create("RingB", v, Layers.Enemy);
            ringB.transform.localPosition = center;
            EditorUtil.Visual("Ring", ringB.transform, MeshAssetGenerator.Torus, M("BossBody"), Vector3.zero, Vector3.one * 3.5f);
            for (int i = 0; i < 6; i++)
            {
                Vector3 p = Quaternion.Euler(0f, i * 60f, 0f) * new Vector3(0f, 0f, 3.5f);
                EditorUtil.Box("Fin" + i, ringB.transform, M("BossDark"), p, new Vector3(0.25f, 0.7f, 0.5f), new Vector3(0f, i * 60f, 0f));
            }

            var boss = AddBoss<SentinelBoss>(root, v, 2200f);
            EditorUtil.Set(boss, "eye", eye.transform);
            EditorUtil.Set(boss, "ringA", ringA.transform);
            EditorUtil.Set(boss, "ringB", ringB.transform);
            EditorUtil.Set(boss, "hoverHeight", 6.5f);
            return root;
        }

        // ------------------------------------------------------------------ The Juggernaut

        static GameObject Juggernaut()
        {
            var root = BossRoot("Boss_Juggernaut");
            var capsule = root.AddComponent<CapsuleCollider>();
            capsule.radius = 1.7f;
            capsule.height = 5.4f;
            capsule.center = new Vector3(0f, 2.7f, 0f);
            var health = root.GetComponent<BossHealth>();

            var v = EditorUtil.Create("Visual", root.transform, Layers.Enemy).transform;
            var rock = M("Rock");
            var dark = M("BossDark");
            var glow = M("BossCrimson");
            EditorUtil.Box("LegL", v, dark, new Vector3(-0.9f, 0.8f, 0f), new Vector3(1.0f, 1.6f, 1.2f));
            EditorUtil.Box("LegR", v, dark, new Vector3(0.9f, 0.8f, 0f), new Vector3(1.0f, 1.6f, 1.2f));
            EditorUtil.Box("Body", v, rock, new Vector3(0f, 3.0f, 0f), new Vector3(3.2f, 2.8f, 2.4f), new Vector3(0f, 0f, 3f));
            EditorUtil.Box("BackRock", v, rock, new Vector3(0f, 4.3f, -0.6f), new Vector3(2.4f, 1.4f, 1.6f), new Vector3(12f, 8f, 0f));
            EditorUtil.Box("Belly", v, dark, new Vector3(0f, 2.4f, 1.1f), new Vector3(2.0f, 1.3f, 0.3f));
            EditorUtil.Box("Core", v, glow, new Vector3(0f, 3.1f, 1.21f), new Vector3(0.9f, 0.9f, 0.1f));
            EditorUtil.Box("Head", v, dark, new Vector3(0f, 4.4f, 0.9f), new Vector3(1.2f, 0.9f, 1.0f));
            EditorUtil.Box("Eyes", v, glow, new Vector3(0f, 4.5f, 1.41f), new Vector3(0.8f, 0.12f, 0.05f));
            EditorUtil.Box("ArmL", v, rock, new Vector3(-2.1f, 2.6f, 0.4f), new Vector3(1.1f, 2.6f, 1.1f), new Vector3(-10f, 0f, -6f));
            EditorUtil.Box("ArmR", v, rock, new Vector3(2.1f, 2.6f, 0.4f), new Vector3(1.1f, 2.6f, 1.1f), new Vector3(-10f, 0f, 6f));
            EditorUtil.Box("FistL", v, dark, new Vector3(-2.2f, 1.1f, 0.7f), new Vector3(1.4f, 1.1f, 1.4f));
            EditorUtil.Box("FistR", v, dark, new Vector3(2.2f, 1.1f, 0.7f), new Vector3(1.4f, 1.1f, 1.4f));

            var hold = EditorUtil.Create("RockHoldPoint", root.transform, Layers.Enemy);
            hold.transform.localPosition = new Vector3(0f, 7f, 0.4f);
            var held = EditorUtil.Create("HeldRock", hold.transform, Layers.Enemy);
            EditorUtil.Visual("Rock", held.transform, EditorUtil.SphereMesh, rock, Vector3.zero, new Vector3(2.4f, 2.0f, 2.2f), new Vector3(20f, 30f, 0f));
            held.SetActive(false);

            var rage = EditorUtil.Create("RageAura", root.transform, Layers.Enemy);
            var aura = EditorUtil.Visual("Aura", rage.transform, EditorUtil.SphereMesh, M("Rage"), new Vector3(0f, 2.8f, 0f), new Vector3(5.6f, 6.4f, 5.6f));
            EditorUtil.DisableShadows(aura);
            rage.SetActive(false);

            CriticalHitbox(root.transform, "CoreHitbox", new Vector3(0f, 3.1f, 1.5f), 0.55f, 1.8f, health);

            var boss = AddBoss<JuggernautBoss>(root, v, 3000f);
            EditorUtil.Set(boss, "rockHoldPoint", hold.transform);
            EditorUtil.Set(boss, "heldRockVisual", held);
            EditorUtil.Set(boss, "rageVisual", rage);
            return root;
        }

        // ------------------------------------------------------------------ The Core

        static GameObject Core()
        {
            var root = BossRoot("Boss_Core");
            var sphere = root.AddComponent<SphereCollider>();
            sphere.radius = 2.2f;
            sphere.center = new Vector3(0f, 3.2f, 0f);

            var v = EditorUtil.Create("Visual", root.transform, Layers.Enemy).transform;
            var dark = M("BossDark");
            EditorUtil.Visual("Pedestal", v, EditorUtil.CylinderMesh, dark, new Vector3(0f, 0.5f, 0f), new Vector3(3.2f, 0.5f, 3.2f));
            EditorUtil.Visual("Collar", v, EditorUtil.CylinderMesh, M("BossBody"), new Vector3(0f, 1.1f, 0f), new Vector3(2.4f, 0.12f, 2.4f));
            var coreVisual = EditorUtil.Create("CoreVisual", v, Layers.Enemy);
            coreVisual.transform.localPosition = new Vector3(0f, 3.2f, 0f);
            EditorUtil.Sphere("Heart", coreVisual.transform, M("BossCrimson"), Vector3.zero, Vector3.one * 3.0f);
            EditorUtil.Visual("RingA", coreVisual.transform, MeshAssetGenerator.Torus, dark, Vector3.zero, Vector3.one * 2.0f);
            EditorUtil.Visual("RingB", coreVisual.transform, MeshAssetGenerator.Torus, dark, Vector3.zero, Vector3.one * 2.0f, new Vector3(60f, 0f, 0f));
            EditorUtil.Visual("RingC", coreVisual.transform, MeshAssetGenerator.Torus, dark, Vector3.zero, Vector3.one * 2.0f, new Vector3(-60f, 0f, 0f));
            for (int i = 0; i < 6; i++)
            {
                Vector3 dir = Quaternion.Euler(0f, i * 60f, 0f) * Vector3.forward;
                EditorUtil.Box("Plate" + i, coreVisual.transform, M("BossBody"), dir * 1.7f, new Vector3(0.9f, 1.6f, 0.15f), Quaternion.LookRotation(dir).eulerAngles);
            }
            var lightGo = EditorUtil.Create("CoreLight", coreVisual.transform, Layers.Enemy);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.2f, 0.25f);
            light.range = 18f;
            light.intensity = 2.5f;
            light.shadows = LightShadows.None;

            var shield = EditorUtil.Create("Shield", root.transform, Layers.Enemy);
            var bubble = EditorUtil.Visual("Bubble", shield.transform, EditorUtil.SphereMesh, lib.shield, new Vector3(0f, 3.2f, 0f), Vector3.one * 5.6f);
            EditorUtil.DisableShadows(bubble);

            var boss = AddBoss<CoreBoss>(root, v, 3200f);
            EditorUtil.Set(boss, "shieldVisual", shield);
            EditorUtil.Set(boss, "coreVisual", coreVisual.transform);
            return root;
        }
    }
}

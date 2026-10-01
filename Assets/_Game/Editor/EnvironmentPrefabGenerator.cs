using Momentum.Audio;
using Momentum.Bosses;
using Momentum.Levels;
using Momentum.Weapons;
using UnityEngine;

namespace Momentum.EditorTools
{
    /// <summary>
    /// Builds every level object prefab (geometry, movement objects, gameplay triggers, pickups, props,
    /// lights, boss arena objects) and registers them in the PrefabRegistry under their catalog type.
    /// Pivot conventions follow LevelObjectCatalog: "centerPivot" objects are centered, the others sit on
    /// the ground at their origin. Scalable objects use unit-sized meshes scaled by the root transform.
    /// </summary>
    public static class EnvironmentPrefabGenerator
    {
        static MaterialLibrary lib;
        static PrefabRegistry reg;

        public static void Generate(PrefabRegistry registry, MaterialLibrary library)
        {
            lib = library;
            reg = registry;
            EditorUtil.EnsureFolder(GamePaths.EnvironmentPrefabs);
            EditorUtil.EnsureFolder(GamePaths.PickupPrefabs);

            // Geometry
            Register("Cube", SimpleGeometry("Cube", "White"));
            Register("Floor", SimpleGeometry("Floor", "LightGray"));
            Register("Wall", SimpleGeometry("Wall", "White"));
            Register("Platform", SimpleGeometry("Platform", "Gray"));
            Register("Pillar", Pillar());
            Register("Ramp", Ramp());
            Register("Stairs", Stairs());
            Register("MovingPlatform", MovingPlatformPrefab());
            Register("Door", DoorPrefab());
            Register("Glass", GlassPrefab());
            Register("BreakableWall", BreakableWallPrefab());

            // Movement
            Register("LaunchPad", LaunchPadPrefab());
            Register("SpeedPad", SpeedPadPrefab());
            Register("GrapplePoint", GrapplePointPrefab());

            // Gameplay
            Register("SpawnPoint", SpawnPointPrefab());
            Register("StartTrigger", StartTriggerPrefab());
            Register("Checkpoint", CheckpointPrefab());
            Register("FinishTrigger", FinishPrefab());
            Register("KillZone", KillZonePrefab());
            Register("HazardPanel", HazardPanelPrefab());
            Register("Secret", SecretPrefab());
            Register("Sign", SignPrefab());

            // Pickups
            Register("WeaponPickup", WeaponPickupPrefab(), GamePaths.PickupPrefabs);
            Register("AmmoPickup", AmmoPickupPrefab(), GamePaths.PickupPrefabs);
            Register("HealthPickup", HealthPickupPrefab(), GamePaths.PickupPrefabs);

            // Props
            Register("Crate", CratePrefab());
            Register("Barrel", BarrelPrefab(false));
            Register("ExplosiveBarrel", BarrelPrefab(true));

            // Lights & boss arena
            Register("Light", LightPrefab());
            Register("BossTrigger", BossTriggerPrefab());
            Register("BossSocket", BossSocketPrefab());
        }

        static void Register(string type, GameObject root, string folder = null)
        {
            string path = (folder ?? GamePaths.EnvironmentPrefabs) + "/" + type + ".prefab";
            var prefab = EditorUtil.SavePrefab(root, path);
            reg.SetLevelObjectPrefab(type, prefab);
        }

        static Material M(string key) => lib.Get(key);

        static GameObject Root(string name, int layer)
        {
            return EditorUtil.Create(name, null, layer);
        }

        static void AddMesh(GameObject go, Mesh mesh, Material material, bool shadows = true)
        {
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = material;
            if (!shadows)
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
        }

        static BoxCollider AddBox(GameObject go, Vector3 center, Vector3 size, bool trigger)
        {
            var box = go.AddComponent<BoxCollider>();
            box.center = center;
            box.size = size;
            box.isTrigger = trigger;
            return box;
        }

        static GameObject VisualNoShadow(string name, Transform parent, Mesh mesh, Material mat, Vector3 pos, Vector3 scale, Vector3 euler = default)
        {
            var go = EditorUtil.Visual(name, parent, mesh, mat, pos, scale, euler);
            EditorUtil.DisableShadows(go);
            return go;
        }

        static ParticleSystem AddBurstEffect(Transform parent, string name, Vector3 localPos, Color a, Color b, int count, float speed, ParticleSystemShapeType shape, float radius)
        {
            var go = EditorUtil.Create(name, parent, parent.gameObject.layer);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            var ps = EffectPrefabGenerator.AddParticles(go, new EffectPrefabGenerator.ParticleSpec
            {
                burst = count,
                lifetime = new Vector2(0.5f, 1f),
                speed = new Vector2(speed * 0.5f, speed),
                size = new Vector2(0.1f, 0.25f),
                colorA = a,
                colorB = b,
                gravity = 0.4f,
                shape = shape,
                radius = radius,
                angle = 15f,
                additive = true,
                stretched = true,
                duration = 0.5f,
                endSize = 0.2f,
                maxParticles = 120
            });
            var main = ps.main;
            main.playOnAwake = false;
            return ps;
        }

        // ------------------------------------------------------------------ Geometry

        static GameObject SimpleGeometry(string name, string materialKey)
        {
            var go = Root(name, Layers.Environment);
            go.isStatic = false;
            AddMesh(go, EditorUtil.CubeMesh, M(materialKey));
            go.AddComponent<BoxCollider>();
            var geo = go.AddComponent<LevelGeometry>();
            EditorUtil.Set(geo, "materialKey", materialKey);
            return go;
        }

        static GameObject Pillar()
        {
            var go = Root("Pillar", Layers.Environment);
            var mesh = EditorUtil.Visual("Mesh", go.transform, EditorUtil.CylinderMesh, M("White"), Vector3.zero, new Vector3(1f, 0.5f, 1f));
            var col = mesh.AddComponent<MeshCollider>();
            col.sharedMesh = EditorUtil.CylinderMesh;
            col.convex = true;
            var geo = go.AddComponent<LevelGeometry>();
            EditorUtil.Set(geo, "materialKey", "White");
            return go;
        }

        static GameObject Ramp()
        {
            var go = Root("Ramp", Layers.Environment);
            AddMesh(go, MeshAssetGenerator.Wedge, M("LightGray"));
            var col = go.AddComponent<MeshCollider>();
            col.sharedMesh = MeshAssetGenerator.Wedge;
            col.convex = true;
            var geo = go.AddComponent<LevelGeometry>();
            EditorUtil.Set(geo, "materialKey", "LightGray");
            return go;
        }

        static GameObject Stairs()
        {
            var go = Root("Stairs", Layers.Environment);
            // Smooth slope collider so movement stays fluid; the steps are purely visual.
            var col = go.AddComponent<MeshCollider>();
            col.sharedMesh = MeshAssetGenerator.Wedge;
            col.convex = true;
            const int steps = 8;
            for (int i = 0; i < steps; i++)
            {
                float h = (i + 1f) / steps;
                EditorUtil.Box("Step" + i, go.transform, M("Gray"),
                    new Vector3(0f, h * 0.5f, -0.5f + (i + 0.5f) / steps),
                    new Vector3(1f, h, 1f / steps));
            }
            var geo = go.AddComponent<LevelGeometry>();
            EditorUtil.Set(geo, "materialKey", "Gray");
            return go;
        }

        static GameObject MovingPlatformPrefab()
        {
            var go = Root("MovingPlatform", Layers.Environment);
            AddMesh(go, EditorUtil.CubeMesh, M("AccentBlue"));
            go.AddComponent<BoxCollider>();
            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            go.AddComponent<MovingPlatform>();
            // Edge trim so the moving surface reads clearly.
            VisualNoShadow("Trim", go.transform, EditorUtil.CubeMesh, M("Dark"), new Vector3(0f, -0.3f, 0f), new Vector3(1.02f, 0.45f, 1.02f));
            return go;
        }

        static GameObject DoorPrefab()
        {
            var go = Root("Door", Layers.Environment);
            var panel = EditorUtil.Box("Panel", go.transform, M("Dark"), Vector3.zero, Vector3.one);
            panel.AddComponent<BoxCollider>();
            // Accent stripes on the panel (children of the panel so they move with it).
            VisualNoShadow("StripeA", panel.transform, EditorUtil.CubeMesh, M("Accent"), new Vector3(0f, 0.15f, 0f), new Vector3(1.002f, 0.04f, 1.04f));
            VisualNoShadow("StripeB", panel.transform, EditorUtil.CubeMesh, M("Accent"), new Vector3(0f, -0.15f, 0f), new Vector3(1.002f, 0.04f, 1.04f));
            var status = VisualNoShadow("StatusLight", go.transform, EditorUtil.CubeMesh, M("LightFixture"), new Vector3(0f, 0.53f, 0f), new Vector3(0.5f, 0.03f, 1.3f));
            var door = go.AddComponent<Door>();
            EditorUtil.Set(door, "panel", panel.transform);
            EditorUtil.Set(door, "statusLight", status.GetComponent<Renderer>());
            return go;
        }

        static GameObject GlassPrefab()
        {
            var go = Root("Glass", Layers.Environment);
            AddMesh(go, EditorUtil.CubeMesh, M("Glass"), false);
            go.AddComponent<BoxCollider>();
            go.tag = Tags.NoWallRun;
            var b = go.AddComponent<BreakableObject>();
            EditorUtil.Set(b, "maxHealth", 10f);
            EditorUtil.Set(b, "breakOnPlayerImpact", true);
            EditorUtil.Set(b, "impactSpeedThreshold", 9f);
            EditorUtil.Set(b, "bossBreakable", true);
            EditorUtil.Set(b, "fragmentPrefab", reg.glassShard);
            EditorUtil.Set(b, "fragmentCount", 12);
            EditorUtil.Set(b, "fragmentsUseOwnMaterial", false);
            EditorUtil.Set(b, "breakSound", (int)SoundId.GlassBreak);
            return go;
        }

        static GameObject BreakableWallPrefab()
        {
            var go = Root("BreakableWall", Layers.Environment);
            AddMesh(go, EditorUtil.CubeMesh, M("Gray"));
            go.AddComponent<BoxCollider>();
            // Crack-like accent bars so players learn to recognise breakable walls.
            VisualNoShadow("CrackA", go.transform, EditorUtil.CubeMesh, M("Dark"), new Vector3(-0.1f, 0.1f, 0f), new Vector3(0.6f, 0.05f, 1.02f), new Vector3(0f, 0f, 35f));
            VisualNoShadow("CrackB", go.transform, EditorUtil.CubeMesh, M("Dark"), new Vector3(0.15f, -0.15f, 0f), new Vector3(0.45f, 0.05f, 1.02f), new Vector3(0f, 0f, -25f));
            VisualNoShadow("Marker", go.transform, EditorUtil.CubeMesh, M("AccentYellow"), new Vector3(0f, 0.45f, 0f), new Vector3(1.01f, 0.06f, 1.02f));
            var b = go.AddComponent<BreakableObject>();
            EditorUtil.Set(b, "maxHealth", 150f);
            EditorUtil.Set(b, "breakOnPlayerImpact", false);
            EditorUtil.Set(b, "bossBreakable", true);
            EditorUtil.Set(b, "fragmentPrefab", reg.debrisChunk);
            EditorUtil.Set(b, "fragmentCount", 14);
            EditorUtil.Set(b, "fragmentsUseOwnMaterial", true);
            EditorUtil.Set(b, "breakSound", (int)SoundId.CrateBreak);
            return go;
        }

        // ------------------------------------------------------------------ Movement objects

        static GameObject LaunchPadPrefab()
        {
            var go = Root("LaunchPad", Layers.Trigger);
            AddBox(go, new Vector3(0f, 0.6f, 0f), new Vector3(2.5f, 1.2f, 2.5f), true);
            VisualNoShadow("Base", go.transform, EditorUtil.CylinderMesh, M("Dark"), new Vector3(0f, 0.04f, 0f), new Vector3(2.6f, 0.04f, 2.6f));
            VisualNoShadow("Glow", go.transform, EditorUtil.CylinderMesh, M("LaunchPad"), new Vector3(0f, 0.085f, 0f), new Vector3(2.0f, 0.01f, 2.0f));
            VisualNoShadow("Ring", go.transform, MeshAssetGenerator.Torus, M("LaunchPad"), new Vector3(0f, 0.1f, 0f), new Vector3(1.2f, 1.2f, 1.2f));
            for (int i = 0; i < 2; i++)
            {
                VisualNoShadow("Chevron" + i, go.transform, MeshAssetGenerator.Chevron, M("Dark"), new Vector3(0f, 0.1f, -0.2f + i * 0.45f), new Vector3(1.4f, 1f, 1.4f));
            }
            var ps = AddBurstEffect(go.transform, "LaunchEffect", new Vector3(0f, 0.2f, 0f), new Color(0.3f, 1f, 0.5f), Color.white, 30, 14f, ParticleSystemShapeType.Circle, 1f);
            var pad = go.AddComponent<LaunchPad>();
            EditorUtil.Set(pad, "launchEffect", ps);
            return go;
        }

        static GameObject SpeedPadPrefab()
        {
            var go = Root("SpeedPad", Layers.Trigger);
            AddBox(go, new Vector3(0f, 0.5f, 0f), Vector3.one, true);
            VisualNoShadow("Strip", go.transform, EditorUtil.CubeMesh, M("Dark"), new Vector3(0f, 0.02f, 0f), new Vector3(1f, 0.04f, 1f));
            VisualNoShadow("Glow", go.transform, EditorUtil.CubeMesh, M("SpeedPad"), new Vector3(0f, 0.045f, 0f), new Vector3(0.85f, 0.01f, 0.94f));
            for (int i = 0; i < 3; i++)
            {
                VisualNoShadow("Chevron" + i, go.transform, MeshAssetGenerator.Chevron, M("Dark"), new Vector3(0f, 0.05f, -0.3f + i * 0.3f), new Vector3(1f, 0.3f, 0.5f));
            }
            go.AddComponent<SpeedPad>();
            return go;
        }

        static GameObject GrapplePointPrefab()
        {
            var go = Root("GrapplePoint", Layers.Grapple);
            var sphere = go.AddComponent<SphereCollider>();
            sphere.radius = 0.5f;
            var orb = VisualNoShadow("Orb", go.transform, EditorUtil.SphereMesh, M("GrappleOrb"), Vector3.zero, Vector3.one * 0.7f);
            var ringRoot = EditorUtil.Create("Rings", go.transform, Layers.Grapple);
            var spin = ringRoot.AddComponent<SpinAndBob>();
            EditorUtil.Set(spin, "spinSpeed", 70f);
            EditorUtil.Set(spin, "bobHeight", 0f);
            VisualNoShadow("RingA", ringRoot.transform, MeshAssetGenerator.Torus, M("Dark"), Vector3.zero, Vector3.one * 0.62f, new Vector3(90f, 0f, 0f));
            VisualNoShadow("RingB", ringRoot.transform, MeshAssetGenerator.Torus, M("Dark"), Vector3.zero, Vector3.one * 0.62f, new Vector3(90f, 90f, 0f));
            var gp = go.AddComponent<GrapplePoint>();
            EditorUtil.Set(gp, "highlightRenderer", orb.GetComponent<Renderer>());
            return go;
        }

        // ------------------------------------------------------------------ Gameplay objects

        static GameObject SpawnPointPrefab()
        {
            var go = Root("SpawnPoint", Layers.IgnoreRaycast);
            var marker = EditorUtil.Create("Marker", go.transform, Layers.IgnoreRaycast);
            VisualNoShadow("Body", marker.transform, EditorUtil.CapsuleMesh, M("SpawnMarker"), new Vector3(0f, 1f, 0f), new Vector3(0.8f, 1f, 0.8f));
            VisualNoShadow("Arrow", marker.transform, MeshAssetGenerator.Cone, M("SpawnMarker"), new Vector3(0f, 1.2f, 0.7f), new Vector3(0.4f, 0.7f, 0.4f), new Vector3(90f, 0f, 0f));
            VisualNoShadow("Pad", go.transform, EditorUtil.CylinderMesh, M("Dark"), new Vector3(0f, 0.02f, 0f), new Vector3(1.6f, 0.02f, 1.6f));
            VisualNoShadow("PadGlow", go.transform, MeshAssetGenerator.Torus, M("HealthGlow"), new Vector3(0f, 0.05f, 0f), new Vector3(0.75f, 1f, 0.75f));
            var sp = go.AddComponent<SpawnPoint>();
            EditorUtil.Set(sp, "markerVisual", marker);
            return go;
        }

        static GameObject StartTriggerPrefab()
        {
            var go = Root("StartTrigger", Layers.Trigger);
            AddBox(go, Vector3.zero, Vector3.one, true);
            var vis = VisualNoShadow("EditorVolume", go.transform, EditorUtil.CubeMesh, M("TriggerVolume"), Vector3.zero, Vector3.one);
            // Visible gate frame (stays visible in play).
            VisualNoShadow("PostL", go.transform, EditorUtil.CubeMesh, M("AccentBlue"), new Vector3(-0.5f, 0f, 0f), new Vector3(0.04f, 1f, 0.4f));
            VisualNoShadow("PostR", go.transform, EditorUtil.CubeMesh, M("AccentBlue"), new Vector3(0.5f, 0f, 0f), new Vector3(0.04f, 1f, 0.4f));
            VisualNoShadow("Top", go.transform, EditorUtil.CubeMesh, M("AccentBlue"), new Vector3(0f, 0.5f, 0f), new Vector3(1.04f, 0.05f, 0.4f));
            var st = go.AddComponent<StartTrigger>();
            EditorUtil.Set(st, "editorVisual", vis);
            return go;
        }

        static GameObject CheckpointPrefab()
        {
            var go = Root("Checkpoint", Layers.Trigger);
            AddBox(go, new Vector3(0f, 2f, 0f), new Vector3(4.5f, 4f, 4.5f), true);
            VisualNoShadow("Base", go.transform, EditorUtil.CylinderMesh, M("Dark"), new Vector3(0f, 0.03f, 0f), new Vector3(2.4f, 0.03f, 2.4f));
            var ring = VisualNoShadow("Ring", go.transform, MeshAssetGenerator.Torus, M("Checkpoint"), new Vector3(0f, 0.08f, 0f), new Vector3(1.1f, 1.5f, 1.1f));
            EditorUtil.Box("Pole", go.transform, M("Dark"), new Vector3(1.25f, 1.25f, 0f), new Vector3(0.12f, 2.5f, 0.12f));
            var flag = VisualNoShadow("Flag", go.transform, EditorUtil.CubeMesh, M("Checkpoint"), new Vector3(1.25f, 2.35f, 0.38f), new Vector3(0.05f, 0.4f, 0.65f));
            var fx = AddBurstEffect(go.transform, "ActivateEffect", new Vector3(0f, 0.2f, 0f), new Color(0.3f, 1f, 0.5f), Color.white, 40, 9f, ParticleSystemShapeType.Circle, 1.1f);
            var cp = go.AddComponent<Checkpoint>();
            EditorUtil.SetArray(cp, "indicatorRenderers", new Object[] { ring.GetComponent<Renderer>(), flag.GetComponent<Renderer>() });
            EditorUtil.Set(cp, "activateEffect", fx);
            return go;
        }

        static GameObject FinishPrefab()
        {
            var go = Root("FinishTrigger", Layers.Trigger);
            AddBox(go, new Vector3(0f, 2f, 0f), new Vector3(5f, 4f, 5f), true);
            VisualNoShadow("Base", go.transform, EditorUtil.CylinderMesh, M("Dark"), new Vector3(0f, 0.03f, 0f), new Vector3(4.6f, 0.03f, 4.6f));
            VisualNoShadow("BaseGlow", go.transform, MeshAssetGenerator.Torus, M("Finish"), new Vector3(0f, 0.08f, 0f), new Vector3(2.2f, 1.5f, 2.2f));

            var open = EditorUtil.Create("OpenVisual", go.transform, Layers.Trigger);
            VisualNoShadow("Beam", open.transform, EditorUtil.CylinderMesh, M("FinishGlow"), new Vector3(0f, 2.5f, 0f), new Vector3(4.2f, 2.5f, 4.2f));
            var ringRoot = EditorUtil.Create("Rings", open.transform, Layers.Trigger);
            var spin = ringRoot.AddComponent<SpinAndBob>();
            EditorUtil.Set(spin, "spinSpeed", 45f);
            EditorUtil.Set(spin, "bobHeight", 0.3f);
            VisualNoShadow("RingA", ringRoot.transform, MeshAssetGenerator.Torus, M("Finish"), new Vector3(0f, 1.5f, 0f), Vector3.one * 2.1f);
            VisualNoShadow("RingB", ringRoot.transform, MeshAssetGenerator.Torus, M("Finish"), new Vector3(0f, 3.2f, 0f), Vector3.one * 1.6f);

            var locked = EditorUtil.Create("LockedBarrier", go.transform, Layers.Trigger);
            VisualNoShadow("Barrier", locked.transform, EditorUtil.CylinderMesh, M("LockedBarrier"), new Vector3(0f, 2.5f, 0f), new Vector3(4.4f, 2.5f, 4.4f));
            locked.SetActive(false);

            var finish = go.AddComponent<FinishTrigger>();
            EditorUtil.Set(finish, "lockedBarrier", locked);
            EditorUtil.Set(finish, "openVisual", open);
            return go;
        }

        static GameObject KillZonePrefab()
        {
            var go = Root("KillZone", Layers.Trigger);
            AddMesh(go, EditorUtil.CubeMesh, M("HazardVolume"), false);
            AddBox(go, Vector3.zero, Vector3.one, true);
            var kz = go.AddComponent<KillZone>();
            EditorUtil.Set(kz, "visual", go.GetComponent<Renderer>());
            return go;
        }

        static GameObject HazardPanelPrefab()
        {
            var go = Root("HazardPanel", Layers.Environment);
            go.tag = Tags.Hazard;
            AddMesh(go, EditorUtil.CubeMesh, M("HazardPanel"));
            go.AddComponent<BoxCollider>();
            var trigger = AddBox(go, new Vector3(0f, 2f, 0f), new Vector3(1f, 3f, 1f), true);
            var hp = go.AddComponent<HazardPanel>();
            EditorUtil.Set(hp, "panelRenderer", go.GetComponent<Renderer>());
            EditorUtil.Set(hp, "damageTrigger", trigger);
            return go;
        }

        static GameObject SecretPrefab()
        {
            var go = Root("Secret", Layers.Trigger);
            AddBox(go, Vector3.zero, Vector3.one, true);
            var vis = VisualNoShadow("EditorVolume", go.transform, EditorUtil.CubeMesh, M("SecretVolume"), Vector3.zero, Vector3.one);
            var secret = go.AddComponent<SecretArea>();
            EditorUtil.Set(secret, "editorVisual", vis);
            return go;
        }

        static GameObject SignPrefab()
        {
            var go = Root("Sign", Layers.Default);
            var backing = VisualNoShadow("Backing", go.transform, EditorUtil.CubeMesh, M("Dark"), new Vector3(0f, 0f, 0.07f), new Vector3(3f, 1.2f, 0.1f));
            var textGo = EditorUtil.Create("Text", go.transform, Layers.Default);
            textGo.transform.localScale = Vector3.one * 0.1f;
            var tm = textGo.AddComponent<TextMesh>();
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tm.font = font;
            tm.fontSize = 80;
            tm.characterSize = 1f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.text = "SIGN";
            var tr = textGo.GetComponent<MeshRenderer>();
            tr.sharedMaterial = lib.textMaterial != null ? lib.textMaterial : (font != null ? font.material : null);
            tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            tr.receiveShadows = false;
            var sign = go.AddComponent<WorldSign>();
            EditorUtil.Set(sign, "textMesh", tm);
            EditorUtil.Set(sign, "backing", backing.transform);
            return go;
        }

        // ------------------------------------------------------------------ Pickups

        static GameObject PickupRoot(string name, out GameObject visual, float triggerRadius, float visualHeight)
        {
            var go = Root(name, Layers.Pickup);
            go.tag = Tags.Pickup;
            var sphere = go.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = triggerRadius;
            sphere.center = new Vector3(0f, visualHeight, 0f);
            VisualNoShadow("Base", go.transform, EditorUtil.CylinderMesh, M("Dark"), new Vector3(0f, 0.03f, 0f), new Vector3(1.1f, 0.03f, 1.1f));
            visual = EditorUtil.Create("Visual", go.transform, Layers.Pickup);
            visual.transform.localPosition = new Vector3(0f, visualHeight, 0f);
            var spin = visual.AddComponent<SpinAndBob>();
            EditorUtil.Set(spin, "spinSpeed", 90f);
            EditorUtil.Set(spin, "bobHeight", 0.12f);
            return go;
        }

        static void ConfigurePickup(PickupBase pickup, GameObject visual, SoundId sound, float respawnTime)
        {
            EditorUtil.Set(pickup, "visualRoot", visual);
            EditorUtil.Set(pickup, "collectOnTouch", true);
            EditorUtil.Set(pickup, "respawns", true);
            EditorUtil.Set(pickup, "respawnTime", respawnTime);
            EditorUtil.Set(pickup, "collectSound", (int)sound);
        }

        static GameObject WeaponPickupPrefab()
        {
            var go = PickupRoot("WeaponPickup", out var visual, 1.1f, 1f);
            VisualNoShadow("Glow", go.transform, MeshAssetGenerator.Torus, M("PickupGlow"), new Vector3(0f, 0.08f, 0f), Vector3.one * 0.5f);
            var display = EditorUtil.Create("Display", visual.transform, Layers.Pickup);
            var pickup = go.AddComponent<WeaponPickup>();
            ConfigurePickup(pickup, visual, SoundId.WeaponPickup, 15f);
            EditorUtil.Set(pickup, "displayRoot", display.transform);
            EditorUtil.Set(pickup, "weapon", reg.GetWeapon("shotgun"));
            return go;
        }

        static GameObject AmmoPickupPrefab()
        {
            var go = PickupRoot("AmmoPickup", out var visual, 0.9f, 0.6f);
            EditorUtil.Box("Box", visual.transform, M("WeaponBody"), Vector3.zero, new Vector3(0.7f, 0.42f, 0.42f));
            EditorUtil.Box("Lid", visual.transform, M("AccentYellow"), new Vector3(0f, 0.23f, 0f), new Vector3(0.72f, 0.06f, 0.44f));
            EditorUtil.Box("Band", visual.transform, M("AccentYellow"), Vector3.zero, new Vector3(0.12f, 0.43f, 0.43f));
            for (int i = 0; i < 3; i++)
            {
                EditorUtil.Visual("Shell" + i, visual.transform, EditorUtil.CylinderMesh, M("Accent"), new Vector3(-0.18f + i * 0.18f, 0.32f, 0f), new Vector3(0.08f, 0.1f, 0.08f));
            }
            var pickup = go.AddComponent<AmmoPickup>();
            ConfigurePickup(pickup, visual, SoundId.AmmoPickup, 12f);
            return go;
        }

        static GameObject HealthPickupPrefab()
        {
            var go = PickupRoot("HealthPickup", out var visual, 0.9f, 0.6f);
            EditorUtil.Box("Box", visual.transform, M("EnemyBody"), Vector3.zero, new Vector3(0.55f, 0.55f, 0.3f));
            EditorUtil.Box("CrossH", visual.transform, M("HealthGlow"), Vector3.zero, new Vector3(0.36f, 0.12f, 0.32f));
            EditorUtil.Box("CrossV", visual.transform, M("HealthGlow"), Vector3.zero, new Vector3(0.12f, 0.36f, 0.32f));
            var pickup = go.AddComponent<HealthPickup>();
            ConfigurePickup(pickup, visual, SoundId.HealthPickup, 20f);
            return go;
        }

        // ------------------------------------------------------------------ Props

        static Rigidbody AddPropBody(GameObject go, float mass)
        {
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = mass;
            rb.drag = 0.05f;
            rb.angularDrag = 0.3f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            return rb;
        }

        static GameObject CratePrefab()
        {
            var go = Root("Crate", Layers.Prop);
            AddBox(go, new Vector3(0f, 0.5f, 0f), Vector3.one, false);
            EditorUtil.Box("Body", go.transform, M("Crate"), new Vector3(0f, 0.5f, 0f), Vector3.one * 0.98f);
            // Frame edges.
            var frame = M("Dark");
            foreach (var y in new[] { 0.03f, 0.97f })
            {
                EditorUtil.Box("EdgeX" + y, go.transform, frame, new Vector3(0f, y, 0.47f), new Vector3(1f, 0.07f, 0.07f));
                EditorUtil.Box("EdgeX2" + y, go.transform, frame, new Vector3(0f, y, -0.47f), new Vector3(1f, 0.07f, 0.07f));
                EditorUtil.Box("EdgeZ" + y, go.transform, frame, new Vector3(0.47f, y, 0f), new Vector3(0.07f, 0.07f, 1f));
                EditorUtil.Box("EdgeZ2" + y, go.transform, frame, new Vector3(-0.47f, y, 0f), new Vector3(0.07f, 0.07f, 1f));
            }
            EditorUtil.Box("Diagonal", go.transform, frame, new Vector3(0f, 0.5f, 0.495f), new Vector3(1.2f, 0.07f, 0.02f), new Vector3(0f, 0f, 45f));
            AddPropBody(go, 6f);
            var b = go.AddComponent<BreakableObject>();
            EditorUtil.Set(b, "maxHealth", 25f);
            EditorUtil.Set(b, "fragmentPrefab", reg.debrisChunk);
            EditorUtil.Set(b, "fragmentCount", 8);
            EditorUtil.Set(b, "fragmentsUseOwnMaterial", true);
            EditorUtil.Set(b, "breakSound", (int)SoundId.CrateBreak);
            return go;
        }

        static GameObject BarrelPrefab(bool explosive)
        {
            var go = Root(explosive ? "ExplosiveBarrel" : "Barrel", Layers.Prop);
            var body = EditorUtil.Visual("Body", go.transform, EditorUtil.CylinderMesh, M(explosive ? "AccentRed" : "AccentBlue"), new Vector3(0f, 0.6f, 0f), new Vector3(0.8f, 0.6f, 0.8f));
            var col = body.AddComponent<MeshCollider>();
            col.sharedMesh = EditorUtil.CylinderMesh;
            col.convex = true;
            EditorUtil.Visual("RimTop", go.transform, EditorUtil.CylinderMesh, M("Dark"), new Vector3(0f, 1.1f, 0f), new Vector3(0.84f, 0.04f, 0.84f));
            EditorUtil.Visual("RimBottom", go.transform, EditorUtil.CylinderMesh, M("Dark"), new Vector3(0f, 0.1f, 0f), new Vector3(0.84f, 0.04f, 0.84f));
            if (explosive)
            {
                EditorUtil.Visual("Band", go.transform, EditorUtil.CylinderMesh, M("AccentYellow"), new Vector3(0f, 0.62f, 0f), new Vector3(0.82f, 0.09f, 0.82f));
                VisualNoShadow("Warning", go.transform, MeshAssetGenerator.Chevron, M("Dark"), new Vector3(0f, 0.62f, -0.415f), new Vector3(0.35f, 0.4f, 0.35f), new Vector3(90f, 0f, 0f));
            }
            AddPropBody(go, explosive ? 8f : 10f);
            var b = go.AddComponent<BreakableObject>();
            EditorUtil.Set(b, "maxHealth", explosive ? 20f : 60f);
            EditorUtil.Set(b, "fragmentPrefab", reg.debrisChunk);
            EditorUtil.Set(b, "fragmentCount", 8);
            EditorUtil.Set(b, "fragmentsUseOwnMaterial", true);
            EditorUtil.Set(b, "breakSound", (int)SoundId.CrateBreak);
            EditorUtil.Set(b, "explosive", explosive);
            if (explosive)
            {
                EditorUtil.Set(b, "explosionRadius", 6.5f);
                EditorUtil.Set(b, "explosionDamage", 70f);
                EditorUtil.Set(b, "explosionForce", 22f);
                EditorUtil.Set(b, "explosionDelay", 0.12f);
            }
            return go;
        }

        // ------------------------------------------------------------------ Lights & boss objects

        static GameObject LightPrefab()
        {
            var go = Root("Light", Layers.Default);
            var lightGo = EditorUtil.Create("PointLight", go.transform);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 12f;
            light.intensity = 2f;
            light.color = new Color(1f, 0.85f, 0.6f);
            light.shadows = LightShadows.None;
            var fixture = VisualNoShadow("Fixture", go.transform, EditorUtil.SphereMesh, M("LightFixture"), Vector3.zero, Vector3.one * 0.35f);
            VisualNoShadow("Cage", go.transform, MeshAssetGenerator.Torus, M("Dark"), Vector3.zero, Vector3.one * 0.22f, new Vector3(90f, 0f, 0f));
            var dl = go.AddComponent<DecorLight>();
            EditorUtil.Set(dl, "pointLight", light);
            EditorUtil.Set(dl, "fixture", fixture.GetComponent<Renderer>());
            return go;
        }

        static GameObject BossTriggerPrefab()
        {
            var go = Root("BossTrigger", Layers.Trigger);
            var sphere = go.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = 22f;
            go.AddComponent<BossArenaTrigger>();
            VisualNoShadow("ArenaMark", go.transform, MeshAssetGenerator.Torus, M("Hazard"), new Vector3(0f, 0.05f, 0f), new Vector3(3f, 2f, 3f));
            return go;
        }

        static GameObject BossSocketPrefab()
        {
            var go = Root("BossSocket", Layers.Default);
            var marker = EditorUtil.Create("Marker", go.transform);
            VisualNoShadow("Core", marker.transform, EditorUtil.SphereMesh, M("EnemyPurple"), Vector3.zero, Vector3.one * 0.6f);
            VisualNoShadow("Ring", marker.transform, MeshAssetGenerator.Torus, M("EnemyPurple"), Vector3.zero, Vector3.one * 0.6f);
            var socket = go.AddComponent<BossSocket>();
            EditorUtil.Set(socket, "editorMarker", marker);
            return go;
        }
    }
}

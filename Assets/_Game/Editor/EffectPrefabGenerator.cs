using Momentum.Audio;
using Momentum.Bosses;
using UnityEngine;

namespace Momentum.EditorTools
{
    /// <summary>
    /// Builds the pooled effect and projectile prefabs procedurally: muzzle flashes, impacts, explosions,
    /// tracers, telegraphs, shockwaves, lasers, debris, and every projectile type.
    /// </summary>
    public static class EffectPrefabGenerator
    {
        public struct ParticleSpec
        {
            public int burst;
            public float rate;
            public Vector2 lifetime;
            public Vector2 speed;
            public Vector2 size;
            public Color colorA;
            public Color colorB;
            public float gravity;
            public ParticleSystemShapeType shape;
            public float radius;
            public float angle;
            public bool additive;
            public bool stretched;
            public float duration;
            public float endSize;
            public bool loop;
            public int maxParticles;
        }

        static MaterialLibrary lib;

        public static void Generate(PrefabRegistry registry, MaterialLibrary library)
        {
            EditorUtil.Require(registry, "PrefabRegistry");
            EditorUtil.Require(library, "MaterialLibrary");
            lib = library;
            EditorUtil.EnsureFolder(GamePaths.EffectPrefabs);
            EditorUtil.EnsureFolder(GamePaths.ProjectilePrefabs);

            registry.muzzleFlash = Save(MuzzleFlash(), "MuzzleFlash");
            registry.impactEnvironment = Save(Impact("ImpactEnvironment", new Color(1f, 0.85f, 0.5f), new Color(1f, 0.6f, 0.2f), true), "ImpactEnvironment");
            registry.impactEnemy = Save(Impact("ImpactEnemy", new Color(1f, 0.2f, 0.2f), Color.white, false), "ImpactEnemy");
            registry.explosion = Save(Explosion("Explosion", 1f), "Explosion");
            registry.smallExplosion = Save(Explosion("SmallExplosion", 0.45f), "SmallExplosion");
            registry.enemyDeath = Save(Burst("EnemyDeath", 30, new Vector2(4f, 12f), new Vector2(0.4f, 0.9f), new Vector2(0.1f, 0.3f), new Color(1f, 0.25f, 0.2f), Color.white, 1.5f, 2f), "EnemyDeath");
            registry.pickupBurst = Save(Burst("PickupBurst", 24, new Vector2(2f, 6f), new Vector2(0.3f, 0.7f), new Vector2(0.08f, 0.2f), new Color(1f, 0.8f, 0.3f), new Color(0.3f, 0.9f, 1f), -0.3f, 1.2f), "PickupBurst");
            registry.checkpointBurst = Save(Burst("CheckpointBurst", 40, new Vector2(3f, 9f), new Vector2(0.5f, 1.1f), new Vector2(0.1f, 0.25f), new Color(0.3f, 1f, 0.5f), Color.white, -0.5f, 1.8f), "CheckpointBurst");
            registry.tracer = Save(Line("Tracer", lib.line, 0.03f), "Tracer");
            registry.railTrail = Save(Line("RailTrail", lib.line, 0.15f), "RailTrail");
            registry.debrisChunk = Save(Debris("DebrisChunk", lib.Get("Gray"), 4f), "DebrisChunk");
            registry.glassShard = Save(Debris("GlassShard", lib.Get("Glass"), 3f), "GlassShard");
            registry.telegraphDisc = Save(Telegraph("TelegraphDisc", EditorUtil.CylinderMesh), "TelegraphDisc");
            registry.telegraphLine = Save(Telegraph("TelegraphLine", EditorUtil.CubeMesh), "TelegraphLine");
            registry.shockwave = Save(ShockwaveRing(), "Shockwave");
            registry.laserBeam = Save(Laser(), "LaserBeam");

            registry.enemyBullet = SaveProjectile(BulletProjectile("EnemyBullet", lib.Get("Projectile"), 0.28f, new Color(1f, 0.2f, 0.25f)), "EnemyBullet");
            registry.enemyPellet = SaveProjectile(BulletProjectile("EnemyPellet", lib.Get("EnemyOrange"), 0.18f, new Color(1f, 0.55f, 0.1f)), "EnemyPellet");
            registry.energyOrb = SaveProjectile(BulletProjectile("EnergyOrb", lib.Get("Orb"), 0.45f, new Color(0.85f, 0.2f, 1f)), "EnergyOrb");
            registry.rocket = SaveProjectile(Rocket("Rocket", lib.Get("WeaponBody"), lib.Get("Accent"), false), "Rocket");
            registry.missile = SaveProjectile(Rocket("Missile", lib.Get("BossDark"), lib.Get("EnemyRed"), true), "Missile");
            registry.grenade = SaveProjectile(GrenadeProjectile(), "Grenade");
            registry.rock = SaveProjectile(RockProjectile(), "Rock");
        }

        static GameObject Save(GameObject go, string name)
        {
            return EditorUtil.SavePrefab(go, GamePaths.EffectPrefabs + "/" + name + ".prefab");
        }

        static GameObject SaveProjectile(GameObject go, string name)
        {
            return EditorUtil.SavePrefab(go, GamePaths.ProjectilePrefabs + "/" + name + ".prefab");
        }

        // ------------------------------------------------------------------ Particles

        public static ParticleSystem AddParticles(GameObject go, ParticleSpec spec)
        {
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = spec.duration > 0f ? spec.duration : 0.5f;
            main.loop = spec.loop;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(spec.lifetime.x, spec.lifetime.y);
            main.startSpeed = new ParticleSystem.MinMaxCurve(spec.speed.x, spec.speed.y);
            main.startSize = new ParticleSystem.MinMaxCurve(spec.size.x, spec.size.y);
            main.startColor = new ParticleSystem.MinMaxGradient(spec.colorA, spec.colorB);
            main.gravityModifier = spec.gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = spec.maxParticles > 0 ? spec.maxParticles : 200;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);

            var emission = ps.emission;
            emission.rateOverTime = spec.rate;
            if (spec.burst > 0) emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)spec.burst) });

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = spec.shape;
            shape.radius = Mathf.Max(0.01f, spec.radius);
            shape.angle = spec.angle;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.4f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(grad);

            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, spec.endSize));

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = lib.Get(spec.additive ? "ParticleAdditive" : "ParticleAlpha");
            renderer.renderMode = spec.stretched ? ParticleSystemRenderMode.Stretch : ParticleSystemRenderMode.Billboard;
            if (spec.stretched)
            {
                renderer.velocityScale = 0.04f;
                renderer.lengthScale = 2f;
            }
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return ps;
        }

        static GameObject Root(string name, float lifetime)
        {
            var go = new GameObject(name);
            var auto = go.AddComponent<AutoDespawn>();
            EditorUtil.Set(auto, "lifetime", lifetime);
            return go;
        }

        static Light AddLight(GameObject go, Color color, float range, float peak, float duration)
        {
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = range;
            light.intensity = 0f;
            light.shadows = LightShadows.None;
            var flash = go.AddComponent<LightFlash>();
            EditorUtil.Set(flash, "peakIntensity", peak);
            EditorUtil.Set(flash, "duration", duration);
            return light;
        }

        static GameObject MuzzleFlash()
        {
            var go = Root("MuzzleFlash", 0.3f);
            AddParticles(go, new ParticleSpec
            {
                burst = 8, lifetime = new Vector2(0.05f, 0.09f), speed = new Vector2(1f, 5f), size = new Vector2(0.25f, 0.5f),
                colorA = new Color(1f, 0.85f, 0.4f), colorB = new Color(1f, 0.5f, 0.1f), shape = ParticleSystemShapeType.Cone, angle = 18f, radius = 0.05f,
                additive = true, duration = 0.1f, endSize = 0.3f
            });
            var lightGo = new GameObject("Light");
            lightGo.transform.SetParent(go.transform, false);
            AddLight(lightGo, new Color(1f, 0.7f, 0.3f), 6f, 3f, 0.06f);
            return go;
        }

        static GameObject Impact(string name, Color a, Color b, bool dust)
        {
            var go = Root(name, 1f);
            AddParticles(go, new ParticleSpec
            {
                burst = 12, lifetime = new Vector2(0.15f, 0.35f), speed = new Vector2(3f, 9f), size = new Vector2(0.04f, 0.09f),
                colorA = a, colorB = b, gravity = 1.2f, shape = ParticleSystemShapeType.Cone, angle = 35f, radius = 0.02f,
                additive = true, stretched = true, duration = 0.2f, endSize = 0.5f
            });
            if (dust)
            {
                var d = new GameObject("Dust");
                d.transform.SetParent(go.transform, false);
                AddParticles(d, new ParticleSpec
                {
                    burst = 4, lifetime = new Vector2(0.4f, 0.8f), speed = new Vector2(0.4f, 1.5f), size = new Vector2(0.25f, 0.5f),
                    colorA = new Color(0.75f, 0.75f, 0.75f, 0.6f), colorB = new Color(0.55f, 0.55f, 0.55f, 0.4f), shape = ParticleSystemShapeType.Cone, angle = 25f, radius = 0.05f,
                    additive = false, duration = 0.2f, endSize = 2f
                });
            }
            return go;
        }

        static GameObject Explosion(string name, float scale)
        {
            var go = Root(name, 3f);
            AddParticles(go, new ParticleSpec
            {
                burst = 1, lifetime = new Vector2(0.12f, 0.15f), speed = Vector2.zero, size = new Vector2(7f, 7f) * scale,
                colorA = new Color(1f, 0.9f, 0.6f), colorB = new Color(1f, 0.8f, 0.5f), shape = ParticleSystemShapeType.Sphere, radius = 0.01f,
                additive = true, duration = 0.2f, endSize = 1.4f, maxParticles = 4
            });
            var fire = new GameObject("Fireball");
            fire.transform.SetParent(go.transform, false);
            AddParticles(fire, new ParticleSpec
            {
                burst = 26, lifetime = new Vector2(0.35f, 0.8f), speed = new Vector2(4f, 11f) * scale, size = new Vector2(1.2f, 3f) * scale,
                colorA = new Color(1f, 0.6f, 0.15f), colorB = new Color(1f, 0.3f, 0.05f), gravity = -0.3f, shape = ParticleSystemShapeType.Sphere, radius = 0.6f * scale,
                additive = true, duration = 0.3f, endSize = 0.2f
            });
            var smoke = new GameObject("Smoke");
            smoke.transform.SetParent(go.transform, false);
            AddParticles(smoke, new ParticleSpec
            {
                burst = 14, lifetime = new Vector2(1.2f, 2.4f), speed = new Vector2(1f, 4f) * scale, size = new Vector2(2f, 4f) * scale,
                colorA = new Color(0.3f, 0.3f, 0.32f, 0.7f), colorB = new Color(0.15f, 0.15f, 0.16f, 0.5f), gravity = -0.15f, shape = ParticleSystemShapeType.Sphere, radius = 0.8f * scale,
                additive = false, duration = 0.3f, endSize = 2f
            });
            var sparks = new GameObject("Sparks");
            sparks.transform.SetParent(go.transform, false);
            AddParticles(sparks, new ParticleSpec
            {
                burst = 30, lifetime = new Vector2(0.4f, 1f), speed = new Vector2(8f, 22f) * scale, size = new Vector2(0.05f, 0.12f),
                colorA = new Color(1f, 0.8f, 0.4f), colorB = new Color(1f, 0.5f, 0.1f), gravity = 1.5f, shape = ParticleSystemShapeType.Sphere, radius = 0.3f,
                additive = true, stretched = true, duration = 0.3f, endSize = 0.4f
            });
            var light = new GameObject("Light");
            light.transform.SetParent(go.transform, false);
            AddLight(light, new Color(1f, 0.6f, 0.25f), 18f * scale, 8f, 0.3f);
            return go;
        }

        static GameObject Burst(string name, int count, Vector2 speed, Vector2 life, Vector2 size, Color a, Color b, float gravity, float lifetime)
        {
            var go = Root(name, lifetime);
            AddParticles(go, new ParticleSpec
            {
                burst = count, lifetime = life, speed = speed, size = size, colorA = a, colorB = b, gravity = gravity,
                shape = ParticleSystemShapeType.Sphere, radius = 0.5f, additive = true, duration = 0.3f, endSize = 0.1f
            });
            return go;
        }

        static GameObject Line(string name, Material material, float width)
        {
            var go = new GameObject(name);
            var line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.widthMultiplier = width;
            line.numCapVertices = 2;
            line.textureMode = LineTextureMode.Stretch;
            line.alignment = LineAlignment.View;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.enabled = false;
            go.AddComponent<Tracer>();
            return go;
        }

        static GameObject Debris(string name, Material material, float lifetime)
        {
            var go = Root(name, lifetime);
            go.layer = Layers.Debris;
            go.AddComponent<MeshFilter>().sharedMesh = EditorUtil.CubeMesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = material;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            go.AddComponent<BoxCollider>();
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 0.3f;
            rb.drag = 0.2f;
            rb.angularDrag = 0.5f;
            rb.interpolation = RigidbodyInterpolation.None;
            return go;
        }

        static GameObject Telegraph(string name, Mesh mesh)
        {
            var go = new GameObject(name);
            var vis = EditorUtil.Visual("Shape", go.transform, mesh, lib.telegraph, Vector3.zero, Vector3.one);
            EditorUtil.DisableShadows(vis);
            var t = go.AddComponent<TelegraphIndicator>();
            EditorUtil.Set(t, "shapeRenderer", vis.GetComponent<MeshRenderer>());
            return go;
        }

        static GameObject ShockwaveRing()
        {
            var go = new GameObject("Shockwave");
            var ring = EditorUtil.Visual("Ring", go.transform, MeshAssetGenerator.Band, lib.telegraph, Vector3.zero, Vector3.one);
            EditorUtil.DisableShadows(ring);
            var s = go.AddComponent<Shockwave>();
            EditorUtil.Set(s, "ringVisual", ring.transform);
            EditorUtil.Set(s, "ringRenderer", ring.GetComponent<MeshRenderer>());
            return go;
        }

        static GameObject Laser()
        {
            var go = new GameObject("LaserBeam");
            var line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = lib.laser;
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.widthMultiplier = 0.3f;
            line.numCapVertices = 4;
            line.textureMode = LineTextureMode.Stretch;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            go.AddComponent<LaserBeam>();
            var glow = new GameObject("Glow");
            glow.transform.SetParent(go.transform, false);
            var light = glow.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.2f, 0.2f);
            light.range = 6f;
            light.intensity = 2f;
            light.shadows = LightShadows.None;
            return go;
        }

        // ------------------------------------------------------------------ Projectiles

        static void AddTrail(GameObject go, Color color, float time, float width)
        {
            var trail = go.AddComponent<TrailRenderer>();
            trail.sharedMaterial = lib.line;
            trail.time = time;
            trail.startWidth = width;
            trail.endWidth = 0f;
            trail.startColor = color;
            trail.endColor = new Color(color.r, color.g, color.b, 0f);
            trail.minVertexDistance = 0.1f;
            trail.autodestruct = false;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows = false;
        }

        static GameObject BulletProjectile(string name, Material material, float size, Color trailColor)
        {
            var go = new GameObject(name);
            go.layer = Layers.Projectile;
            var vis = EditorUtil.Sphere("Visual", go.transform, material, Vector3.zero, Vector3.one * size);
            EditorUtil.DisableShadows(vis);
            AddTrail(go, trailColor, 0.12f, size * 0.8f);
            var p = go.AddComponent<Projectile>();
            EditorUtil.Set(p, "radius", size * 0.45f);
            EditorUtil.Set(p, "impactSound", (int)SoundId.BulletImpact);
            return go;
        }

        static GameObject Rocket(string name, Material body, Material accent, bool shootable)
        {
            var go = new GameObject(name);
            go.layer = Layers.Projectile;
            EditorUtil.Cylinder("Body", go.transform, body, Vector3.zero, new Vector3(0.18f, 0.35f, 0.18f), new Vector3(90f, 0f, 0f));
            EditorUtil.Visual("Nose", go.transform, MeshAssetGenerator.Cone, accent, new Vector3(0f, 0f, 0.35f), new Vector3(0.18f, 0.22f, 0.18f), new Vector3(90f, 0f, 0f));
            for (int i = 0; i < 4; i++)
            {
                var fin = EditorUtil.Box("Fin" + i, go.transform, accent, new Vector3(0f, 0f, -0.28f), new Vector3(0.02f, 0.18f, 0.16f), new Vector3(0f, 0f, i * 90f));
                fin.transform.localPosition += fin.transform.up * 0.1f;
            }
            EditorUtil.DisableShadows(go);
            AddTrail(go, shootable ? new Color(1f, 0.3f, 0.2f) : new Color(1f, 0.6f, 0.3f), 0.35f, 0.25f);
            var exhaust = new GameObject("Exhaust");
            exhaust.transform.SetParent(go.transform, false);
            exhaust.transform.localPosition = new Vector3(0f, 0f, -0.4f);
            exhaust.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            AddParticles(exhaust, new ParticleSpec
            {
                rate = 60f, loop = true, lifetime = new Vector2(0.2f, 0.4f), speed = new Vector2(1f, 3f), size = new Vector2(0.2f, 0.45f),
                colorA = new Color(1f, 0.6f, 0.2f), colorB = new Color(0.5f, 0.5f, 0.5f, 0.6f), shape = ParticleSystemShapeType.Cone, angle = 10f, radius = 0.05f,
                additive = true, duration = 1f, endSize = 2f
            });
            var lightGo = new GameObject("Light");
            lightGo.transform.SetParent(go.transform, false);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.55f, 0.2f);
            light.range = 5f;
            light.intensity = 2f;
            light.shadows = LightShadows.None;
            var p = go.AddComponent<Projectile>();
            EditorUtil.Set(p, "radius", 0.2f);
            EditorUtil.Set(p, "impactSound", (int)SoundId.None);
            if (shootable)
            {
                var col = go.AddComponent<SphereCollider>();
                col.radius = 0.45f;
                EditorUtil.Set(p, "shootable", true);
                EditorUtil.Set(p, "shootableHealth", 1f);
            }
            return go;
        }

        static GameObject GrenadeProjectile()
        {
            var go = new GameObject("Grenade");
            go.layer = Layers.Projectile;
            EditorUtil.Sphere("Body", go.transform, lib.Get("WeaponBody"), Vector3.zero, Vector3.one * 0.28f);
            EditorUtil.Cylinder("Band", go.transform, lib.Get("AccentGreen"), Vector3.zero, new Vector3(0.3f, 0.03f, 0.3f));
            EditorUtil.DisableShadows(go);
            var col = go.AddComponent<SphereCollider>();
            col.radius = 0.14f;
            var mat = new PhysicMaterial("GrenadeBounce")
            {
                bounciness = 0.45f,
                dynamicFriction = 0.4f,
                staticFriction = 0.4f,
                bounceCombine = PhysicMaterialCombine.Maximum
            };
            col.sharedMaterial = EditorUtil.SaveAsset(mat, GamePaths.Materials + "/Physics_GrenadeBounce.physicMaterial");
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 0.5f;
            rb.drag = 0.05f;
            rb.angularDrag = 0.3f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            AddTrail(go, new Color(0.4f, 1f, 0.5f), 0.25f, 0.15f);
            go.AddComponent<Grenade>();
            return go;
        }

        static GameObject RockProjectile()
        {
            var go = new GameObject("Rock");
            go.layer = Layers.Projectile;
            EditorUtil.Sphere("Rock", go.transform, lib.Get("Rock"), Vector3.zero, new Vector3(1.1f, 0.9f, 1f));
            EditorUtil.Box("Chunk", go.transform, lib.Get("Rock"), new Vector3(0.2f, 0.25f, 0f), Vector3.one * 0.6f, new Vector3(20f, 35f, 10f));
            AddTrail(go, new Color(0.6f, 0.55f, 0.5f, 0.6f), 0.3f, 0.6f);
            var p = go.AddComponent<Projectile>();
            EditorUtil.Set(p, "radius", 0.5f);
            EditorUtil.Set(p, "impactSound", (int)SoundId.None);
            return go;
        }
    }
}

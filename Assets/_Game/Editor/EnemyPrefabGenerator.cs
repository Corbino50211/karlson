using System;
using Momentum.Enemies;
using UnityEngine;

namespace Momentum.EditorTools
{
    /// <summary>
    /// Creates the six EnemyDefinition assets (stats) and procedural enemy prefabs. Every enemy shares the
    /// same rig: root (Rigidbody, body collider, EnemyHealth, HitFlash, behaviour) with a "Visual" root that
    /// breaks apart on death, an aim pivot carrying the weapon and muzzle, an eye, a critical head hitbox and
    /// an alert indicator. Stats are only written when a definition is first created, so tuning survives reruns.
    /// </summary>
    public static class EnemyPrefabGenerator
    {
        static MaterialLibrary lib;
        static PrefabRegistry reg;

        public static void Generate(PrefabRegistry registry, MaterialLibrary library)
        {
            lib = library;
            reg = registry;
            EditorUtil.EnsureFolder(GamePaths.EnemyPrefabs);
            EditorUtil.EnsureFolder(GamePaths.EnemyDefinitions);

            Save(EnemyType.Gunner, BuildGunner(Definition(EnemyType.Gunner, "Gunner", new Color(1f, 0.12f, 0.15f), d =>
            {
                d.maxHealth = 60f; d.knockbackMultiplier = 1f; d.healthDropChance = 0.15f; d.ammoDropChance = 0.3f;
                d.moveSpeed = 3.5f; d.chaseSpeed = 6f; d.acceleration = 25f; d.turnSpeed = 360f; d.patrolRadius = 6f;
                d.sightRange = 40f; d.fieldOfView = 130f; d.hearingRange = 7f; d.reactionTime = 0.45f; d.searchDuration = 6f;
                d.attackRange = 28f; d.preferredRange = 14f; d.attackCooldown = 1.8f; d.damage = 7f; d.projectileSpeed = 30f;
                d.projectileCount = 3; d.burstInterval = 0.14f; d.inaccuracy = 2.5f; d.leadFactor = 0.4f; d.hitKnockback = 2f; d.windupTime = 0.4f;
            })));

            Save(EnemyType.Shotgunner, BuildShotgunner(Definition(EnemyType.Shotgunner, "Brute", new Color(1f, 0.5f, 0.1f), d =>
            {
                d.maxHealth = 95f; d.knockbackMultiplier = 0.8f; d.healthDropChance = 0.25f; d.ammoDropChance = 0.3f;
                d.moveSpeed = 4f; d.chaseSpeed = 7.5f; d.acceleration = 30f; d.turnSpeed = 420f; d.patrolRadius = 4f;
                d.sightRange = 35f; d.fieldOfView = 140f; d.hearingRange = 9f; d.reactionTime = 0.35f; d.searchDuration = 8f;
                d.attackRange = 11f; d.preferredRange = 5f; d.attackCooldown = 1.6f; d.damage = 6f; d.projectileSpeed = 34f;
                d.projectileCount = 8; d.burstInterval = 0f; d.inaccuracy = 0f; d.leadFactor = 0.2f; d.hitKnockback = 6f; d.windupTime = 0.5f;
            })));

            Save(EnemyType.Sniper, BuildSniper(Definition(EnemyType.Sniper, "Marksman", new Color(0.7f, 0.25f, 1f), d =>
            {
                d.maxHealth = 45f; d.knockbackMultiplier = 1.2f; d.healthDropChance = 0.2f; d.ammoDropChance = 0.35f;
                d.moveSpeed = 3f; d.chaseSpeed = 4.5f; d.acceleration = 20f; d.turnSpeed = 300f; d.patrolRadius = 0f;
                d.sightRange = 70f; d.fieldOfView = 100f; d.hearingRange = 6f; d.reactionTime = 0.6f; d.searchDuration = 5f;
                d.attackRange = 65f; d.preferredRange = 35f; d.attackCooldown = 3.2f; d.damage = 35f; d.projectileSpeed = 0f;
                d.projectileCount = 1; d.burstInterval = 0f; d.inaccuracy = 0.3f; d.leadFactor = 0f; d.hitKnockback = 5f; d.windupTime = 1.6f;
            })));

            Save(EnemyType.Charger, BuildCharger(Definition(EnemyType.Charger, "Charger", new Color(1f, 0.3f, 0.1f), d =>
            {
                d.maxHealth = 140f; d.knockbackMultiplier = 0.35f; d.healthDropChance = 0.35f; d.ammoDropChance = 0.2f;
                d.moveSpeed = 4f; d.chaseSpeed = 8f; d.acceleration = 40f; d.turnSpeed = 300f; d.patrolRadius = 5f;
                d.sightRange = 35f; d.fieldOfView = 150f; d.hearingRange = 10f; d.reactionTime = 0.3f; d.searchDuration = 6f;
                d.attackRange = 16f; d.preferredRange = 0f; d.attackCooldown = 2.4f; d.damage = 30f; d.projectileSpeed = 0f;
                d.projectileCount = 1; d.burstInterval = 0f; d.inaccuracy = 0f; d.leadFactor = 0.6f; d.hitKnockback = 18f; d.windupTime = 0.6f;
            })));

            Save(EnemyType.Drone, BuildDrone(Definition(EnemyType.Drone, "Drone", new Color(0.2f, 0.85f, 1f), d =>
            {
                d.maxHealth = 30f; d.knockbackMultiplier = 1.5f; d.healthDropChance = 0.1f; d.ammoDropChance = 0.2f;
                d.moveSpeed = 6f; d.chaseSpeed = 9f; d.acceleration = 18f; d.turnSpeed = 540f; d.patrolRadius = 6f;
                d.sightRange = 45f; d.fieldOfView = 360f; d.hearingRange = 12f; d.reactionTime = 0.4f; d.searchDuration = 6f;
                d.attackRange = 26f; d.preferredRange = 11f; d.attackCooldown = 1.4f; d.damage = 6f; d.projectileSpeed = 26f;
                d.projectileCount = 2; d.burstInterval = 0.2f; d.inaccuracy = 3f; d.leadFactor = 0.3f; d.hitKnockback = 1.5f; d.windupTime = 0.3f;
            })));

            Save(EnemyType.Turret, BuildTurret(Definition(EnemyType.Turret, "Turret", new Color(1f, 0.85f, 0.15f), d =>
            {
                d.maxHealth = 120f; d.knockbackMultiplier = 0f; d.healthDropChance = 0f; d.ammoDropChance = 0.4f;
                d.moveSpeed = 0f; d.chaseSpeed = 0f; d.acceleration = 0f; d.turnSpeed = 120f; d.patrolRadius = 0f;
                d.sightRange = 45f; d.fieldOfView = 360f; d.hearingRange = 0f; d.reactionTime = 0.6f; d.searchDuration = 3f;
                d.attackRange = 40f; d.preferredRange = 0f; d.attackCooldown = 2f; d.damage = 6f; d.projectileSpeed = 32f;
                d.projectileCount = 5; d.burstInterval = 0.1f; d.inaccuracy = 2f; d.leadFactor = 0.5f; d.hitKnockback = 2f; d.windupTime = 0.5f;
            })));
        }

        static EnemyDefinition Definition(EnemyType type, string displayName, Color accent, Action<EnemyDefinition> configure)
        {
            var def = EditorUtil.LoadOrCreate<EnemyDefinition>(GamePaths.EnemyDefinitions + "/Enemy_" + type + ".asset", out bool created);
            def.type = type;
            if (created)
            {
                def.displayName = displayName;
                def.accentColor = accent;
                def.bodyColor = new Color(0.92f, 0.92f, 0.94f);
                configure(def);
            }
            UnityEditor.EditorUtility.SetDirty(def);
            return def;
        }

        static void Save(EnemyType type, GameObject root)
        {
            var prefab = EditorUtil.SavePrefab(root, GamePaths.EnemyPrefabs + "/Enemy_" + type + ".prefab");
            reg.SetEnemyPrefab(type, prefab);
        }

        // ------------------------------------------------------------------ Shared rig

        class Rig
        {
            public GameObject root;
            public Transform visual;
            public Transform aimPivot;
            public Transform muzzle;
            public Transform eye;
            public EnemyHealth health;
        }

        static Material M(string key) => lib.Get(key);

        static Rig CreateRig(string name, float radius, float bodyHeight, float headY, float headRadius, float aimY, float mass)
        {
            var rig = new Rig();
            var root = EditorUtil.Create(name, null, Layers.Enemy);
            root.tag = Tags.Enemy;
            rig.root = root;

            var rb = root.AddComponent<Rigidbody>();
            rb.mass = mass;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            var body = root.AddComponent<CapsuleCollider>();
            body.radius = radius;
            body.height = Mathf.Max(bodyHeight, radius * 2f);
            body.center = new Vector3(0f, body.height * 0.5f, 0f);

            rig.health = root.AddComponent<EnemyHealth>();
            root.AddComponent<HitFlash>();

            rig.visual = EditorUtil.Create("Visual", root.transform, Layers.Enemy).transform;

            var aim = EditorUtil.Create("AimPivot", rig.visual, Layers.Enemy);
            aim.transform.localPosition = new Vector3(0f, aimY, 0f);
            rig.aimPivot = aim.transform;

            var eye = EditorUtil.Create("Eye", root.transform, Layers.Enemy);
            eye.transform.localPosition = new Vector3(0f, headY, 0.15f);
            rig.eye = eye.transform;

            if (headRadius > 0f)
            {
                var head = EditorUtil.Create("HeadHitbox", root.transform, Layers.Enemy);
                head.transform.localPosition = new Vector3(0f, headY, 0f);
                var sphere = head.AddComponent<SphereCollider>();
                sphere.radius = headRadius;
                var hitbox = head.AddComponent<Hitbox>();
                EditorUtil.Set(hitbox, "owner", rig.health);
                EditorUtil.Set(hitbox, "damageMultiplier", 2f);
                EditorUtil.Set(hitbox, "critical", true);
            }
            return rig;
        }

        static GameObject AlertIndicator(Transform parent, float height)
        {
            var alert = EditorUtil.Create("AlertIndicator", parent, Layers.Enemy);
            alert.transform.localPosition = new Vector3(0f, height, 0f);
            alert.AddComponent<Billboard>();
            var mat = M("EnemyYellow");
            var bar = EditorUtil.Box("Bar", alert.transform, mat, new Vector3(0f, 0.12f, 0f), new Vector3(0.12f, 0.38f, 0.04f));
            var dot = EditorUtil.Box("Dot", alert.transform, mat, new Vector3(0f, -0.2f, 0f), new Vector3(0.12f, 0.12f, 0.04f));
            EditorUtil.DisableShadows(bar);
            EditorUtil.DisableShadows(dot);
            alert.SetActive(false);
            return alert;
        }

        static Transform Muzzle(Transform parent, Vector3 localPos)
        {
            var m = EditorUtil.Create("Muzzle", parent, Layers.Enemy);
            m.transform.localPosition = localPos;
            return m.transform;
        }

        static void Wire(EnemyBase enemy, Rig rig, EnemyDefinition def, GameObject projectile, GameObject alert)
        {
            EditorUtil.Set(enemy, "definition", def);
            EditorUtil.Set(enemy, "eye", rig.eye);
            EditorUtil.Set(enemy, "aimPivot", rig.aimPivot);
            EditorUtil.Set(enemy, "muzzle", rig.muzzle);
            EditorUtil.Set(enemy, "visualRoot", rig.visual);
            EditorUtil.Set(enemy, "alertIndicator", alert);
            EditorUtil.Set(enemy, "projectilePrefab", projectile);
            EditorUtil.Set(rig.health, "maxHealth", def.maxHealth);
        }

        /// <summary>Simple humanoid: torso capsule, shoulders, visor head and legs.</summary>
        static void Humanoid(Rig rig, float width, float height, string accent)
        {
            var body = M("EnemyBody");
            var dark = M("EnemyDark");
            var glow = M(accent);
            var v = rig.visual;
            float torsoY = height * 0.58f;
            EditorUtil.Visual("Torso", v, EditorUtil.CapsuleMesh, body, new Vector3(0f, torsoY, 0f), new Vector3(width, height * 0.32f, width * 0.8f));
            EditorUtil.Box("Belt", v, dark, new Vector3(0f, height * 0.42f, 0f), new Vector3(width * 0.95f, 0.1f, width * 0.75f));
            EditorUtil.Box("Chest", v, glow, new Vector3(0f, torsoY + 0.15f, width * 0.38f), new Vector3(width * 0.45f, 0.12f, 0.05f));
            EditorUtil.Box("LegL", v, dark, new Vector3(-width * 0.22f, height * 0.2f, 0f), new Vector3(width * 0.28f, height * 0.42f, width * 0.3f));
            EditorUtil.Box("LegR", v, dark, new Vector3(width * 0.22f, height * 0.2f, 0f), new Vector3(width * 0.28f, height * 0.42f, width * 0.3f));
            EditorUtil.Box("ShoulderL", v, body, new Vector3(-width * 0.55f, torsoY + 0.25f, 0f), new Vector3(width * 0.3f, 0.22f, width * 0.45f));
            EditorUtil.Box("ShoulderR", v, body, new Vector3(width * 0.55f, torsoY + 0.25f, 0f), new Vector3(width * 0.3f, 0.22f, width * 0.45f));
            float headY = rig.eye.localPosition.y;
            EditorUtil.Box("Head", v, body, new Vector3(0f, headY, 0f), new Vector3(0.42f, 0.42f, 0.42f));
            EditorUtil.Box("Visor", v, glow, new Vector3(0f, headY + 0.02f, 0.2f), new Vector3(0.34f, 0.1f, 0.06f));
        }

        // ------------------------------------------------------------------ Enemies

        static GameObject BuildGunner(EnemyDefinition def)
        {
            var rig = CreateRig("Enemy_Gunner", 0.45f, 1.5f, 1.65f, 0.28f, 1.3f, 80f);
            Humanoid(rig, 0.85f, 1.9f, "EnemyRed");
            EditorUtil.Box("Gun", rig.aimPivot, M("WeaponBody"), new Vector3(0.38f, 0f, 0.35f), new Vector3(0.12f, 0.16f, 0.7f));
            EditorUtil.Box("GunStripe", rig.aimPivot, M("EnemyRed"), new Vector3(0.38f, 0.09f, 0.3f), new Vector3(0.13f, 0.03f, 0.4f));
            rig.muzzle = Muzzle(rig.aimPivot, new Vector3(0.38f, 0f, 0.75f));
            var alert = AlertIndicator(rig.root.transform, 2.45f);
            var enemy = rig.root.AddComponent<GunnerEnemy>();
            Wire(enemy, rig, def, reg.enemyBullet, alert);
            return rig.root;
        }

        static GameObject BuildShotgunner(EnemyDefinition def)
        {
            var rig = CreateRig("Enemy_Shotgunner", 0.55f, 1.55f, 1.72f, 0.3f, 1.3f, 110f);
            Humanoid(rig, 1.1f, 1.95f, "EnemyOrange");
            var gunMat = M("WeaponBody");
            EditorUtil.Box("Gun", rig.aimPivot, gunMat, new Vector3(0.45f, 0f, 0.3f), new Vector3(0.2f, 0.2f, 0.6f));
            EditorUtil.Visual("BarrelL", rig.aimPivot, EditorUtil.CylinderMesh, M("WeaponMetal"), new Vector3(0.4f, 0.03f, 0.75f), new Vector3(0.08f, 0.22f, 0.08f), new Vector3(90f, 0f, 0f));
            EditorUtil.Visual("BarrelR", rig.aimPivot, EditorUtil.CylinderMesh, M("WeaponMetal"), new Vector3(0.5f, 0.03f, 0.75f), new Vector3(0.08f, 0.22f, 0.08f), new Vector3(90f, 0f, 0f));
            EditorUtil.Box("Armor", rig.visual, M("EnemyOrange"), new Vector3(0f, 1.25f, 0.4f), new Vector3(0.7f, 0.5f, 0.08f));
            rig.muzzle = Muzzle(rig.aimPivot, new Vector3(0.45f, 0.03f, 0.98f));
            var alert = AlertIndicator(rig.root.transform, 2.55f);
            var enemy = rig.root.AddComponent<ShotgunnerEnemy>();
            Wire(enemy, rig, def, reg.enemyPellet, alert);
            EditorUtil.Set(enemy, "pelletSpread", 9f);
            return rig.root;
        }

        static GameObject BuildSniper(EnemyDefinition def)
        {
            var rig = CreateRig("Enemy_Sniper", 0.4f, 1.6f, 1.78f, 0.26f, 1.45f, 70f);
            Humanoid(rig, 0.72f, 2.05f, "EnemyPurple");
            EditorUtil.Box("Rifle", rig.aimPivot, M("WeaponBody"), new Vector3(0.32f, 0f, 0.5f), new Vector3(0.1f, 0.14f, 1.2f));
            EditorUtil.Visual("Scope", rig.aimPivot, EditorUtil.CylinderMesh, M("EnemyPurple"), new Vector3(0.32f, 0.12f, 0.45f), new Vector3(0.07f, 0.16f, 0.07f), new Vector3(90f, 0f, 0f));
            EditorUtil.Box("Cape", rig.visual, M("EnemyDark"), new Vector3(0f, 1.2f, -0.32f), new Vector3(0.6f, 0.9f, 0.06f));
            rig.muzzle = Muzzle(rig.aimPivot, new Vector3(0.32f, 0f, 1.12f));

            var laserGo = EditorUtil.Create("Laser", rig.root.transform, Layers.Enemy);
            var laser = laserGo.AddComponent<LineRenderer>();
            laser.sharedMaterial = lib.laser;
            laser.useWorldSpace = true;
            laser.positionCount = 2;
            laser.SetPosition(0, Vector3.zero);
            laser.SetPosition(1, Vector3.forward);
            laser.widthMultiplier = 0.03f;
            laser.startColor = new Color(1f, 0.85f, 0.1f);
            laser.endColor = new Color(1f, 0.85f, 0.1f);
            laser.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            laser.receiveShadows = false;
            laser.enabled = false;

            var alert = AlertIndicator(rig.root.transform, 2.6f);
            var enemy = rig.root.AddComponent<SniperEnemy>();
            Wire(enemy, rig, def, null, alert);
            EditorUtil.Set(enemy, "laser", laser);
            EditorUtil.Set(enemy, "aimTime", 1.6f);
            EditorUtil.Set(enemy, "lockTime", 0.3f);
            return rig.root;
        }

        static GameObject BuildCharger(EnemyDefinition def)
        {
            var rig = CreateRig("Enemy_Charger", 0.75f, 1.7f, 1.45f, 0.4f, 1.2f, 200f);
            // The charger's head sits at the front of its body; move the critical hitbox there.
            rig.root.transform.Find("HeadHitbox").localPosition = new Vector3(0f, 1.45f, 0.7f);
            var body = M("EnemyBody");
            var dark = M("EnemyDark");
            var glow = M("EnemyOrange");
            var v = rig.visual;
            EditorUtil.Box("Torso", v, body, new Vector3(0f, 1.05f, 0f), new Vector3(1.5f, 1.0f, 1.2f));
            EditorUtil.Box("Plate", v, glow, new Vector3(0f, 1.1f, 0.61f), new Vector3(1.1f, 0.5f, 0.06f));
            EditorUtil.Box("Hump", v, body, new Vector3(0f, 1.6f, -0.25f), new Vector3(1.1f, 0.4f, 0.8f));
            EditorUtil.Box("Head", v, dark, new Vector3(0f, 1.45f, 0.55f), new Vector3(0.6f, 0.45f, 0.5f));
            EditorUtil.Box("Eyes", v, M("EnemyRed"), new Vector3(0f, 1.5f, 0.81f), new Vector3(0.42f, 0.08f, 0.04f));
            EditorUtil.Visual("HornL", v, MeshAssetGenerator.Cone, dark, new Vector3(-0.42f, 1.65f, 0.65f), new Vector3(0.2f, 0.55f, 0.2f), new Vector3(60f, 0f, 20f));
            EditorUtil.Visual("HornR", v, MeshAssetGenerator.Cone, dark, new Vector3(0.42f, 1.65f, 0.65f), new Vector3(0.2f, 0.55f, 0.2f), new Vector3(60f, 0f, -20f));
            EditorUtil.Box("ArmL", v, dark, new Vector3(-0.9f, 0.75f, 0.2f), new Vector3(0.35f, 1.1f, 0.4f));
            EditorUtil.Box("ArmR", v, dark, new Vector3(0.9f, 0.75f, 0.2f), new Vector3(0.35f, 1.1f, 0.4f));
            EditorUtil.Box("LegL", v, dark, new Vector3(-0.4f, 0.3f, 0f), new Vector3(0.4f, 0.6f, 0.5f));
            EditorUtil.Box("LegR", v, dark, new Vector3(0.4f, 0.3f, 0f), new Vector3(0.4f, 0.6f, 0.5f));
            rig.muzzle = Muzzle(rig.aimPivot, new Vector3(0f, 0f, 0.9f));
            var alert = AlertIndicator(rig.root.transform, 2.4f);
            var enemy = rig.root.AddComponent<ChargerEnemy>();
            Wire(enemy, rig, def, null, alert);
            return rig.root;
        }

        static GameObject BuildDrone(EnemyDefinition def)
        {
            var root = EditorUtil.Create("Enemy_Drone", null, Layers.Enemy);
            root.tag = Tags.Enemy;
            var rb = root.AddComponent<Rigidbody>();
            rb.mass = 20f;
            rb.useGravity = false;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            var sphere = root.AddComponent<SphereCollider>();
            sphere.radius = 0.55f;
            sphere.center = new Vector3(0f, 0.6f, 0f);
            var health = root.AddComponent<EnemyHealth>();
            root.AddComponent<HitFlash>();

            var rig = new Rig { root = root, health = health };
            rig.visual = EditorUtil.Create("Visual", root.transform, Layers.Enemy).transform;
            var v = rig.visual;
            EditorUtil.Sphere("Body", v, M("EnemyBody"), new Vector3(0f, 0.6f, 0f), new Vector3(0.95f, 0.75f, 0.95f));
            EditorUtil.Visual("Band", v, EditorUtil.CylinderMesh, M("EnemyDark"), new Vector3(0f, 0.6f, 0f), new Vector3(1.0f, 0.06f, 1.0f));
            EditorUtil.Box("ArmL", v, M("EnemyDark"), new Vector3(-0.7f, 0.75f, 0f), new Vector3(0.6f, 0.06f, 0.12f));
            EditorUtil.Box("ArmR", v, M("EnemyDark"), new Vector3(0.7f, 0.75f, 0f), new Vector3(0.6f, 0.06f, 0.12f));
            var rotor = EditorUtil.Create("Rotor", v, Layers.Enemy);
            rotor.transform.localPosition = new Vector3(0f, 1.05f, 0f);
            EditorUtil.Box("BladeA", rotor.transform, M("EnemyDark"), Vector3.zero, new Vector3(1.4f, 0.03f, 0.12f));
            EditorUtil.Box("BladeB", rotor.transform, M("EnemyDark"), Vector3.zero, new Vector3(0.12f, 0.03f, 1.4f));
            EditorUtil.Visual("Hub", v, EditorUtil.CylinderMesh, M("EnemyCyan"), new Vector3(0f, 0.98f, 0f), new Vector3(0.18f, 0.06f, 0.18f));

            var aim = EditorUtil.Create("AimPivot", v, Layers.Enemy);
            aim.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            rig.aimPivot = aim.transform;
            EditorUtil.Visual("Lens", aim.transform, EditorUtil.SphereMesh, M("EnemyCyan"), new Vector3(0f, 0.05f, 0.4f), Vector3.one * 0.26f);
            EditorUtil.Box("Gun", aim.transform, M("WeaponBody"), new Vector3(0f, -0.2f, 0.3f), new Vector3(0.12f, 0.12f, 0.5f));
            rig.muzzle = Muzzle(aim.transform, new Vector3(0f, -0.2f, 0.6f));

            var eye = EditorUtil.Create("Eye", root.transform, Layers.Enemy);
            eye.transform.localPosition = new Vector3(0f, 0.6f, 0.3f);
            rig.eye = eye.transform;

            var core = EditorUtil.Create("CoreHitbox", aim.transform, Layers.Enemy);
            core.transform.localPosition = new Vector3(0f, 0.05f, 0.42f);
            var coreCol = core.AddComponent<SphereCollider>();
            coreCol.radius = 0.16f;
            var hitbox = core.AddComponent<Hitbox>();
            EditorUtil.Set(hitbox, "owner", health);
            EditorUtil.Set(hitbox, "damageMultiplier", 2f);
            EditorUtil.Set(hitbox, "critical", true);

            var alert = AlertIndicator(root.transform, 1.6f);
            var enemy = root.AddComponent<DroneEnemy>();
            Wire(enemy, rig, def, reg.enemyBullet, alert);
            EditorUtil.Set(enemy, "flying", true);
            EditorUtil.Set(enemy, "hoverHeight", 4.5f);
            EditorUtil.Set(enemy, "rotor", rotor.transform);
            return root;
        }

        static GameObject BuildTurret(EnemyDefinition def)
        {
            var rig = CreateRig("Enemy_Turret", 0.45f, 1.0f, 1.2f, 0f, 1.2f, 500f);
            rig.root.GetComponent<Rigidbody>().isKinematic = true;
            var body = M("EnemyBody");
            var dark = M("EnemyDark");
            var glow = M("EnemyYellow");
            var v = rig.visual;
            EditorUtil.Visual("Base", v, EditorUtil.CylinderMesh, dark, new Vector3(0f, 0.15f, 0f), new Vector3(1.3f, 0.15f, 1.3f));
            EditorUtil.Visual("Column", v, EditorUtil.CylinderMesh, body, new Vector3(0f, 0.6f, 0f), new Vector3(0.6f, 0.35f, 0.6f));
            EditorUtil.Visual("Ring", v, EditorUtil.CylinderMesh, glow, new Vector3(0f, 0.95f, 0f), new Vector3(0.7f, 0.04f, 0.7f));
            var head = EditorUtil.Box("Head", rig.aimPivot, body, Vector3.zero, new Vector3(0.8f, 0.5f, 0.7f));
            head.AddComponent<BoxCollider>();
            EditorUtil.Box("Sensor", rig.aimPivot, glow, new Vector3(0f, 0.08f, 0.36f), new Vector3(0.3f, 0.1f, 0.04f));
            EditorUtil.Visual("BarrelL", rig.aimPivot, EditorUtil.CylinderMesh, M("WeaponMetal"), new Vector3(-0.2f, -0.08f, 0.6f), new Vector3(0.1f, 0.3f, 0.1f), new Vector3(90f, 0f, 0f));
            EditorUtil.Visual("BarrelR", rig.aimPivot, EditorUtil.CylinderMesh, M("WeaponMetal"), new Vector3(0.2f, -0.08f, 0.6f), new Vector3(0.1f, 0.3f, 0.1f), new Vector3(90f, 0f, 0f));
            rig.muzzle = Muzzle(rig.aimPivot, new Vector3(0f, -0.08f, 0.95f));

            var sensor = EditorUtil.Create("SensorHitbox", rig.aimPivot, Layers.Enemy);
            sensor.transform.localPosition = new Vector3(0f, 0.08f, 0.38f);
            var sensorCol = sensor.AddComponent<SphereCollider>();
            sensorCol.radius = 0.14f;
            var hitbox = sensor.AddComponent<Hitbox>();
            EditorUtil.Set(hitbox, "owner", rig.health);
            EditorUtil.Set(hitbox, "damageMultiplier", 2f);
            EditorUtil.Set(hitbox, "critical", true);

            var shield = EditorUtil.Create("Shield", rig.root.transform, Layers.Enemy);
            var shieldVis = EditorUtil.Visual("Bubble", shield.transform, EditorUtil.SphereMesh, lib.shield, new Vector3(0f, 0.8f, 0f), Vector3.one * 2.4f);
            EditorUtil.DisableShadows(shieldVis);
            shield.SetActive(false);

            var alert = AlertIndicator(rig.root.transform, 2.1f);
            var enemy = rig.root.AddComponent<TurretEnemy>();
            Wire(enemy, rig, def, reg.enemyBullet, alert);
            EditorUtil.Set(enemy, "stationary", true);
            EditorUtil.Set(enemy, "shieldVisual", shield);
            return rig.root;
        }
    }
}

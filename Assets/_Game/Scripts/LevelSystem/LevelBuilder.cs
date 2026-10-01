using System.Collections.Generic;
using Momentum.Bosses;
using Momentum.Enemies;
using UnityEngine;

namespace Momentum.Levels
{
    /// <summary>
    /// Fluent helper for authoring LevelData in C#. The campaign levels are written with this and turned
    /// into scenes by the DemoLevelGenerator; custom tools can use it to generate JSON maps.
    /// Positions of "standing" objects (spawn, enemies, pickups, pads...) are feet/base positions.
    /// </summary>
    public class LevelBuilder
    {
        public LevelData Data { get; }

        public LevelBuilder(string id, string name)
        {
            Data = new LevelData
            {
                levelId = id,
                levelName = name,
                author = "MOMENTUM",
                objects = new List<LevelObjectData>()
            };
        }

        public LevelObjectData Add(string type, Vector3 position, Vector3 rotation, Vector3 scale)
        {
            var d = LevelObjectCatalog.CreateData(type, position);
            d.rotation = rotation;
            d.scale = scale;
            d.id = Data.NextId();
            Data.objects.Add(d);
            return d;
        }

        public LevelObjectData Add(string type, Vector3 position, float yaw = 0f)
        {
            var def = LevelObjectCatalog.Get(type);
            return Add(type, position, new Vector3(0f, yaw, 0f), def != null ? def.defaultScale : Vector3.one);
        }

        // ------------------------------------------------------------------ Settings

        public LevelBuilder Environment(string preset)
        {
            Data.environment = EnvironmentApplier.Preset(preset);
            return this;
        }

        public LevelBuilder KillHeight(float y)
        {
            Data.killHeight = y;
            return this;
        }

        public LevelBuilder Ranks(float s, float a, float b, float c)
        {
            Data.rankTimes = new RankThresholds(s, a, b, c);
            return this;
        }

        public LevelBuilder StartingWeapons(params string[] ids)
        {
            Data.startingWeapons = new List<string>(ids);
            return this;
        }

        public LevelBuilder Music(string track)
        {
            Data.music = track;
            return this;
        }

        // ------------------------------------------------------------------ Geometry (center pivots)

        public LevelObjectData Box(Vector3 center, Vector3 size, string material = "White", float yaw = 0f)
        {
            var d = Add("Cube", center, new Vector3(0f, yaw, 0f), size);
            d.Set("material", material);
            return d;
        }

        public LevelObjectData Floor(Vector3 center, Vector3 size, string material = "LightGray")
        {
            var d = Add("Floor", center, Vector3.zero, size);
            d.Set("material", material);
            return d;
        }

        /// <summary>Floor slab whose top surface is at topY.</summary>
        public LevelObjectData FloorTop(float x, float topY, float z, float width, float depth, float thickness = 1f, string material = "LightGray")
        {
            return Floor(new Vector3(x, topY - thickness * 0.5f, z), new Vector3(width, thickness, depth), material);
        }

        public LevelObjectData Wall(Vector3 center, Vector3 size, string material = "White", float yaw = 0f)
        {
            var d = Add("Wall", center, new Vector3(0f, yaw, 0f), size);
            d.Set("material", material);
            return d;
        }

        public LevelObjectData Platform(Vector3 center, Vector3 size, string material = "Gray", float yaw = 0f)
        {
            var d = Add("Platform", center, new Vector3(0f, yaw, 0f), size);
            d.Set("material", material);
            return d;
        }

        public LevelObjectData Pillar(Vector3 baseCenter, float diameter, float height, string material = "White")
        {
            var d = Add("Pillar", baseCenter + Vector3.up * height * 0.5f, Vector3.zero, new Vector3(diameter, height, diameter));
            d.Set("material", material);
            return d;
        }

        public LevelObjectData MovingPlatform(Vector3 center, Vector3 size, Vector3 offset, float speed, float wait = 0.75f, float spin = 0f)
        {
            var d = Add("MovingPlatform", center, Vector3.zero, size);
            d.Set("offset", offset);
            d.Set("speed", speed);
            d.Set("wait", wait);
            d.Set("spin", spin);
            return d;
        }

        public LevelObjectData Door(Vector3 center, float yaw, string mode, string arena = "", Vector3? size = null)
        {
            var d = Add("Door", center, new Vector3(0f, yaw, 0f), size ?? new Vector3(4f, 5f, 0.4f));
            d.Set("mode", mode);
            d.Set("arena", arena);
            return d;
        }

        public LevelObjectData Glass(Vector3 center, Vector3 size, float yaw = 0f)
        {
            return Add("Glass", center, new Vector3(0f, yaw, 0f), size);
        }

        public LevelObjectData BreakableWall(Vector3 center, Vector3 size, float yaw = 0f, float health = 150f)
        {
            var d = Add("BreakableWall", center, new Vector3(0f, yaw, 0f), size);
            d.Set("health", health);
            return d;
        }

        public LevelObjectData KillZone(Vector3 center, Vector3 size, bool visible = true)
        {
            var d = Add("KillZone", center, Vector3.zero, size);
            d.Set("visible", visible);
            return d;
        }

        public LevelObjectData Hazard(Vector3 center, Vector3 size, float cycleOff, float cycleOn, float dps = 35f)
        {
            var d = Add("HazardPanel", center, Vector3.zero, size);
            d.Set("cycleOff", cycleOff);
            d.Set("cycleOn", cycleOn);
            d.Set("dps", dps);
            return d;
        }

        public LevelObjectData StartGate(Vector3 center, float yaw, Vector3 size)
        {
            return Add("StartTrigger", center, new Vector3(0f, yaw, 0f), size);
        }

        public LevelObjectData Secret(Vector3 center, Vector3 size)
        {
            return Add("Secret", center, Vector3.zero, size);
        }

        // ------------------------------------------------------------------ Geometry (bottom pivots)

        public LevelObjectData Ramp(Vector3 bottomCenter, float yaw, Vector3 size, string material = "LightGray")
        {
            var d = Add("Ramp", bottomCenter, new Vector3(0f, yaw, 0f), size);
            d.Set("material", material);
            return d;
        }

        public LevelObjectData Stairs(Vector3 bottomCenter, float yaw, Vector3 size, string material = "Gray")
        {
            var d = Add("Stairs", bottomCenter, new Vector3(0f, yaw, 0f), size);
            d.Set("material", material);
            return d;
        }

        public LevelObjectData SpeedPad(Vector3 position, float yaw, float speed = 26f, Vector3? size = null)
        {
            var d = Add("SpeedPad", position, new Vector3(0f, yaw, 0f), size ?? new Vector3(3f, 1f, 5f));
            d.Set("speed", speed);
            return d;
        }

        // ------------------------------------------------------------------ Gameplay objects

        public LevelObjectData Spawn(Vector3 feet, float yaw)
        {
            return Add("SpawnPoint", feet, yaw);
        }

        public LevelObjectData Checkpoint(Vector3 feet, float yaw, int order)
        {
            var d = Add("Checkpoint", feet, yaw);
            d.Set("order", order);
            return d;
        }

        public LevelObjectData Finish(Vector3 feet, float yaw, bool requireBoss = true)
        {
            var d = Add("FinishTrigger", feet, yaw);
            d.Set("requireBoss", requireBoss);
            return d;
        }

        public LevelObjectData LaunchPad(Vector3 position, float yaw, float up = 22f, float forward = 8f)
        {
            var d = Add("LaunchPad", position, yaw);
            d.Set("up", up);
            d.Set("forward", forward);
            return d;
        }

        public LevelObjectData GrapplePoint(Vector3 position)
        {
            return Add("GrapplePoint", position, 0f);
        }

        public LevelObjectData Sign(Vector3 position, float yaw, string text, float size = 1f)
        {
            var d = Add("Sign", position, yaw);
            d.Set("text", text);
            d.Set("size", size);
            return d;
        }

        public LevelObjectData Enemy(EnemyType type, Vector3 feet, float yaw = 0f, string arena = "", float patrol = 4f)
        {
            var d = Add("EnemySpawn", feet, yaw);
            d.Set("enemy", type.ToString());
            d.Set("arena", arena);
            d.Set("patrol", patrol);
            return d;
        }

        public LevelObjectData Weapon(string weaponId, Vector3 feet, float yaw = 0f)
        {
            var d = Add("WeaponPickup", feet, yaw);
            d.Set("weapon", weaponId);
            return d;
        }

        public LevelObjectData Ammo(Vector3 feet)
        {
            return Add("AmmoPickup", feet, 0f);
        }

        public LevelObjectData Health(Vector3 feet)
        {
            return Add("HealthPickup", feet, 0f);
        }

        public LevelObjectData Crate(Vector3 feet, float size = 1.2f, float yaw = 0f)
        {
            return Add("Crate", feet, new Vector3(0f, yaw, 0f), Vector3.one * size);
        }

        public LevelObjectData Barrel(Vector3 feet)
        {
            return Add("Barrel", feet, 0f);
        }

        public LevelObjectData ExplosiveBarrel(Vector3 feet)
        {
            return Add("ExplosiveBarrel", feet, 0f);
        }

        public LevelObjectData Light(Vector3 position, Color color, float range = 12f, float intensity = 2f, bool pulse = false)
        {
            var d = Add("Light", position, 0f);
            d.Set("color", color);
            d.Set("range", range);
            d.Set("intensity", intensity);
            d.Set("pulse", pulse);
            return d;
        }

        public LevelObjectData Boss(BossType type, Vector3 feet, float yaw, float arenaRadius, float activationRadius)
        {
            var d = Add("BossTrigger", feet, yaw);
            d.Set("boss", type.ToString());
            d.Set("radius", arenaRadius);
            d.Set("activation", activationRadius);
            return d;
        }

        public LevelObjectData Socket(BossSocketType type, Vector3 position)
        {
            var d = Add("BossSocket", position, 0f);
            d.Set("socket", type.ToString());
            return d;
        }
    }
}

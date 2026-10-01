using System;
using System.Collections.Generic;
using UnityEngine;

namespace Momentum.Levels
{
    public enum PropertyKind
    {
        Float,
        Int,
        Bool,
        String,
        Choice,
        Vector3,
        Color
    }

    /// <summary>Describes one editable property of a level object type.</summary>
    public class PropertyDef
    {
        public string key;
        public string label;
        public PropertyKind kind;
        public string defaultValue;
        public float min = float.MinValue;
        public float max = float.MaxValue;
        public string[] options;

        public PropertyDef(string key, string label, PropertyKind kind, string defaultValue)
        {
            this.key = key;
            this.label = label;
            this.kind = kind;
            this.defaultValue = defaultValue;
        }
    }

    /// <summary>Describes a placeable level object type (used by the editor, loader and generator).</summary>
    public class LevelObjectDef
    {
        public string type;
        public string displayName;
        public string category;
        public string description;
        public Vector3 defaultScale = Vector3.one;
        /// <summary>True when scale is applied to the object (geometry). False for fixed-size objects.</summary>
        public bool scalable = true;
        /// <summary>True when the pivot is at the center (placement offsets by half height).</summary>
        public bool centerPivot = true;
        public int maxCount;
        public Color editorColor = Color.white;
        public readonly List<PropertyDef> properties = new List<PropertyDef>();
    }

    /// <summary>
    /// Central list of every object type available in custom levels / the level editor, with categories,
    /// default sizes and editable properties.
    /// </summary>
    public static class LevelObjectCatalog
    {
        public const string Geometry = "Geometry";
        public const string Gameplay = "Gameplay";
        public const string Movement = "Movement";
        public const string Enemies = "Enemies";
        public const string Pickups = "Pickups";
        public const string Props = "Props";
        public const string Lights = "Lights";
        public const string Boss = "Boss";

        public static readonly string[] Categories = { Geometry, Movement, Gameplay, Enemies, Pickups, Props, Lights, Boss };

        public static readonly string[] WeaponIds = { "pistol", "smg", "shotgun", "rifle", "revolver", "rocket", "grenade", "railgun" };
        public static readonly string[] EnemyTypes = { "Gunner", "Shotgunner", "Sniper", "Charger", "Drone", "Turret" };
        public static readonly string[] BossTypes = { "Warden", "Sentinel", "Juggernaut", "Core" };
        public static readonly string[] DoorModes = { "Proximity", "ArenaClear", "BossFight", "Interact", "Locked" };
        public static readonly string[] SocketTypes = { "TurretMount", "EnemySpawn", "WeakPoint", "LaserEmitter" };

        static readonly Dictionary<string, LevelObjectDef> defs = new Dictionary<string, LevelObjectDef>(StringComparer.OrdinalIgnoreCase);
        static readonly List<LevelObjectDef> ordered = new List<LevelObjectDef>();

        public static IReadOnlyList<LevelObjectDef> All => ordered;

        static LevelObjectCatalog()
        {
            // Geometry
            Add("Cube", "Cube", Geometry, new Vector3(2f, 2f, 2f), "Solid block.").WithMaterial("White").WithNoWallRun();
            Add("Floor", "Floor", Geometry, new Vector3(10f, 0.5f, 10f), "Large floor slab.").WithMaterial("LightGray");
            Add("Wall", "Wall", Geometry, new Vector3(0.5f, 6f, 10f), "Tall wall - great for wallrunning.").WithMaterial("White").WithNoWallRun();
            Add("Ramp", "Ramp", Geometry, new Vector3(4f, 2f, 6f), "Slope rising toward its forward direction.", center: false).WithMaterial("LightGray");
            Add("Stairs", "Stairs", Geometry, new Vector3(3f, 2f, 4f), "Stairs (smooth slope collider).", center: false).WithMaterial("Gray");
            Add("Platform", "Platform", Geometry, new Vector3(4f, 0.4f, 4f), "Thin platform.").WithMaterial("Gray");
            Add("Pillar", "Pillar", Geometry, new Vector3(1.5f, 8f, 1.5f), "Cylindrical pillar.").WithMaterial("White");
            Add("MovingPlatform", "Moving Platform", Movement, new Vector3(4f, 0.4f, 4f), "Moves between its position and position + offset.")
                .Prop("offset", "Offset (x,y,z)", PropertyKind.Vector3, "0,0,10")
                .Prop("speed", "Speed", PropertyKind.Float, "4", 0.1f, 50f)
                .Prop("wait", "Wait Time", PropertyKind.Float, "0.75", 0f, 10f)
                .Prop("spin", "Spin (deg/s)", PropertyKind.Float, "0", -360f, 360f)
                .Prop("pingPong", "Ping Pong", PropertyKind.Bool, "true");
            Add("Door", "Door", Geometry, new Vector3(4f, 5f, 0.4f), "Sliding door.")
                .Choice("mode", "Mode", DoorModes, "Proximity")
                .Prop("arena", "Arena Id", PropertyKind.String, "");
            Add("Glass", "Glass", Geometry, new Vector3(4f, 4f, 0.15f), "Breakable glass. Run through it fast or shoot it.")
                .Prop("health", "Health", PropertyKind.Float, "10", 1f, 1000f);
            Add("BreakableWall", "Breakable Wall", Geometry, new Vector3(6f, 6f, 1f), "Destructible wall (shootable, boss charges smash it).")
                .Prop("health", "Health", PropertyKind.Float, "150", 1f, 5000f)
                .Prop("bossBreakable", "Boss Breakable", PropertyKind.Bool, "true");

            // Movement
            Add("LaunchPad", "Launch Pad", Movement, new Vector3(2.5f, 0.3f, 2.5f), "Launches the player upward and forward.", scalable: false, center: false)
                .Prop("up", "Upward Velocity", PropertyKind.Float, "22", 1f, 80f)
                .Prop("forward", "Forward Velocity", PropertyKind.Float, "8", -50f, 80f);
            Add("SpeedPad", "Speed Pad", Movement, new Vector3(3f, 1f, 5f), "Boosts horizontal speed along its forward direction.", center: false)
                .Prop("speed", "Boost Speed", PropertyKind.Float, "26", 5f, 80f);
            Add("GrapplePoint", "Grapple Point", Movement, Vector3.one, "Magnetic grapple anchor.", scalable: false);

            // Gameplay
            Add("SpawnPoint", "Spawn Point", Gameplay, Vector3.one, "Player start. Exactly one per level.", scalable: false, center: false).maxCount = 1;
            Add("StartTrigger", "Start Gate", Gameplay, new Vector3(6f, 4f, 1f), "Timer starts when the player passes through.");
            Add("Checkpoint", "Checkpoint", Gameplay, Vector3.one, "Respawn point and time split.", scalable: false, center: false)
                .Prop("order", "Order", PropertyKind.Int, "1", 0f, 999f);
            Add("FinishTrigger", "Finish", Gameplay, Vector3.one, "Finish line.", scalable: false, center: false)
                .Prop("requireBoss", "Requires Boss Defeat", PropertyKind.Bool, "true");
            Add("KillZone", "Kill Zone", Gameplay, new Vector3(20f, 1f, 20f), "Instant death volume (pits, lava).")
                .Prop("visible", "Visible In Play", PropertyKind.Bool, "true");
            Add("HazardPanel", "Hazard Panel", Gameplay, new Vector3(4f, 0.2f, 4f), "Electrified floor panel (timed or boss-triggered).")
                .Prop("cycleOff", "Off Time (0 = boss only)", PropertyKind.Float, "3", 0f, 60f)
                .Prop("cycleOn", "On Time", PropertyKind.Float, "2", 0.1f, 60f)
                .Prop("dps", "Damage / sec", PropertyKind.Float, "35", 0f, 500f);
            Add("Secret", "Secret Area", Gameplay, new Vector3(4f, 4f, 4f), "Hidden area trigger.");
            Add("Sign", "Sign", Gameplay, Vector3.one, "3D text sign. Use \\n for new lines.", scalable: false)
                .Prop("text", "Text", PropertyKind.String, "HELLO")
                .Prop("size", "Size", PropertyKind.Float, "1", 0.2f, 10f)
                .Prop("color", "Color", PropertyKind.Color, "#FFFFFF");

            // Enemies
            Add("EnemySpawn", "Enemy", Enemies, Vector3.one, "Enemy placed in the level.", scalable: false, center: false)
                .Choice("enemy", "Enemy Type", EnemyTypes, "Gunner")
                .Prop("arena", "Arena Id", PropertyKind.String, "")
                .Prop("patrol", "Patrol Radius", PropertyKind.Float, "4", 0f, 50f);

            // Pickups
            Add("WeaponPickup", "Weapon", Pickups, Vector3.one, "Weapon pickup.", scalable: false, center: false)
                .Choice("weapon", "Weapon", WeaponIds, "shotgun")
                .Prop("respawn", "Respawns", PropertyKind.Bool, "true");
            Add("AmmoPickup", "Ammo Box", Pickups, Vector3.one, "Refills reserve ammo.", scalable: false, center: false)
                .Prop("amount", "Amount (fraction)", PropertyKind.Float, "0.4", 0.05f, 1f)
                .Prop("respawn", "Respawns", PropertyKind.Bool, "true");
            Add("HealthPickup", "Health", Pickups, Vector3.one, "Restores health.", scalable: false, center: false)
                .Prop("amount", "Amount", PropertyKind.Float, "40", 1f, 200f)
                .Prop("respawn", "Respawns", PropertyKind.Bool, "true");

            // Props
            Add("Crate", "Crate", Props, Vector3.one * 1.2f, "Breakable physics crate.", center: false);
            Add("Barrel", "Barrel", Props, Vector3.one, "Physics barrel.", scalable: false, center: false);
            Add("ExplosiveBarrel", "Explosive Barrel", Props, Vector3.one, "Explodes when destroyed. Great for boosts.", scalable: false, center: false);

            // Lights
            Add("Light", "Light", Lights, Vector3.one, "Decorative light.", scalable: false)
                .Prop("color", "Color", PropertyKind.Color, "#FFD9A0")
                .Prop("range", "Range", PropertyKind.Float, "12", 1f, 100f)
                .Prop("intensity", "Intensity", PropertyKind.Float, "2", 0f, 10f)
                .Prop("pulse", "Pulse", PropertyKind.Bool, "false");

            // Boss
            Add("BossTrigger", "Boss Arena", Boss, Vector3.one, "Boss + arena trigger. Fight starts when the player gets close.", scalable: false, center: false)
                .Choice("boss", "Boss", BossTypes, "Warden")
                .Prop("radius", "Arena Radius", PropertyKind.Float, "30", 8f, 200f)
                .Prop("activation", "Activation Radius", PropertyKind.Float, "22", 4f, 200f)
                .Prop("health", "Health (0 = default)", PropertyKind.Float, "0", 0f, 100000f);
            Add("BossSocket", "Boss Socket", Boss, Vector3.one, "Turret mount / enemy spawn / weak point / laser emitter for The Core.", scalable: false)
                .Choice("socket", "Socket Type", SocketTypes, "EnemySpawn");

            defs["SpawnPoint"].editorColor = new Color(0.3f, 1f, 0.4f);
            defs["StartTrigger"].editorColor = new Color(0.3f, 0.8f, 1f);
            defs["FinishTrigger"].editorColor = new Color(1f, 0.85f, 0.2f);
            defs["KillZone"].editorColor = new Color(1f, 0.2f, 0.2f);
            defs["Secret"].editorColor = new Color(0.8f, 0.4f, 1f);
        }

        static LevelObjectDef Add(string type, string name, string category, Vector3 scale, string description, bool scalable = true, bool center = true)
        {
            var def = new LevelObjectDef
            {
                type = type,
                displayName = name,
                category = category,
                defaultScale = scale,
                description = description,
                scalable = scalable,
                centerPivot = center
            };
            defs[type] = def;
            ordered.Add(def);
            return def;
        }

        static LevelObjectDef Prop(this LevelObjectDef def, string key, string label, PropertyKind kind, string value, float min = float.MinValue, float max = float.MaxValue)
        {
            def.properties.Add(new PropertyDef(key, label, kind, value) { min = min, max = max });
            return def;
        }

        static LevelObjectDef Choice(this LevelObjectDef def, string key, string label, string[] options, string value)
        {
            def.properties.Add(new PropertyDef(key, label, PropertyKind.Choice, value) { options = options });
            return def;
        }

        static LevelObjectDef WithMaterial(this LevelObjectDef def, string materialKey)
        {
            return def.Choice("material", "Material", MaterialLibrary.GeometryKeys, materialKey);
        }

        static LevelObjectDef WithNoWallRun(this LevelObjectDef def)
        {
            return def.Prop("noWallRun", "No Wallrun", PropertyKind.Bool, "false");
        }

        public static LevelObjectDef Get(string type)
        {
            if (string.IsNullOrEmpty(type)) return null;
            defs.TryGetValue(type, out var def);
            return def;
        }

        public static IEnumerable<LevelObjectDef> InCategory(string category)
        {
            foreach (var d in ordered)
            {
                if (d.category == category) yield return d;
            }
        }

        /// <summary>Creates level data for a new object of the given type with default scale and properties.</summary>
        public static LevelObjectData CreateData(string type, Vector3 position)
        {
            var def = Get(type);
            var data = new LevelObjectData(type, position, Vector3.zero, def != null ? def.defaultScale : Vector3.one);
            if (def != null)
            {
                foreach (var p in def.properties) data.Set(p.key, p.defaultValue);
            }
            return data;
        }
    }
}

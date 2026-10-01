using System;
using Momentum.Audio;
using Momentum.Bosses;
using Momentum.Enemies;
using Momentum.Levels;
using UnityEngine;

namespace Momentum.EditorTools
{
    /// <summary>
    /// The ten original campaign levels, authored in code with LevelBuilder. The DemoLevelGenerator turns each
    /// into a scene (with prefab links) and exports the same layout as JSON so it can be opened in the in-game
    /// level editor. Coordinates: +Z is "forward" through each level, Y is up, floor tops are given explicitly.
    ///
    /// Movement reference used for the layouts (default MovementSettings): run 11 m/s, sprint 14 m/s, jump
    /// height ~1.6 m, flat jump ~7 m, mantle up to 2.3 m, vault up to 1.15 m, wallrun ~20 m with a ~2-4 m drop,
    /// grapple range 42 m, launch pad (22 up) ~8.6 m.
    /// </summary>
    public static class CampaignLevels
    {
        public class Entry
        {
            public string id;
            public int number;
            public string name;
            public string scene;
            public string description;
            public bool boss;
            public string bossName = "";
            public Color accent;
            public MusicTrack music = MusicTrack.Level;
            public RankThresholds ranks;
            public Func<LevelData> build;
        }

        public static readonly Entry[] All =
        {
            new Entry { id = "level01", number = 1, name = "Training Facility", scene = "Level01_TrainingFacility", accent = new Color(1f, 0.45f, 0.1f),
                description = "Learn to move: jumping, mantling, sliding, wallrunning, launch pads and the grapple. Then clear your first arena.",
                ranks = new RankThresholds(45f, 60f, 85f, 130f), build = TrainingFacility },
            new Entry { id = "level02", number = 2, name = "Warehouse", scene = "Level02_Warehouse", accent = new Color(1f, 0.85f, 0.2f),
                description = "A dark storage hall. Fight through the aisles or take the catwalk. Pick up the SMG.",
                ranks = new RankThresholds(55f, 75f, 100f, 150f), build = Warehouse },
            new Entry { id = "level03", number = 3, name = "Construction Site", scene = "Level03_ConstructionSite", accent = new Color(1f, 0.75f, 0.1f),
                description = "Climb an unfinished building, ride the crane load and reach the roof of the second tower.",
                ranks = new RankThresholds(60f, 80f, 110f, 160f), build = ConstructionSite },
            new Entry { id = "level04", number = 4, name = "Tower", scene = "Level04_Tower", accent = new Color(0.3f, 0.6f, 1f),
                description = "A hollow tower at night. Spiral up the ledges above the coolant pool. The revolver waits at the bottom.",
                ranks = new RankThresholds(75f, 100f, 135f, 190f), build = Tower },
            new Entry { id = "level05", number = 5, name = "Research Complex", scene = "Level05_ResearchComplex", accent = new Color(0.25f, 0.9f, 0.6f),
                description = "Glass labs, electrified floors and a sniper gallery. Grab the grenade launcher.",
                ranks = new RankThresholds(80f, 105f, 140f, 200f), build = ResearchComplex },
            new Entry { id = "level06", number = 6, name = "Reactor", scene = "Level06_Reactor", accent = new Color(0.95f, 0.3f, 0.15f),
                boss = true, bossName = "THE JUGGERNAUT", music = MusicTrack.Boss,
                description = "Cross the coolant channels, take the rocket launcher and bring down The Juggernaut.",
                ranks = new RankThresholds(110f, 145f, 195f, 280f), build = Reactor },
            new Entry { id = "level07", number = 7, name = "Sky Facility", scene = "Level07_SkyFacility", accent = new Color(0.4f, 0.8f, 1f),
                description = "Floating platforms high above the clouds. Speed pads, grapple chains, wall jumps and the railgun.",
                ranks = new RankThresholds(70f, 95f, 130f, 185f), build = SkyFacility },
            new Entry { id = "level08", number = 8, name = "The Warden", scene = "Level08_TheWarden", accent = new Color(0.9f, 0.12f, 0.2f),
                boss = true, bossName = "THE WARDEN", music = MusicTrack.Boss,
                description = "The armored guardian of the facility. Ground slams, shotgun blasts, charges and missiles.",
                ranks = new RankThresholds(90f, 120f, 160f, 240f), build = TheWarden },
            new Entry { id = "level09", number = 9, name = "The Sentinel", scene = "Level09_TheSentinel", accent = new Color(0.7f, 0.25f, 1f),
                boss = true, bossName = "THE SENTINEL", music = MusicTrack.Boss,
                description = "A floating eye that sweeps lasers, summons drones and electrifies the arena floor.",
                ranks = new RankThresholds(100f, 130f, 175f, 260f), build = TheSentinel },
            new Entry { id = "level10", number = 10, name = "The Core", scene = "Level10_TheCore", accent = new Color(1f, 0.15f, 0.25f),
                boss = true, bossName = "THE CORE", music = MusicTrack.Boss,
                description = "The heart of the facility. Four stages: turrets, lasers, summons and the weak points above the arena.",
                ranks = new RankThresholds(150f, 190f, 250f, 360f), build = TheCore }
        };

        // ------------------------------------------------------------------ Helpers

        static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);

        static LevelBuilder New(Entry e, string environment, float killHeight, string music, params string[] weapons)
        {
            var b = new LevelBuilder(e.id, e.name).Environment(environment).KillHeight(killHeight).Ranks(e.ranks.s, e.ranks.a, e.ranks.b, e.ranks.c).Music(music);
            b.StartingWeapons(weapons);
            b.Data.description = e.description;
            return b;
        }

        static Entry Get(string id)
        {
            foreach (var e in All) if (e.id == id) return e;
            return null;
        }

        /// <summary>Floor slab covering x0..x1, z0..z1 with its top surface at 'top'.</summary>
        static LevelObjectData Rect(LevelBuilder b, float x0, float x1, float z0, float z1, float top, string mat = "LightGray", float thickness = 1f)
        {
            return b.FloorTop((x0 + x1) * 0.5f, top, (z0 + z1) * 0.5f, Mathf.Abs(x1 - x0), Mathf.Abs(z1 - z0), thickness, mat);
        }

        /// <summary>Wall along Z at x, spanning z0..z1, from y0 up by h.</summary>
        static LevelObjectData WallZ(LevelBuilder b, float x, float z0, float z1, float y0, float h, string mat = "White", float t = 0.6f)
        {
            return b.Wall(V(x, y0 + h * 0.5f, (z0 + z1) * 0.5f), V(t, h, Mathf.Abs(z1 - z0)), mat, 0f);
        }

        /// <summary>Wall along X at z, spanning x0..x1, from y0 up by h.</summary>
        static LevelObjectData WallX(LevelBuilder b, float z, float x0, float x1, float y0, float h, string mat = "White", float t = 0.6f)
        {
            return b.Wall(V((x0 + x1) * 0.5f, y0 + h * 0.5f, z), V(t, h, Mathf.Abs(x1 - x0)), mat, 90f);
        }

        /// <summary>Wall along X with an opening (gapWidth wide, from the bottom up to gapHeight) centered at gapX.</summary>
        static void WallXGap(LevelBuilder b, float z, float x0, float x1, float y0, float h, float gapX, float gapWidth, float gapHeight, string mat = "White")
        {
            float g0 = gapX - gapWidth * 0.5f;
            float g1 = gapX + gapWidth * 0.5f;
            if (g0 > x0 + 0.01f) WallX(b, z, x0, g0, y0, h, mat);
            if (x1 > g1 + 0.01f) WallX(b, z, g1, x1, y0, h, mat);
            if (gapHeight < h - 0.01f) WallX(b, z, g0, g1, y0 + gapHeight, h - gapHeight, mat);
        }

        /// <summary>Wall along Z with an opening centered at gapZ.</summary>
        static void WallZGap(LevelBuilder b, float x, float z0, float z1, float y0, float h, float gapZ, float gapWidth, float gapHeight, string mat = "White")
        {
            float g0 = gapZ - gapWidth * 0.5f;
            float g1 = gapZ + gapWidth * 0.5f;
            if (g0 > z0 + 0.01f) WallZ(b, x, z0, g0, y0, h, mat);
            if (z1 > g1 + 0.01f) WallZ(b, x, g1, z1, y0, h, mat);
            if (gapHeight < h - 0.01f) WallZ(b, x, g0, g1, y0 + gapHeight, h - gapHeight, mat);
        }

        /// <summary>Visible kill volume (lava, coolant, bottomless pit) whose top surface is at y.</summary>
        static LevelObjectData Pit(LevelBuilder b, float x0, float x1, float z0, float z1, float top, bool visible = true)
        {
            return b.KillZone(V((x0 + x1) * 0.5f, top - 1f, (z0 + z1) * 0.5f), V(Mathf.Abs(x1 - x0), 2f, Mathf.Abs(z1 - z0)), visible);
        }

        /// <summary>Thin platform with its top at 'top'.</summary>
        static LevelObjectData Ledge(LevelBuilder b, float x0, float x1, float z0, float z1, float top, string mat = "Gray")
        {
            return b.Platform(V((x0 + x1) * 0.5f, top - 0.2f, (z0 + z1) * 0.5f), V(Mathf.Abs(x1 - x0), 0.4f, Mathf.Abs(z1 - z0)), mat);
        }

        static readonly Color Warm = new Color(1f, 0.82f, 0.55f);
        static readonly Color Cold = new Color(0.55f, 0.8f, 1f);
        static readonly Color Red = new Color(1f, 0.2f, 0.18f);
        static readonly Color Purple = new Color(0.7f, 0.35f, 1f);

        // ------------------------------------------------------------------ 01 Training Facility

        static LevelData TrainingFacility()
        {
            var b = New(Get("level01"), "Day", -25f, "Level");

            // Start room
            Rect(b, -10f, 10f, -10f, 20f, 0f);
            WallZ(b, -10f, -10f, 20f, 0f, 8f);
            WallZ(b, 10f, -10f, 20f, 0f, 8f);
            WallX(b, -10f, -10f, 10f, 0f, 8f);
            WallXGap(b, 20f, -10f, 10f, 0f, 8f, 0f, 8f, 5f);
            b.Spawn(V(0f, 0f, -6f), 0f);
            b.Sign(V(0f, 3.2f, 2f), 0f, "WASD - MOVE    MOUSE - LOOK\\nSPACE - JUMP    SHIFT - SPRINT", 0.7f);
            b.Weapon("pistol", V(-4f, 0f, 8f));
            b.Sign(V(-4f, 2.8f, 11f), 0f, "WALK INTO WEAPONS TO TAKE THEM\\nLMB FIRE   R RELOAD   RMB ZOOM", 0.5f);
            b.StartGate(V(0f, 2.5f, 20f), 0f, V(8f, 5f, 1f));

            // Jump course over lava
            Rect(b, -5f, 5f, 20f, 32f, 0f);
            Rect(b, -5f, 5f, 36f, 48f, 0f);
            Rect(b, -5f, 5f, 53f, 62f, 0f);
            Pit(b, -20f, 20f, 20f, 62f, -5f);
            b.Sign(V(0f, 3f, 25f), 0f, "JUMP THE GAPS", 1f);
            b.Box(V(0f, 1f, 64f), V(10f, 2f, 4f), "Gray");
            b.Sign(V(0f, 4.2f, 57f), 0f, "JUMP INTO LEDGES TO CLIMB THEM", 0.8f);
            Rect(b, -5f, 5f, 66f, 82f, 2f);
            b.Box(V(0f, 2.5f, 74f), V(10f, 1f, 1f), "Accent");
            b.Sign(V(0f, 5f, 69f), 0f, "RUN INTO LOW OBSTACLES TO VAULT", 0.7f);

            // Slide tunnel
            Rect(b, -5f, 5f, 82f, 112f, 2f);
            WallZ(b, -5.3f, 82f, 108f, 2f, 7f);
            WallZ(b, 5.3f, 82f, 108f, 2f, 7f);
            b.Box(V(0f, 5.75f, 98f), V(10f, 4.5f, 8f), "Dark");
            b.Sign(V(0f, 5.2f, 87f), 0f, "HOLD LEFT CTRL WHILE RUNNING TO SLIDE", 0.7f);

            // Wallrun pit
            WallZ(b, 5.3f, 108f, 130f, -4f, 14f, "AccentBlue");
            WallZ(b, -5.3f, 113f, 128f, -4f, 14f, "AccentBlue");
            Pit(b, -20f, 20f, 112f, 128f, -5f);
            b.Sign(V(0f, 5.4f, 105f), 0f, "JUMP AT A WALL HOLDING W TO WALLRUN\\nSPACE TO JUMP OFF", 0.6f);
            Rect(b, -6f, 6f, 128f, 160f, 1f);
            b.Checkpoint(V(0f, 1f, 140f), 0f, 1);

            // First arena
            Rect(b, -15f, 15f, 160f, 210f, 1f);
            WallZ(b, -15f, 160f, 210f, 1f, 9f);
            WallZ(b, 15f, 160f, 210f, 1f, 9f);
            WallXGap(b, 160f, -15f, 15f, 1f, 9f, 0f, 12f, 9f);
            WallXGap(b, 210f, -15f, 15f, 1f, 9f, 0f, 6f, 5f);
            b.Door(V(0f, 3.5f, 210f), 0f, "ArenaClear", "train", V(6f, 5f, 0.4f));
            b.Weapon("shotgun", V(0f, 1f, 165f));
            b.Sign(V(0f, 4f, 167f), 0f, "SHOTGUN: SHOOT THE FLOOR WHILE JUMPING\\nTO BLAST YOURSELF UPWARD", 0.6f);
            b.Enemy(EnemyType.Gunner, V(-8f, 1f, 186f), 180f, "train", 3f);
            b.Enemy(EnemyType.Gunner, V(9f, 1f, 194f), 180f, "train", 3f);
            b.Enemy(EnemyType.Turret, V(-10f, 1f, 204f), 150f, "train", 0f);
            b.Box(V(-6f, 2f, 180f), V(3f, 2f, 3f), "Gray");
            b.Box(V(7f, 1.75f, 186f), V(4f, 1.5f, 2f), "Gray");
            b.Crate(V(-11f, 1f, 172f));
            b.Crate(V(11.5f, 1f, 175f), 1.5f);
            b.ExplosiveBarrel(V(9f, 1f, 199f));
            b.ExplosiveBarrel(V(-12f, 1f, 200f));
            b.Health(V(13f, 1f, 164f));
            b.Ammo(V(-13f, 1f, 164f));

            // Launch pad up the block
            Rect(b, -5f, 5f, 210f, 226f, 1f);
            Rect(b, -5f, 5f, 226f, 252f, 8f, "White", 7f);
            b.LaunchPad(V(0f, 1f, 222.5f), 0f, 22f, 3f);
            b.Sign(V(0f, 4f, 214f), 0f, "LAUNCH PADS SEND YOU FLYING", 0.8f);
            b.Checkpoint(V(0f, 8f, 244f), 0f, 2);

            // Grapple gap
            b.Sign(V(0f, 11f, 236f), 0f, "AIM AT A BLUE ORB AND HOLD Q TO GRAPPLE\\nRELEASE Q TO LET GO", 0.6f);
            b.GrapplePoint(V(0f, 17f, 262f));
            b.GrapplePoint(V(0f, 17f, 279f));
            Pit(b, -25f, 25f, 252f, 290f, -5f);
            Rect(b, -7f, 7f, 290f, 320f, 6f);

            // Secret ledge to the right of the grapple gap
            b.GrapplePoint(V(11f, 20f, 266f));
            Ledge(b, 13f, 19f, 267f, 273f, 13f, "AccentBlue");
            b.Secret(V(16f, 15f, 270f), V(5f, 3.5f, 5f));
            b.Health(V(16f, 13f, 270f));

            // Finish
            b.Enemy(EnemyType.Drone, V(-4f, 6f, 304f), 180f);
            b.Pillar(V(-6f, 6f, 316f), 1f, 6f, "Accent");
            b.Pillar(V(6f, 6f, 316f), 1f, 6f, "Accent");
            b.Finish(V(0f, 6f, 314f), 0f, false);
            return b.Data;
        }

        // ------------------------------------------------------------------ 02 Warehouse

        static LevelData Warehouse()
        {
            var b = New(Get("level02"), "Industrial", -20f, "Level", "pistol");

            Rect(b, -24f, 24f, 0f, 90f, 0f, "Gray");
            WallZ(b, -24f, 0f, 140f, 0f, 14f, "Dark");
            WallZ(b, 24f, 0f, 140f, 0f, 14f, "Dark");
            WallX(b, 0f, -24f, 24f, 0f, 14f, "Dark");
            WallX(b, 140f, -24f, -3f, 0f, 14f, "Dark");
            WallX(b, 140f, 3f, 24f, 0f, 14f, "Dark");
            WallX(b, 140f, -3f, 3f, 8f, 6f, "Dark");
            for (int i = 1; i <= 6; i++) b.Box(V(0f, 13.5f, i * 20f), V(48f, 0.6f, 0.6f), "Dark");
            foreach (float z in new[] { 15f, 45f, 75f, 105f, 130f })
            {
                b.Light(V(-12f, 12f, z), Warm, 26f, 1.6f);
                b.Light(V(12f, 12f, z), Warm, 26f, 1.6f);
            }

            b.Spawn(V(0f, 0f, 5f), 0f);
            b.Sign(V(0f, 3.5f, 11f), 0f, "WAREHOUSE 7\\nFIGHT THROUGH THE AISLES OR TAKE THE CATWALK", 0.7f);
            b.StartGate(V(0f, 3f, 16f), 0f, V(48f, 6f, 1f));

            // Shelving rows (walls of the centre aisle, tops walkable)
            b.Box(V(9f, 3f, 40f), V(3f, 6f, 40f), "AccentBlue");
            b.Box(V(-9f, 3f, 40f), V(3f, 6f, 40f), "AccentBlue");
            b.Weapon("smg", V(0f, 0f, 22f));
            b.Crate(V(-3f, 0f, 27f), 1.5f);
            b.Crate(V(3.5f, 0f, 34f), 1.5f);
            b.Crate(V(-2f, 0f, 43f), 1.2f);
            b.Box(V(0f, 1f, 51f), V(6f, 2f, 1.5f), "Gray");
            b.Enemy(EnemyType.Gunner, V(-3f, 0f, 47f), 180f, "", 3f);
            b.Enemy(EnemyType.Shotgunner, V(3f, 0f, 56f), 180f, "", 2f);

            // Right lane with explosive barrels
            b.ExplosiveBarrel(V(16f, 0f, 40f));
            b.ExplosiveBarrel(V(17.2f, 0f, 41.4f));
            b.ExplosiveBarrel(V(15.5f, 0f, 42.5f));
            b.Enemy(EnemyType.Gunner, V(17f, 0f, 47f), 180f, "", 4f);
            b.Crate(V(20f, 0f, 30f), 1.8f);

            // Hidden launch pad onto the right shelf (secret on top)
            b.LaunchPad(V(12f, 0f, 30f), -90f, 17f, 3f);
            b.Secret(V(9f, 7.5f, 57f), V(3f, 3f, 4f));
            b.Health(V(9f, 6f, 57.5f));
            b.Ammo(V(9f, 6f, 54.5f));

            // Catwalk on the left (fast route)
            b.Stairs(V(-19f, 0f, 14f), 0f, V(4f, 6f, 10f));
            Ledge(b, -21f, -17f, 19f, 60f, 6f);
            Ledge(b, -21f, -17f, 66f, 118f, 6f);
            b.Ammo(V(-19f, 6f, 40f));

            // Partition: glass in the middle, breakable wall on the right
            WallX(b, 64f, -24f, -6f, 0f, 6f, "LightGray");
            b.Glass(V(0f, 2.5f, 64f), V(12f, 5f, 0.15f));
            WallX(b, 64f, -6f, 6f, 5f, 1f, "LightGray");
            WallX(b, 64f, 6f, 12f, 0f, 6f, "LightGray");
            b.BreakableWall(V(16f, 3f, 64f), V(8f, 6f, 0.8f), 0f, 120f);
            WallX(b, 64f, 20f, 24f, 0f, 6f, "LightGray");
            b.Checkpoint(V(0f, 0f, 68f), 0f, 1);

            // Conveyor and loading dock
            b.SpeedPad(V(0f, 0f, 72f), 0f, 24f, V(4f, 1f, 9f));
            b.Ramp(V(0f, 0f, 86f), 0f, V(6f, 3f, 8f), "Accent");
            b.Stairs(V(-12f, 0f, 86f), 0f, V(4f, 3f, 8f));
            b.Stairs(V(12f, 0f, 86f), 0f, V(4f, 3f, 8f));
            Rect(b, -24f, 24f, 90f, 140f, 3f, "Gray", 3f);
            b.Box(V(14f, 4.75f, 110f), V(5f, 3.5f, 12f), "AccentRed");
            b.Box(V(14f, 4.25f, 118.5f), V(5f, 2.5f, 3f), "Dark");
            b.Box(V(-12f, 4f, 122f), V(6f, 2f, 3f), "Gray");
            b.Crate(V(-4f, 3f, 104f), 1.5f);
            b.ExplosiveBarrel(V(-7f, 3f, 128f));
            b.Health(V(-20f, 3f, 100f));
            b.Ammo(V(20f, 3f, 98f));
            b.Enemy(EnemyType.Sniper, V(-19f, 6f, 114f), 180f, "dock", 0f);
            b.Enemy(EnemyType.Gunner, V(-6f, 3f, 112f), 180f, "dock", 3f);
            b.Enemy(EnemyType.Gunner, V(6f, 3f, 121f), 180f, "dock", 3f);
            b.Enemy(EnemyType.Shotgunner, V(0f, 3f, 128f), 180f, "dock", 2f);
            b.Enemy(EnemyType.Turret, V(10f, 3f, 133f), 200f, "dock", 0f);
            b.Door(V(0f, 5.5f, 140f), 0f, "ArenaClear", "dock", V(6f, 5f, 0.4f));

            // Exit corridor
            Rect(b, -3f, 3f, 140f, 156f, 3f, "Gray");
            WallZ(b, -3.3f, 140f, 156f, 3f, 6f, "Dark");
            WallZ(b, 3.3f, 140f, 156f, 3f, 6f, "Dark");
            WallX(b, 156.3f, -3.6f, 3.6f, 3f, 6f, "Dark");
            b.Light(V(0f, 7f, 150f), Warm, 12f, 2f);
            b.Finish(V(0f, 3f, 151f), 0f, false);
            return b.Data;
        }

        // ------------------------------------------------------------------ 03 Construction Site

        static LevelData ConstructionSite()
        {
            var b = New(Get("level03"), "Dusk", -15f, "Level", "pistol", "shotgun");

            // Ground and fences
            Rect(b, -30f, 30f, -10f, 76f, 0f, "Gray");
            WallZ(b, -30f, -10f, 76f, 0f, 3f, "AccentYellow", 0.3f);
            WallZ(b, 30f, -10f, 76f, 0f, 3f, "AccentYellow", 0.3f);
            WallX(b, -10f, -30f, 30f, 0f, 3f, "AccentYellow", 0.3f);
            Pit(b, -40f, 50f, 76f, 140f, -8f);
            Rect(b, -40f, 50f, 76f, 140f, -10f, "Dark");

            b.Spawn(V(0f, 0f, -6f), 0f);
            b.Sign(V(0f, 3f, -1f), 0f, "SITE 03 - REACH THE ROOF", 1f);
            b.StartGate(V(0f, 3f, 4f), 0f, V(60f, 6f, 1f));
            b.Weapon("rifle", V(0f, 0f, 10f));
            b.Box(V(-8f, 1f, 14f), V(4f, 2f, 4f), "LightGray");
            b.Box(V(7f, 0.75f, 18f), V(3f, 1.5f, 6f), "LightGray");
            b.Box(V(-16f, 1.5f, 24f), V(2f, 3f, 12f), "Accent");
            b.Crate(V(12f, 0f, 24f), 1.5f);
            b.Barrel(V(-3f, 0f, 28f));
            b.Barrel(V(-2f, 0f, 29f));
            b.Enemy(EnemyType.Gunner, V(-12f, 0f, 32f), 180f, "", 5f);
            b.Enemy(EnemyType.Gunner, V(12f, 0f, 36f), 180f, "", 4f);
            b.Enemy(EnemyType.Charger, V(0f, 0f, 44f), 180f, "", 3f);
            b.Ammo(V(-20f, 0f, 20f));

            // Building 1 skeleton
            foreach (float x in new[] { -12f, 0f, 12f })
            {
                foreach (float z in new[] { 50f, 62f, 74f })
                {
                    if (x == 0f && z == 62f) continue;
                    b.Pillar(V(x, 0f, z), 1f, 24f, "LightGray");
                }
            }
            b.Ramp(V(-8f, 0f, 40f), 0f, V(5f, 6f, 14f), "Gray");
            Rect(b, -12f, 12f, 50f, 74f, 6f, "LightGray", 0.6f);
            b.Stairs(V(16f, 0f, 56f), 0f, V(3f, 6f, 10f));
            Ledge(b, 11.5f, 17.5f, 61f, 72f, 6f);
            b.Checkpoint(V(0f, 6f, 54f), 0f, 1);
            b.Enemy(EnemyType.Gunner, V(5f, 6f, 66f), 180f, "", 3f);
            b.Enemy(EnemyType.Shotgunner, V(-6f, 6f, 70f), 180f, "", 2f);
            b.Crate(V(-2f, 6f, 60f), 1.2f);

            // Floor 2 (half) reached by launch pad
            Rect(b, -12f, 12f, 50f, 64f, 12f, "LightGray", 0.6f);
            b.LaunchPad(V(4f, 6f, 68f), 180f, 19f, 4f);
            b.Enemy(EnemyType.Turret, V(-10f, 12f, 52f), 45f, "", 0f);
            b.Health(V(10f, 12f, 52f));

            // Floor 3 via a ramp
            b.Ramp(V(-8f, 12f, 57f), 180f, V(4f, 6f, 12f), "Gray");
            Rect(b, -6f, 12f, 50f, 64f, 18f, "LightGray", 0.6f);
            b.Checkpoint(V(4f, 18f, 56f), 0f, 2);
            b.Enemy(EnemyType.Gunner, V(9f, 18f, 52f), -90f, "", 2f);
            b.Light(V(0f, 21f, 62f), Warm, 16f, 2f);

            // Crane with a moving load and grapple anchors
            b.Pillar(V(22f, -10f, 40f), 2f, 45f, "AccentYellow");
            b.Box(V(22f, 35.5f, 60f), V(1.5f, 1f, 50f), "AccentYellow");
            b.Box(V(22f, 35.5f, 33f), V(4f, 3f, 4f), "Dark");
            b.MovingPlatform(V(15f, 17.75f, 70f), V(4f, 0.5f, 4f), V(0f, 0f, 26f), 4f, 1f);
            b.GrapplePoint(V(22f, 33f, 70f));
            b.GrapplePoint(V(22f, 33f, 86f));

            // Secret on the crane jib
            b.Secret(V(22f, 37.5f, 80f), V(3f, 3f, 4f));
            b.Health(V(22f, 36f, 79f));
            b.Ammo(V(22f, 36f, 81f));

            // Building 2: deck, stair core and the roof finish
            Rect(b, 4f, 24f, 98f, 120f, 18f, "LightGray", 4f);
            foreach (float x in new[] { 5f, 23f })
            {
                foreach (float z in new[] { 99f, 119f })
                {
                    b.Pillar(V(x, -10f, z), 1.2f, 24f, "Gray");
                }
            }
            b.Checkpoint(V(8f, 18f, 102f), 0f, 3);
            b.Enemy(EnemyType.Gunner, V(19f, 18f, 104f), -90f, "", 2f);
            b.Enemy(EnemyType.Drone, V(8f, 18f, 114f), 180f);
            b.Box(V(14f, 21f, 114f), V(8f, 6f, 8f), "White");
            b.LaunchPad(V(14f, 18f, 107.5f), 0f, 18f, 2f);
            b.Light(V(6f, 21f, 108f), Warm, 14f, 2f);
            b.Pillar(V(10.5f, 24f, 117.5f), 0.4f, 3f, "Accent");
            b.Finish(V(14f, 24f, 114f), 0f, false);
            return b.Data;
        }

        // ------------------------------------------------------------------ 04 Tower

        static LevelData Tower()
        {
            var b = New(Get("level04"), "Night", -20f, "Level", "pistol", "shotgun", "smg");

            // Courtyard and entrance
            Rect(b, -10f, 10f, -40f, -15f, 0f, "Gray");
            b.Spawn(V(0f, 0f, -36f), 0f);
            b.Sign(V(0f, 3.5f, -31f), 0f, "CLIMB THE TOWER", 1f);
            b.StartGate(V(0f, 3f, -27f), 0f, V(20f, 6f, 1f));
            b.Light(V(0f, 5f, -22f), Cold, 16f, 2f);

            // Tower shell
            WallZ(b, -15f, -15f, 15f, -2f, 68f, "Dark");
            WallZ(b, 15f, -15f, 15f, -2f, 68f, "Dark");
            WallX(b, 15f, -15f, 15f, -2f, 68f, "Dark");
            WallXGap(b, -15f, -15f, 15f, -2f, 68f, 0f, 6f, 7f, "Dark");
            b.Door(V(0f, 2.5f, -15f), 0f, "Proximity", "", V(6f, 5f, 0.4f));

            // Ground walkway and coolant pool
            Rect(b, -15f, 15f, -15f, -9f, 0f, "Gray");
            Rect(b, 9f, 15f, -9f, -6f, 0f, "Gray");
            Rect(b, -15f, 15f, -9f, 15f, -2f, "Dark");
            Pit(b, -15f, 15f, -9f, 15f, -0.5f);
            b.Weapon("revolver", V(-10f, 0f, -12f));

            // Central core (top = finish)
            b.Box(V(0f, 29f, 3f), V(8f, 62f, 8f), "LightGray");

            // L1 east (y4)
            b.Stairs(V(12.5f, 0f, -10f), 0f, V(5f, 4f, 6f));
            Ledge(b, 10f, 15f, -7f, 12f, 4f);
            b.Checkpoint(V(12.5f, 4f, 0f), 0f, 1);
            b.Enemy(EnemyType.Gunner, V(12.5f, 4f, 5f), 180f, "", 0f);
            // L1 -> L2
            b.LaunchPad(V(12.5f, 4f, 10.5f), -90f, 15f, 4f);
            // L2 north (y9)
            Ledge(b, -12f, 12f, 10f, 15f, 9f);
            b.Enemy(EnemyType.Drone, V(-8f, 9f, 12.5f), 180f);
            // L2 -> L3 ramp
            b.Ramp(V(-2f, 9f, 12.5f), -90f, V(5f, 6f, 14f), "Gray");
            // L3 west (y15)
            Ledge(b, -15f, -10f, -12f, 15f, 15f);
            b.Checkpoint(V(-12.5f, 15f, 0f), 0f, 2);
            b.Enemy(EnemyType.Turret, V(-12.5f, 15f, -6f), 90f, "", 0f);
            // L3 -> L4
            b.LaunchPad(V(-12.5f, 15f, -9f), 180f, 17f, 3f);
            // L4 south (y21)
            Ledge(b, -15f, 12f, -15f, -10f, 21f);
            b.Health(V(0f, 21f, -12.5f));
            b.Enemy(EnemyType.Drone, V(6f, 21f, -12.5f), 0f);
            // L4 -> L5
            b.LaunchPad(V(11f, 21f, -12.5f), 0f, 17f, 3f);
            // L5 east (y27)
            Ledge(b, 10f, 15f, -12f, 11f, 27f);
            b.Checkpoint(V(12.5f, 27f, -2f), 0f, 3);
            b.Enemy(EnemyType.Turret, V(12.5f, 27f, 7f), -90f, "", 0f);
            // L5 -> L6 elevator
            b.MovingPlatform(V(12.5f, 26.75f, 12.8f), V(4f, 0.5f, 3.6f), V(0f, 6f, 0f), 2.5f, 1.2f);
            // L6 north (y33)
            Ledge(b, -12f, 10.5f, 10f, 15f, 33f);
            b.Enemy(EnemyType.Gunner, V(7.5f, 33f, 13f), 90f, "", 0f);
            // L6 -> L7 stairs
            b.Stairs(V(-2f, 33f, 12.5f), -90f, V(5f, 6f, 14f));
            // L7 west (y39)
            Ledge(b, -15f, -10f, -12f, 15f, 39f);
            b.Checkpoint(V(-12.5f, 39f, 4f), 0f, 4);
            b.Ammo(V(-12.5f, 39f, 10f));
            b.Enemy(EnemyType.Drone, V(-12.5f, 39f, -6f), 0f);
            // L7 -> L8 grapple
            b.GrapplePoint(V(-5f, 50f, -8f));
            b.LaunchPad(V(-12.5f, 39f, -9f), 180f, 17f, 3f);
            // L8 south (y45)
            Ledge(b, -15f, 12f, -15f, -10f, 45f);
            b.Enemy(EnemyType.Gunner, V(0f, 45f, -12.5f), 0f, "", 3f);
            // L8 -> L9
            b.LaunchPad(V(8f, 45f, -12.5f), 90f, 17f, 3f);
            // L9 east (y51)
            Ledge(b, 10f, 15f, -15f, 12f, 51f);
            b.Health(V(12.5f, 51f, -8f));
            b.Enemy(EnemyType.Gunner, V(12.5f, 51f, 8f), 180f, "", 0f);
            // L9 -> core top
            b.LaunchPad(V(12.5f, 51f, 3f), -90f, 22f, 9f);
            b.GrapplePoint(V(0f, 68f, 3f));
            b.Finish(V(0f, 60f, 3f), 0f, false);

            // Secret ledge in the south-west corner
            b.GrapplePoint(V(-8f, 36f, -8f));
            Ledge(b, -14.5f, -10.5f, -14.5f, -10.5f, 30f, "AccentBlue");
            b.Secret(V(-12.5f, 31.5f, -12.5f), V(3.5f, 3f, 3.5f));
            b.Weapon("grenade", V(-12.5f, 30f, -12.5f));

            // Lights
            b.Light(V(13f, 7f, -13f), Cold, 22f, 1.8f);
            b.Light(V(-13f, 13f, 13f), Cold, 22f, 1.8f);
            b.Light(V(-13f, 19f, -13f), Warm, 22f, 1.8f);
            b.Light(V(13f, 31f, 13f), Cold, 22f, 1.8f);
            b.Light(V(-13f, 43f, 13f), Warm, 22f, 1.8f);
            b.Light(V(13f, 55f, -13f), Cold, 22f, 1.8f);
            b.Light(V(0f, 63f, 3f), Warm, 18f, 2.5f, true);
            return b.Data;
        }

        // ------------------------------------------------------------------ 05 Research Complex

        static LevelData ResearchComplex()
        {
            var b = New(Get("level05"), "Day", -20f, "Level", "pistol", "shotgun", "rifle");

            // Lobby
            Rect(b, -12f, 12f, 0f, 24f, 0f, "White");
            WallZ(b, -12f, 0f, 24f, 0f, 8f);
            WallZ(b, 12f, 0f, 24f, 0f, 8f);
            WallX(b, 0f, -12f, 12f, 0f, 8f);
            WallXGap(b, 24f, -12f, 12f, 0f, 8f, 0f, 6f, 5f);
            WallX(b, 24f, -15f, -12f, 0f, 7f);
            WallX(b, 24f, 12f, 15f, 0f, 7f);
            b.Spawn(V(0f, 0f, 4f), 0f);
            b.Sign(V(0f, 4f, 9f), 0f, "AXIOM RESEARCH\\nAUTHORIZED PERSONNEL ONLY", 0.8f);
            b.Glass(V(-6f, 2f, 14f), V(6f, 4f, 0.15f));
            b.Box(V(6f, 0.6f, 14f), V(5f, 1.2f, 1.5f), "AccentGreen");
            b.StartGate(V(0f, 2.5f, 24f), 0f, V(6f, 5f, 1f));

            // Glass corridor with labs on both sides
            Rect(b, -15f, 15f, 24f, 44f, 0f, "White");
            WallZ(b, -15f, 24f, 44f, 0f, 7f);
            WallZGap(b, 15f, 24f, 44f, 0f, 7f, 33f, 6f, 5f);
            b.Glass(V(-3f, 2.5f, 34f), V(0.15f, 5f, 20f));
            b.Glass(V(3f, 2.5f, 34f), V(0.15f, 5f, 20f));
            WallX(b, 44f, -15f, -3f, 0f, 7f);
            WallX(b, 44f, 3f, 15f, 0f, 7f);
            b.Box(V(-9f, 0.5f, 30f), V(6f, 1f, 2f), "LightGray");
            b.Box(V(-9f, 0.5f, 38f), V(6f, 1f, 2f), "LightGray");
            b.Box(V(9f, 0.5f, 30f), V(6f, 1f, 2f), "LightGray");
            b.Enemy(EnemyType.Gunner, V(-9f, 0f, 34f), 90f, "", 2f);
            b.Enemy(EnemyType.Gunner, V(9f, 0f, 40f), -90f, "", 2f);
            b.Light(V(0f, 6f, 34f), Cold, 14f, 1.5f);

            // Secret closet behind a breakable wall in the right lab
            b.BreakableWall(V(15f, 2.5f, 33f), V(0.8f, 5f, 6f), 0f, 80f);
            Rect(b, 15f, 20f, 29f, 37f, 0f, "White");
            WallZ(b, 20f, 29f, 37f, 0f, 6f);
            WallX(b, 29f, 15f, 20f, 0f, 6f);
            WallX(b, 37f, 15f, 20f, 0f, 6f);
            b.Secret(V(17.5f, 2f, 33f), V(4f, 4f, 6f));
            b.Health(V(17.5f, 0f, 31.5f));
            b.Ammo(V(17.5f, 0f, 34.5f));

            // Hazard corridor
            Rect(b, -3f, 3f, 44f, 70f, 0f, "Dark");
            WallZ(b, -3.3f, 44f, 70f, 0f, 7f);
            WallZ(b, 3.3f, 44f, 70f, 0f, 7f);
            b.Checkpoint(V(0f, 0f, 46.5f), 0f, 1);
            b.Hazard(V(0f, 0.1f, 52f), V(6f, 0.2f, 4f), 2f, 1.4f);
            b.Hazard(V(0f, 0.1f, 58f), V(6f, 0.2f, 4f), 2.6f, 1.4f);
            b.Hazard(V(0f, 0.1f, 64f), V(6f, 0.2f, 4f), 3.2f, 1.4f);
            b.Sign(V(0f, 4.5f, 48f), 0f, "WATCH THE FLOOR - YELLOW MEANS DANGER", 0.6f);

            // Test chamber arena with balconies
            Rect(b, -18f, 18f, 70f, 110f, 0f, "White");
            WallZ(b, -18f, 70f, 110f, 0f, 12f);
            WallZ(b, 18f, 70f, 110f, 0f, 12f);
            WallXGap(b, 70f, -18f, 18f, 0f, 12f, 0f, 6.6f, 7f);
            WallXGap(b, 110f, -18f, 18f, 0f, 12f, 0f, 6f, 5f);
            Ledge(b, -18f, -12f, 76f, 106f, 5f);
            Ledge(b, 12f, 18f, 76f, 106f, 5f);
            b.Stairs(V(-15f, 0f, 73f), 0f, V(4f, 5f, 6f));
            b.Stairs(V(15f, 0f, 73f), 0f, V(4f, 5f, 6f));
            b.Weapon("grenade", V(0f, 0f, 74f));
            b.Pillar(V(-6f, 0f, 88f), 2f, 12f, "AccentGreen");
            b.Pillar(V(6f, 0f, 96f), 2f, 12f, "AccentGreen");
            b.Box(V(0f, 1f, 92f), V(4f, 2f, 1.5f), "Gray");
            b.ExplosiveBarrel(V(-9f, 0f, 100f));
            b.ExplosiveBarrel(V(10f, 0f, 84f));
            b.Enemy(EnemyType.Gunner, V(-15f, 5f, 98f), 90f, "lab", 3f);
            b.Enemy(EnemyType.Gunner, V(15f, 5f, 90f), -90f, "lab", 3f);
            b.Enemy(EnemyType.Shotgunner, V(0f, 0f, 102f), 180f, "lab", 3f);
            b.Enemy(EnemyType.Drone, V(-6f, 0f, 106f), 180f, "lab");
            b.Enemy(EnemyType.Drone, V(6f, 0f, 106f), 180f, "lab");
            b.Health(V(-15f, 5f, 104f));
            b.Ammo(V(15f, 5f, 78f));
            b.Door(V(0f, 2.5f, 110f), 0f, "ArenaClear", "lab", V(6f, 5f, 0.4f));
            b.Light(V(0f, 10f, 90f), Cold, 30f, 1.8f);

            // Vent corridor (slide)
            Rect(b, -3f, 3f, 110f, 130f, 0f, "Dark");
            WallZ(b, -3.3f, 110f, 130f, 0f, 6f);
            WallZ(b, 3.3f, 110f, 130f, 0f, 6f);
            b.Checkpoint(V(0f, 0f, 113f), 0f, 2);
            b.Box(V(0f, 3.75f, 121f), V(6f, 4.5f, 6f), "Gray");

            // Sniper gallery
            Rect(b, -10f, 10f, 130f, 196f, 0f, "White");
            WallZ(b, -10f, 130f, 196f, 0f, 10f);
            WallZ(b, 10f, 130f, 196f, 0f, 10f);
            WallX(b, 196f, -10f, 10f, 0f, 10f);
            WallXGap(b, 130f, -10f, 10f, 0f, 10f, 0f, 6f, 6f);
            foreach (float z in new[] { 142f, 156f, 170f })
            {
                b.Pillar(V(-5f, 0f, z), 1.6f, 10f, "LightGray");
                b.Pillar(V(5f, 0f, z + 6f), 1.6f, 10f, "LightGray");
            }
            Rect(b, -10f, 10f, 182f, 196f, 6f, "Gray", 6f);
            b.Ramp(V(-8f, 0f, 176f), 0f, V(4f, 6f, 12f), "Gray");
            b.LaunchPad(V(6.5f, 0f, 179f), 0f, 17f, 4f);
            b.Enemy(EnemyType.Sniper, V(0f, 6f, 190f), 180f, "", 0f);
            b.Enemy(EnemyType.Turret, V(-7f, 6f, 186f), 160f, "", 0f);
            b.Enemy(EnemyType.Turret, V(7f, 6f, 186f), 200f, "", 0f);
            b.Enemy(EnemyType.Gunner, V(2f, 0f, 160f), 180f, "", 3f);
            b.Checkpoint(V(0f, 0f, 134f), 0f, 3);
            b.Health(V(-8f, 0f, 150f));
            b.Light(V(0f, 9f, 150f), Cold, 24f, 1.6f);
            b.Light(V(0f, 9f, 180f), Cold, 24f, 1.6f);
            b.Finish(V(0f, 6f, 192f), 0f, false);
            return b.Data;
        }

        // ------------------------------------------------------------------ 06 Reactor (The Juggernaut)

        static LevelData Reactor()
        {
            var b = New(Get("level06"), "Industrial", -20f, "Boss", "pistol", "shotgun", "smg", "rifle");

            Rect(b, -6f, 6f, -6f, 10f, 0f, "Gray");
            b.Spawn(V(0f, 0f, -2f), 0f);
            b.StartGate(V(0f, 3f, 8f), 0f, V(12f, 6f, 1f));
            b.Sign(V(0f, 3.5f, 3f), 0f, "REACTOR CORE - COOLANT IS LETHAL", 0.8f);

            // Coolant channels
            Rect(b, -30f, 30f, 10f, 70f, -4f, "Dark");
            Pit(b, -30f, 30f, 10f, 70f, -2f);
            Ledge(b, -1.5f, 1.5f, 10f, 30f, 0f);
            b.MovingPlatform(V(0f, -0.25f, 34f), V(4f, 0.5f, 4f), V(0f, 0f, 12f), 4f, 1f);
            Ledge(b, -1.5f, 1.5f, 50f, 70f, 0f);
            b.Hazard(V(0f, 0.1f, 56f), V(3f, 0.2f, 4f), 1.6f, 1.2f);
            b.Hazard(V(0f, 0.1f, 63f), V(3f, 0.2f, 4f), 2.2f, 1.2f);
            WallZ(b, 6f, 28f, 52f, -3f, 12f, "AccentBlue");
            b.Pillar(V(-18f, -4f, 30f), 6f, 30f, "Gray");
            b.Pillar(V(18f, -4f, 55f), 6f, 30f, "Gray");
            b.Light(V(-18f, 6f, 30f), Cold, 20f, 2f, true);
            b.Light(V(18f, 6f, 55f), Cold, 20f, 2f, true);
            b.Enemy(EnemyType.Drone, V(-6f, 0f, 44f), 180f);
            b.Enemy(EnemyType.Turret, V(0f, 0f, 69f), 180f, "", 0f);

            // Secret platform over the coolant
            b.GrapplePoint(V(-12f, 10f, 40f));
            Ledge(b, -22f, -18f, 41f, 45f, 2f, "AccentBlue");
            b.Secret(V(-20f, 3.5f, 43f), V(4f, 3f, 4f));
            b.Weapon("grenade", V(-20f, 2f, 43f));

            // Armory before the arena
            Rect(b, -10f, 10f, 70f, 90f, 0f, "Gray");
            b.Weapon("rocket", V(0f, 0f, 76f));
            b.Sign(V(0f, 3.5f, 79f), 0f, "ROCKETS: AIM AT YOUR FEET AND JUMP TO ROCKET JUMP", 0.55f);
            b.Health(V(-6f, 0f, 84f));
            b.Ammo(V(6f, 0f, 84f));
            b.Checkpoint(V(0f, 0f, 86f), 0f, 1);
            WallZ(b, -10f, 70f, 90f, 0f, 8f, "Dark");
            WallZ(b, 10f, 70f, 90f, 0f, 8f, "Dark");

            // Boss arena
            Rect(b, -32f, 32f, 90f, 154f, 0f, "Gray");
            WallZ(b, -32f, 90f, 154f, 0f, 14f, "Dark");
            WallZ(b, 32f, 90f, 154f, 0f, 14f, "Dark");
            WallXGap(b, 90f, -32f, 32f, 0f, 14f, 0f, 8f, 5f, "Dark");
            WallXGap(b, 154f, -32f, 32f, 0f, 14f, 0f, 6f, 5f, "Dark");
            b.Door(V(0f, 2.5f, 90f), 0f, "BossFight", "", V(8f, 5f, 0.4f));
            b.BreakableWall(V(-14f, 3f, 110f), V(8f, 6f, 1.2f), 0f, 200f);
            b.BreakableWall(V(14f, 3f, 110f), V(8f, 6f, 1.2f), 0f, 200f);
            b.BreakableWall(V(-14f, 3f, 136f), V(8f, 6f, 1.2f), 0f, 200f);
            b.BreakableWall(V(14f, 3f, 136f), V(8f, 6f, 1.2f), 0f, 200f);
            b.Pillar(V(-22f, 0f, 100f), 4f, 10f, "LightGray");
            b.Pillar(V(22f, 0f, 100f), 4f, 10f, "LightGray");
            b.Pillar(V(-22f, 0f, 144f), 4f, 10f, "LightGray");
            b.Pillar(V(22f, 0f, 144f), 4f, 10f, "LightGray");
            Ledge(b, -32f, -24f, 114f, 130f, 4f);
            Ledge(b, 24f, 32f, 114f, 130f, 4f);
            b.LaunchPad(V(-22.5f, 0f, 122f), -90f, 15f, 6f);
            b.LaunchPad(V(22.5f, 0f, 122f), 90f, 15f, 6f);
            b.Health(V(-28f, 4f, 122f));
            b.Health(V(28f, 4f, 122f));
            b.Ammo(V(0f, 0f, 96f));
            b.Ammo(V(0f, 0f, 150f));
            b.ExplosiveBarrel(V(-26f, 0f, 96f));
            b.ExplosiveBarrel(V(26f, 0f, 148f));
            b.Boss(BossType.Juggernaut, V(0f, 0f, 126f), 180f, 30f, 20f);
            b.Light(V(-28f, 10f, 94f), Red, 30f, 2f, true);
            b.Light(V(28f, 10f, 94f), Red, 30f, 2f, true);
            b.Light(V(-28f, 10f, 150f), Red, 30f, 2f, true);
            b.Light(V(28f, 10f, 150f), Red, 30f, 2f, true);

            // Exit
            b.Door(V(0f, 2.5f, 154f), 0f, "BossFight", "", V(6f, 5f, 0.4f));
            Rect(b, -4f, 4f, 154f, 168f, 0f, "Gray");
            WallZ(b, -4.3f, 154f, 168f, 0f, 6f, "Dark");
            WallZ(b, 4.3f, 154f, 168f, 0f, 6f, "Dark");
            WallX(b, 168.3f, -4.6f, 4.6f, 0f, 6f, "Dark");
            b.Finish(V(0f, 0f, 163f), 0f, true);
            return b.Data;
        }

        // ------------------------------------------------------------------ 07 Sky Facility

        static LevelData SkyFacility()
        {
            var b = New(Get("level07"), "Sky", -30f, "Level", "pistol", "shotgun", "rifle", "rocket");

            // Start pad and speed runway
            Rect(b, -8f, 8f, -8f, 8f, 0f, "White", 2f);
            b.Pillar(V(0f, -22f, 0f), 4f, 20f, "LightGray");
            b.Spawn(V(0f, 0f, -4f), 0f);
            b.StartGate(V(0f, 3f, 7f), 0f, V(16f, 6f, 1f));
            Rect(b, -3f, 3f, 8f, 40f, 0f, "LightGray");
            b.SpeedPad(V(0f, 0f, 13f), 0f, 28f, V(4f, 1f, 6f));
            b.Sign(V(0f, 4f, 10f), 0f, "SPEED PAD - KEEP RUNNING AND JUMP THE GAP", 0.6f);
            b.Ramp(V(0f, 0f, 44f), 0f, V(6f, 2.5f, 8f), "Accent");
            b.GrapplePoint(V(0f, 11f, 56f));
            Rect(b, -6f, 6f, 62f, 90f, 1f, "White", 2f);
            b.Pillar(V(0f, -22f, 76f), 4f, 21f, "LightGray");
            b.Checkpoint(V(0f, 1f, 80f), 0f, 1);
            b.Enemy(EnemyType.Gunner, V(3f, 1f, 86f), 180f, "", 2f);

            // Grapple chain (or the slow moving platform)
            b.GrapplePoint(V(0f, 12f, 102f));
            b.GrapplePoint(V(-6f, 14f, 118f));
            b.GrapplePoint(V(4f, 13f, 134f));
            b.MovingPlatform(V(9f, 0.75f, 96f), V(4f, 0.5f, 4f), V(0f, 3f, 52f), 6f, 0.5f);
            Rect(b, -8f, 8f, 150f, 170f, 4f, "White", 2f);
            b.Enemy(EnemyType.Drone, V(4f, 4f, 162f), 180f);
            b.Ammo(V(-5f, 4f, 156f));

            // Secret island
            b.GrapplePoint(V(22f, 16f, 124f));
            Rect(b, 30f, 38f, 120f, 128f, 6f, "AccentBlue", 2f);
            b.Secret(V(34f, 8f, 124f), V(6f, 4f, 6f));
            b.Health(V(33f, 6f, 124f));
            b.Ammo(V(35f, 6f, 124f));

            // Wall jump gauntlet
            b.Wall(V(3.5f, 9f, 182f), V(0.6f, 10f, 14f), "AccentBlue");
            b.Wall(V(-3.5f, 9f, 197f), V(0.6f, 10f, 14f), "AccentBlue");
            b.Wall(V(3.5f, 9f, 212f), V(0.6f, 10f, 14f), "AccentBlue");
            b.GrapplePoint(V(0f, 16f, 205f));
            b.Sign(V(0f, 7f, 168f), 0f, "WALLRUN AND JUMP BETWEEN THE PANELS", 0.6f);
            Rect(b, -8f, 8f, 222f, 250f, 6f, "White", 2f);
            b.Pillar(V(0f, -18f, 236f), 4f, 22f, "LightGray");
            b.Weapon("railgun", V(0f, 6f, 230f));
            b.Checkpoint(V(0f, 6f, 240f), 0f, 2);
            b.Enemy(EnemyType.Drone, V(-4f, 6f, 246f), 180f);

            // Sniper outposts
            Rect(b, -30f, -22f, 280f, 290f, 14f, "LightGray", 2f);
            b.Enemy(EnemyType.Sniper, V(-26f, 14f, 285f), 160f, "", 0f);
            Rect(b, 22f, 30f, 268f, 278f, 10f, "LightGray", 2f);
            b.Enemy(EnemyType.Sniper, V(26f, 10f, 273f), 200f, "", 0f);

            // Launch to the island
            b.LaunchPad(V(0f, 6f, 248f), 0f, 20f, 14f);
            Rect(b, -6f, 6f, 264f, 300f, 8f, "White", 2f);
            b.Checkpoint(V(0f, 8f, 270f), 0f, 3);
            b.Enemy(EnemyType.Gunner, V(-3f, 8f, 281f), 180f, "", 2f);
            b.Enemy(EnemyType.Gunner, V(3f, 8f, 286f), 180f, "", 2f);
            b.Health(V(4f, 8f, 268f));

            // Control tower
            b.LaunchPad(V(0f, 8f, 296.5f), 0f, 21f, 3f);
            Rect(b, -8f, 8f, 300f, 320f, 16f, "White", 8f);
            b.Pillar(V(0f, -14f, 310f), 6f, 22f, "LightGray");
            b.Pillar(V(-7f, 16f, 318f), 1f, 8f, "AccentBlue");
            b.Pillar(V(7f, 16f, 318f), 1f, 8f, "AccentBlue");
            b.Finish(V(0f, 16f, 314f), 0f, false);
            return b.Data;
        }

        // ------------------------------------------------------------------ 08 The Warden

        static LevelData TheWarden()
        {
            var b = New(Get("level08"), "Boss", -20f, "Boss", "pistol", "shotgun", "smg", "rifle", "revolver", "rocket");

            Rect(b, -4f, 4f, -12f, 20f, 0f, "Gray");
            WallZ(b, -4.3f, -12f, 20f, 0f, 7f, "Dark");
            WallZ(b, 4.3f, -12f, 20f, 0f, 7f, "Dark");
            WallX(b, -12.3f, -4.6f, 4.6f, 0f, 7f, "Dark");
            b.Spawn(V(0f, 0f, -8f), 0f);
            b.StartGate(V(0f, 2.5f, -2f), 0f, V(8f, 5f, 1f));
            b.Sign(V(0f, 4f, 4f), 0f, "THE WARDEN AWAITS", 1f);
            b.Health(V(-2f, 0f, 8f));
            b.Ammo(V(2f, 0f, 8f));
            b.Checkpoint(V(0f, 0f, 14f), 0f, 1);
            b.Light(V(0f, 6f, 6f), Red, 14f, 1.5f, true);

            // Arena
            Rect(b, -35f, 35f, 20f, 90f, 0f, "Gray");
            WallZ(b, -35f, 20f, 90f, 0f, 16f, "Dark");
            WallZ(b, 35f, 20f, 90f, 0f, 16f, "Dark");
            WallXGap(b, 20f, -35f, 35f, 0f, 16f, 0f, 8f, 5f, "Dark");
            WallXGap(b, 90f, -35f, 35f, 0f, 16f, 0f, 6f, 5f, "Dark");
            b.Door(V(0f, 2.5f, 20f), 0f, "BossFight", "", V(8f, 5f, 0.4f));

            b.Pillar(V(-14f, 0f, 41f), 3f, 12f, "LightGray");
            b.Pillar(V(14f, 0f, 41f), 3f, 12f, "LightGray");
            b.Pillar(V(-14f, 0f, 69f), 3f, 12f, "LightGray");
            b.Pillar(V(14f, 0f, 69f), 3f, 12f, "LightGray");
            b.Box(V(-24f, 1f, 55f), V(1f, 2f, 10f), "AccentRed");
            b.Box(V(24f, 1f, 55f), V(1f, 2f, 10f), "AccentRed");

            // Corner platforms with ramps
            Rect(b, -35f, -25f, 20f, 30f, 4f, "Gray", 4f);
            Rect(b, 25f, 35f, 20f, 30f, 4f, "Gray", 4f);
            Rect(b, -35f, -25f, 80f, 90f, 4f, "Gray", 4f);
            Rect(b, 25f, 35f, 80f, 90f, 4f, "Gray", 4f);
            b.Ramp(V(-30f, 0f, 34f), 180f, V(6f, 4f, 8f));
            b.Ramp(V(30f, 0f, 34f), 180f, V(6f, 4f, 8f));
            b.Ramp(V(-30f, 0f, 76f), 0f, V(6f, 4f, 8f));
            b.Ramp(V(30f, 0f, 76f), 0f, V(6f, 4f, 8f));
            b.Health(V(-30f, 4f, 25f));
            b.Health(V(30f, 4f, 85f));
            b.Ammo(V(30f, 4f, 25f));
            b.Ammo(V(-30f, 4f, 85f));
            b.Weapon("grenade", V(0f, 0f, 25f));

            b.LaunchPad(V(-30f, 0f, 55f), 90f, 18f, 6f);
            b.LaunchPad(V(30f, 0f, 55f), -90f, 18f, 6f);
            b.GrapplePoint(V(-15f, 18f, 55f));
            b.GrapplePoint(V(15f, 18f, 55f));
            b.GrapplePoint(V(0f, 18f, 35f));
            b.GrapplePoint(V(0f, 18f, 75f));

            b.Boss(BossType.Warden, V(0f, 0f, 62f), 180f, 32f, 22f);
            b.Light(V(-30f, 12f, 25f), Red, 34f, 2f, true);
            b.Light(V(30f, 12f, 25f), Red, 34f, 2f, true);
            b.Light(V(-30f, 12f, 85f), Red, 34f, 2f, true);
            b.Light(V(30f, 12f, 85f), Red, 34f, 2f, true);

            // Exit
            b.Door(V(0f, 2.5f, 90f), 0f, "BossFight", "", V(6f, 5f, 0.4f));
            Rect(b, -4f, 4f, 90f, 104f, 0f, "Gray");
            WallZ(b, -4.3f, 90f, 104f, 0f, 6f, "Dark");
            WallZ(b, 4.3f, 90f, 104f, 0f, 6f, "Dark");
            WallX(b, 104.3f, -4.6f, 4.6f, 0f, 6f, "Dark");
            b.Finish(V(0f, 0f, 99f), 0f, true);
            return b.Data;
        }

        // ------------------------------------------------------------------ 09 The Sentinel

        static LevelData TheSentinel()
        {
            var b = New(Get("level09"), "Night", -20f, "Boss", "pistol", "shotgun", "smg", "rifle", "revolver", "rocket", "railgun");

            Rect(b, -4f, 4f, -20f, 10f, 0f, "Gray");
            WallZ(b, -4.3f, -20f, 10f, 0f, 7f, "Dark");
            WallZ(b, 4.3f, -20f, 10f, 0f, 7f, "Dark");
            WallX(b, -20.3f, -4.6f, 4.6f, 0f, 7f, "Dark");
            b.Spawn(V(0f, 0f, -16f), 0f);
            b.StartGate(V(0f, 2.5f, -10f), 0f, V(8f, 5f, 1f));
            b.Sign(V(0f, 4f, -4f), 0f, "THE SENTINEL IS WATCHING", 1f);
            b.Health(V(-2f, 0f, 0f));
            b.Ammo(V(2f, 0f, 0f));
            b.Checkpoint(V(0f, 0f, 4f), 0f, 1);
            b.Light(V(0f, 6f, -2f), Purple, 14f, 1.5f, true);

            // Arena
            Rect(b, -30f, 30f, 10f, 70f, 0f, "Dark");
            WallZ(b, -30f, 10f, 70f, 0f, 18f, "Dark");
            WallZ(b, 30f, 10f, 70f, 0f, 18f, "Dark");
            WallXGap(b, 10f, -30f, 30f, 0f, 18f, 0f, 8f, 5f, "Dark");
            WallXGap(b, 70f, -30f, 30f, 0f, 18f, 0f, 6f, 5f, "Dark");
            b.Door(V(0f, 2.5f, 10f), 0f, "BossFight", "", V(8f, 5f, 0.4f));

            // Electrified floor grid (triggered by the boss)
            foreach (float x in new[] { -15f, -5f, 5f, 15f })
            {
                foreach (float z in new[] { 25f, 35f, 45f, 55f })
                {
                    b.Hazard(V(x, 0.1f, z), V(6f, 0.2f, 6f), 0f, 2f, 40f);
                }
            }

            // Safe ground: raised side platforms
            Ledge(b, -30f, -23f, 28f, 52f, 4f);
            Ledge(b, 23f, 30f, 28f, 52f, 4f);
            b.LaunchPad(V(-21.5f, 0f, 40f), -90f, 15f, 4f);
            b.LaunchPad(V(21.5f, 0f, 40f), 90f, 15f, 4f);
            b.Stairs(V(-26.5f, 0f, 25f), 0f, V(5f, 4f, 6f));
            b.Stairs(V(26.5f, 0f, 25f), 0f, V(5f, 4f, 6f));
            b.Health(V(-26.5f, 4f, 46f));
            b.Health(V(26.5f, 4f, 34f));
            b.Ammo(V(-26.5f, 4f, 34f));
            b.Ammo(V(26.5f, 4f, 46f));

            // Cover pillars and grapple anchors
            b.Pillar(V(-10f, 0f, 30f), 2.5f, 10f, "LightGray");
            b.Pillar(V(10f, 0f, 30f), 2.5f, 10f, "LightGray");
            b.Pillar(V(-10f, 0f, 50f), 2.5f, 10f, "LightGray");
            b.Pillar(V(10f, 0f, 50f), 2.5f, 10f, "LightGray");
            b.GrapplePoint(V(-18f, 16f, 40f));
            b.GrapplePoint(V(18f, 16f, 40f));
            b.GrapplePoint(V(0f, 16f, 22f));
            b.GrapplePoint(V(0f, 16f, 58f));

            b.Boss(BossType.Sentinel, V(0f, 0f, 40f), 180f, 28f, 20f);
            b.Light(V(-26f, 14f, 14f), Purple, 30f, 2f, true);
            b.Light(V(26f, 14f, 14f), Purple, 30f, 2f, true);
            b.Light(V(-26f, 14f, 66f), Cold, 30f, 2f, true);
            b.Light(V(26f, 14f, 66f), Cold, 30f, 2f, true);

            // Exit
            b.Door(V(0f, 2.5f, 70f), 0f, "BossFight", "", V(6f, 5f, 0.4f));
            Rect(b, -4f, 4f, 70f, 84f, 0f, "Gray");
            WallZ(b, -4.3f, 70f, 84f, 0f, 6f, "Dark");
            WallZ(b, 4.3f, 70f, 84f, 0f, 6f, "Dark");
            WallX(b, 84.3f, -4.6f, 4.6f, 0f, 6f, "Dark");
            b.Finish(V(0f, 0f, 79f), 0f, true);
            return b.Data;
        }

        // ------------------------------------------------------------------ 10 The Core

        static LevelData TheCore()
        {
            var b = New(Get("level10"), "Boss", -20f, "Boss", "pistol", "shotgun", "smg", "rifle", "revolver", "rocket", "grenade", "railgun");

            // Approach
            Rect(b, -4f, 4f, -40f, 0f, 0f, "Gray");
            WallZ(b, -4.3f, -40f, 0f, 0f, 7f, "Dark");
            WallZ(b, 4.3f, -40f, 0f, 0f, 7f, "Dark");
            WallX(b, -40.3f, -4.6f, 4.6f, 0f, 7f, "Dark");
            b.Spawn(V(0f, 0f, -36f), 0f);
            b.StartGate(V(0f, 2.5f, -30f), 0f, V(8f, 5f, 1f));
            b.Sign(V(0f, 4f, -24f), 0f, "THE CORE\\nDESTROY THE HEART OF THE FACILITY", 0.8f);
            b.Enemy(EnemyType.Drone, V(0f, 0f, -16f), 180f);
            b.Health(V(-2f, 0f, -8f));
            b.Ammo(V(2f, 0f, -8f));
            b.Checkpoint(V(0f, 0f, -5f), 0f, 1);

            // Arena shell
            Rect(b, -36f, 36f, 0f, 72f, 0f, "Dark");
            WallZ(b, -36f, 0f, 72f, 0f, 24f, "Dark");
            WallZ(b, 36f, 0f, 72f, 0f, 24f, "Dark");
            WallXGap(b, 0f, -36f, 36f, 0f, 24f, 0f, 8f, 5f, "Dark");
            WallXGap(b, 72f, -36f, 36f, 0f, 24f, 0f, 6f, 5f, "Dark");
            b.Door(V(0f, 2.5f, 0f), 0f, "BossFight", "", V(8f, 5f, 0.4f));

            // Corner towers, wall bridges and launch pads
            Rect(b, -36f, -28f, 0f, 8f, 10f, "Gray", 10f);
            Rect(b, 28f, 36f, 0f, 8f, 10f, "Gray", 10f);
            Rect(b, -36f, -28f, 64f, 72f, 10f, "Gray", 10f);
            Rect(b, 28f, 36f, 64f, 72f, 10f, "Gray", 10f);
            Ledge(b, -36f, -32f, 8f, 64f, 10f);
            Ledge(b, 32f, 36f, 8f, 64f, 10f);
            b.LaunchPad(V(-25.5f, 0f, 4f), -90f, 24f, 3f);
            b.LaunchPad(V(25.5f, 0f, 4f), 90f, 24f, 3f);
            b.LaunchPad(V(-25.5f, 0f, 68f), -90f, 24f, 3f);
            b.LaunchPad(V(25.5f, 0f, 68f), 90f, 24f, 3f);
            b.Health(V(-32f, 10f, 4f));
            b.Health(V(32f, 10f, 68f));
            b.Ammo(V(32f, 10f, 4f));
            b.Ammo(V(-32f, 10f, 68f));
            b.Health(V(-34f, 10f, 36f));
            b.Health(V(34f, 10f, 36f));
            b.GrapplePoint(V(-14f, 20f, 36f));
            b.GrapplePoint(V(14f, 20f, 36f));
            b.GrapplePoint(V(0f, 22f, 14f));
            b.GrapplePoint(V(0f, 22f, 58f));

            // Cover
            b.Box(V(-12f, 1f, 22f), V(2f, 2f, 8f), "LightGray");
            b.Box(V(12f, 1f, 50f), V(2f, 2f, 8f), "LightGray");
            b.Box(V(-11f, 1f, 48f), V(8f, 2f, 2f), "LightGray");
            b.Box(V(11f, 1f, 24f), V(8f, 2f, 2f), "LightGray");

            // Boss sockets
            b.Socket(BossSocketType.TurretMount, V(-16f, 0f, 20f));
            b.Socket(BossSocketType.TurretMount, V(16f, 0f, 20f));
            b.Socket(BossSocketType.TurretMount, V(-16f, 0f, 52f));
            b.Socket(BossSocketType.TurretMount, V(16f, 0f, 52f));
            b.Socket(BossSocketType.EnemySpawn, V(-24f, 0f, 12f));
            b.Socket(BossSocketType.EnemySpawn, V(24f, 0f, 12f));
            b.Socket(BossSocketType.EnemySpawn, V(-24f, 0f, 60f));
            b.Socket(BossSocketType.EnemySpawn, V(24f, 0f, 60f));
            b.Socket(BossSocketType.LaserEmitter, V(-30f, 1f, 36f));
            b.Socket(BossSocketType.LaserEmitter, V(30f, 1f, 36f));
            b.Socket(BossSocketType.WeakPoint, V(-32f, 16f, 4f));
            b.Socket(BossSocketType.WeakPoint, V(32f, 16f, 4f));
            b.Socket(BossSocketType.WeakPoint, V(-32f, 16f, 68f));
            b.Socket(BossSocketType.WeakPoint, V(32f, 16f, 68f));

            b.Boss(BossType.Core, V(0f, 0f, 36f), 0f, 34f, 24f);
            b.Light(V(-30f, 18f, 6f), Red, 36f, 2.2f, true);
            b.Light(V(30f, 18f, 6f), Red, 36f, 2.2f, true);
            b.Light(V(-30f, 18f, 66f), Red, 36f, 2.2f, true);
            b.Light(V(30f, 18f, 66f), Red, 36f, 2.2f, true);

            // Exit
            b.Door(V(0f, 2.5f, 72f), 0f, "BossFight", "", V(6f, 5f, 0.4f));
            Rect(b, -4f, 4f, 72f, 86f, 0f, "Gray");
            WallZ(b, -4.3f, 72f, 86f, 0f, 6f, "Dark");
            WallZ(b, 4.3f, 72f, 86f, 0f, 6f, "Dark");
            WallX(b, 86.3f, -4.6f, 4.6f, 0f, 6f, "Dark");
            b.Finish(V(0f, 0f, 81f), 0f, true);
            return b.Data;
        }
    }
}

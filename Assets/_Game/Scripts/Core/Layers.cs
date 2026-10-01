using UnityEngine;

namespace Momentum
{
    /// <summary>
    /// Central definition of the physics layers and tags used by the game.
    /// Layer indices are fixed so that generated prefabs stay stable between setup runs.
    /// The editor setup tool writes these names into the project's TagManager.
    /// </summary>
    public static class Layers
    {
        public const int Default = 0;
        public const int TransparentFX = 1;
        public const int IgnoreRaycast = 2;
        public const int Water = 4;
        public const int UI = 5;

        public const int Player = 8;
        public const int Enemy = 9;
        public const int Environment = 10;
        public const int Projectile = 11;
        public const int Pickup = 12;
        public const int Grapple = 13;
        public const int Prop = 14;
        public const int ViewModel = 15;
        public const int EditorGizmo = 16;
        public const int Trigger = 17;
        public const int Debris = 18;

        /// <summary>Custom layers created by the setup tool (index, name).</summary>
        public static readonly int[] CustomLayerIndices =
        {
            Player, Enemy, Environment, Projectile, Pickup, Grapple, Prop, ViewModel, EditorGizmo, Trigger, Debris
        };

        public static readonly string[] CustomLayerNames =
        {
            "Player", "Enemy", "Environment", "Projectile", "Pickup", "Grapple", "Prop", "ViewModel", "EditorGizmo", "Trigger", "Debris"
        };

        /// <summary>Custom tags created by the setup tool.</summary>
        public static readonly string[] CustomTags =
        {
            Tags.Enemy, Tags.Boss, Tags.Pickup, Tags.NoWallRun, Tags.Checkpoint, Tags.Hazard
        };

        public static int ToMask(int layer) => 1 << layer;

        /// <summary>Static world geometry the player can stand on and collide with.</summary>
        public static readonly int SolidMask = (1 << Default) | (1 << Environment) | (1 << Grapple) | (1 << Prop);

        /// <summary>Surfaces that are considered ground for the player.</summary>
        public static readonly int GroundMask = SolidMask;

        /// <summary>Surfaces that can be wall-run on.</summary>
        public static readonly int WallRunMask = (1 << Default) | (1 << Environment) | (1 << Grapple);

        /// <summary>What player hitscan weapons can hit.</summary>
        public static readonly int PlayerShootMask = SolidMask | (1 << Enemy) | (1 << Projectile);

        /// <summary>What enemy weapons can hit.</summary>
        public static readonly int EnemyShootMask = SolidMask | (1 << Player);

        /// <summary>What blocks enemy line of sight.</summary>
        public static readonly int SightBlockMask = (1 << Default) | (1 << Environment) | (1 << Grapple) | (1 << Prop);

        /// <summary>Surfaces the grappling hook can attach to.</summary>
        public static readonly int GrappleMask = (1 << Default) | (1 << Environment) | (1 << Grapple);

        /// <summary>Objects that react to explosions.</summary>
        public static readonly int ExplosionMask = (1 << Default) | (1 << Player) | (1 << Enemy) | (1 << Prop) |
                                                   (1 << Debris) | (1 << Projectile) | (1 << Environment) | (1 << Grapple);

        /// <summary>Objects the player can interact with.</summary>
        public static readonly int InteractMask = (1 << Default) | (1 << Environment) | (1 << Pickup) | (1 << Prop) | (1 << Trigger);

        /// <summary>Layers used by the in-game level editor for selection raycasts.</summary>
        public static readonly int EditorSelectMask = ~((1 << ViewModel) | (1 << EditorGizmo) | (1 << UI));

        /// <summary>
        /// Configures the layer collision matrix. Called by the editor setup tool and again at runtime
        /// so the game behaves correctly even if project settings were reset.
        /// </summary>
        public static void ApplyCollisionMatrix()
        {
            for (int a = 0; a < 32; a++)
            {
                for (int b = a; b < 32; b++)
                {
                    Physics.IgnoreLayerCollision(a, b, ShouldIgnore(a, b));
                }
            }
        }

        static bool ShouldIgnore(int a, int b)
        {
            if (a == ViewModel || b == ViewModel) return true;
            if (a == EditorGizmo || b == EditorGizmo) return true;
            if (IsPair(a, b, Pickup, Pickup)) return true;
            if (a == Pickup || b == Pickup)
            {
                int other = a == Pickup ? b : a;
                return other != Player;
            }
            if (a == Trigger || b == Trigger)
            {
                int other = a == Trigger ? b : a;
                return !(other == Player || other == Enemy || other == Prop || other == Debris);
            }
            if (IsPair(a, b, Projectile, Projectile)) return true;
            if (IsPair(a, b, Projectile, Player)) return true;
            if (IsPair(a, b, Projectile, Debris)) return true;
            if (IsPair(a, b, Debris, Player)) return true;
            if (IsPair(a, b, Debris, Enemy)) return true;
            if (IsPair(a, b, Debris, Debris)) return false;
            return false;
        }

        static bool IsPair(int a, int b, int x, int y) => (a == x && b == y) || (a == y && b == x);

        /// <summary>Sets the layer of a GameObject and all of its children.</summary>
        public static void SetLayerRecursively(GameObject go, int layer)
        {
            if (go == null) return;
            go.layer = layer;
            foreach (Transform child in go.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }
    }

    /// <summary>Tag names used by the game. Built-in Unity tags are included for convenience.</summary>
    public static class Tags
    {
        public const string Untagged = "Untagged";
        public const string Player = "Player";
        public const string MainCamera = "MainCamera";
        public const string Respawn = "Respawn";
        public const string Finish = "Finish";
        public const string EditorOnly = "EditorOnly";

        public const string Enemy = "Enemy";
        public const string Boss = "Boss";
        public const string Pickup = "Pickup";
        public const string NoWallRun = "NoWallRun";
        public const string Checkpoint = "Checkpoint";
        public const string Hazard = "Hazard";
    }
}

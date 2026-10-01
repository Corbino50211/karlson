using System.Collections.Generic;
using Momentum.Enemies;
using UnityEngine;

namespace Momentum.Levels
{
    /// <summary>
    /// Tracks enemies grouped into "arenas" by id. When every enemy of an arena is dead the arena is
    /// cleared: GameEvents.ArenaCleared fires and doors bound to that arena open.
    /// </summary>
    public static class ArenaRegistry
    {
        static readonly Dictionary<string, HashSet<EnemyBase>> arenas = new Dictionary<string, HashSet<EnemyBase>>();
        static readonly HashSet<string> cleared = new HashSet<string>();

        public static void Register(string arenaId, EnemyBase enemy)
        {
            if (string.IsNullOrEmpty(arenaId) || enemy == null) return;
            if (!arenas.TryGetValue(arenaId, out var set))
            {
                set = new HashSet<EnemyBase>();
                arenas[arenaId] = set;
            }
            set.Add(enemy);
            cleared.Remove(arenaId);
        }

        public static void NotifyDeath(string arenaId, EnemyBase enemy)
        {
            if (string.IsNullOrEmpty(arenaId)) return;
            if (!arenas.TryGetValue(arenaId, out var set)) return;
            set.Remove(enemy);
            set.RemoveWhere(e => e == null);
            if (set.Count == 0 && cleared.Add(arenaId))
            {
                GameEvents.RaiseArenaCleared(arenaId);
                GameEvents.RaiseNotification("AREA CLEARED", new Color(0.3f, 1f, 0.5f));
            }
        }

        public static int Remaining(string arenaId)
        {
            if (string.IsNullOrEmpty(arenaId) || !arenas.TryGetValue(arenaId, out var set)) return 0;
            set.RemoveWhere(e => e == null);
            return set.Count;
        }

        public static bool IsCleared(string arenaId)
        {
            return !string.IsNullOrEmpty(arenaId) && cleared.Contains(arenaId);
        }

        public static void Clear()
        {
            arenas.Clear();
            cleared.Clear();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Clear();
        }
    }
}

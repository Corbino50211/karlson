using System;
using System.Collections.Generic;
using Momentum.Levels;
using UnityEngine;

namespace Momentum.Bosses
{
    public enum BossSocketType
    {
        TurretMount,
        EnemySpawn,
        WeakPoint,
        LaserEmitter
    }

    /// <summary>
    /// Marker used by arena bosses (The Core) to know where to place turrets, spawn waves, expose weak
    /// points or mount laser emitters. Levels without sockets fall back to points around the arena.
    /// </summary>
    public class BossSocket : MonoBehaviour, ILevelObjectConfigurable
    {
        public static readonly List<BossSocket> All = new List<BossSocket>();

        [SerializeField] BossSocketType socketType = BossSocketType.EnemySpawn;
        [SerializeField] GameObject editorMarker;

        public BossSocketType SocketType => socketType;

        void OnEnable()
        {
            if (!All.Contains(this)) All.Add(this);
        }

        void OnDisable()
        {
            All.Remove(this);
        }

        void Start()
        {
            if (editorMarker != null) editorMarker.SetActive(false);
        }

        public static List<BossSocket> Find(BossSocketType type, Vector3 center, float radius)
        {
            var result = new List<BossSocket>();
            foreach (var s in All)
            {
                if (s == null || s.socketType != type) continue;
                if ((s.transform.position - center).sqrMagnitude <= radius * radius) result.Add(s);
            }
            return result;
        }

        public void ApplyLevelProperties(LevelObjectData data)
        {
            string value = data.GetString("socket", socketType.ToString());
            if (Enum.TryParse(value, true, out BossSocketType parsed)) socketType = parsed;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            All.Clear();
        }
    }
}

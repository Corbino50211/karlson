using System.Collections.Generic;
using Momentum.Audio;
using Momentum.PlayerSystems;
using Momentum.SaveSystem;
using UnityEngine;

namespace Momentum.Levels
{
    /// <summary>
    /// Tracks the active checkpoint (respawn point) and the run's checkpoint splits, compares them with the
    /// best recorded split for that checkpoint and raises GameEvents.CheckpointReached for the HUD.
    /// </summary>
    public class CheckpointManager : MonoBehaviour
    {
        public static CheckpointManager Instance { get; private set; }

        readonly Dictionary<int, float> splitTimes = new Dictionary<int, float>();
        readonly List<Checkpoint> ordered = new List<Checkpoint>();
        string levelId = "";
        bool recordSplits = true;

        public Checkpoint Current { get; private set; }
        public Vector3 RespawnPosition { get; private set; }
        public float RespawnYaw { get; private set; }
        public int CheckpointCount => ordered.Count;
        public int ReachedCount => splitTimes.Count;

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Resets checkpoints for a new run starting at the given spawn.</summary>
        public void Initialize(Vector3 spawnFeetPosition, float spawnYaw, string id, bool saveSplits)
        {
            levelId = id ?? "";
            recordSplits = saveSplits;
            Current = null;
            RespawnPosition = spawnFeetPosition;
            RespawnYaw = spawnYaw;
            splitTimes.Clear();
            RefreshOrder();
            foreach (var cp in ordered) cp.SetActivated(false, false);
        }

        public void RefreshOrder()
        {
            ordered.Clear();
            foreach (var cp in Checkpoint.All)
            {
                if (cp != null) ordered.Add(cp);
            }
            ordered.Sort((a, b) =>
            {
                int c = a.Order.CompareTo(b.Order);
                return c != 0 ? c : a.GetInstanceID().CompareTo(b.GetInstanceID());
            });
        }

        public int IndexOf(Checkpoint cp)
        {
            if (!ordered.Contains(cp)) RefreshOrder();
            return ordered.IndexOf(cp);
        }

        public void Reach(Checkpoint cp, PlayerController player)
        {
            if (cp == null || cp.IsActivated) return;
            if (Current != null && cp.Order < Current.Order) return;

            Current = cp;
            cp.SetActivated(true, true);
            RespawnPosition = cp.SpawnPosition;
            RespawnYaw = cp.SpawnYaw;

            var timer = LevelTimer.Instance;
            float time = timer != null ? timer.CurrentTime : 0f;
            int index = IndexOf(cp);
            splitTimes[index] = time;

            float delta = float.NaN;
            if (recordSplits && timer != null && timer.Running && SaveManager.Instance != null && !string.IsNullOrEmpty(levelId))
            {
                SaveManager.Instance.TryUpdateBestSplit(levelId, index, time, out float previous);
                if (previous > 0f) delta = time - previous;
            }

            if (player != null && player.Health != null) player.Health.HealAndNotify(player.Health.Max);
            AudioManager.Play2D(SoundId.Checkpoint, 0.8f);
            var registry = PrefabRegistry.Instance;
            if (registry != null && registry.checkpointBurst != null) PoolManager.Spawn(registry.checkpointBurst, cp.transform.position + Vector3.up, Quaternion.identity);
            GameEvents.RaiseCheckpointReached(cp, time, delta);
        }

        /// <summary>Split times in checkpoint order (-1 for checkpoints not reached).</summary>
        public List<float> GetSplits()
        {
            var list = new List<float>();
            for (int i = 0; i < ordered.Count; i++)
            {
                list.Add(splitTimes.TryGetValue(i, out float t) ? t : -1f);
            }
            return list;
        }
    }
}

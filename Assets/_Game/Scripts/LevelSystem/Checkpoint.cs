using System.Collections.Generic;
using Momentum.PlayerSystems;
using UnityEngine;

namespace Momentum.Levels
{
    /// <summary>
    /// Trigger volume that saves the player's respawn point and records a time split.
    /// Order defines the split index (checkpoints are sorted by order).
    /// </summary>
    public class Checkpoint : MonoBehaviour, ILevelObjectConfigurable
    {
        public static readonly List<Checkpoint> All = new List<Checkpoint>();

        [SerializeField] int order;
        [Tooltip("Where the player respawns (forward = facing direction). Defaults to this transform.")]
        [SerializeField] Transform spawnPoint;
        [SerializeField] Renderer[] indicatorRenderers = new Renderer[0];
        [SerializeField] Color inactiveColor = new Color(0.9f, 0.9f, 0.9f);
        [SerializeField] Color activeColor = new Color(0.25f, 1f, 0.45f);
        [SerializeField] ParticleSystem activateEffect;

        MaterialPropertyBlock block;

        public int Order => order;
        public bool IsActivated { get; private set; }
        public Vector3 SpawnPosition => spawnPoint != null ? spawnPoint.position : transform.position;
        public float SpawnYaw => (spawnPoint != null ? spawnPoint : transform).eulerAngles.y;

        void OnEnable()
        {
            if (!All.Contains(this)) All.Add(this);
            ApplyColor(IsActivated ? activeColor : inactiveColor);
        }

        void OnDisable()
        {
            All.Remove(this);
        }

        void OnTriggerEnter(Collider other)
        {
            var player = other.GetComponentInParent<PlayerController>();
            if (player == null || player.IsDead) return;
            if (CheckpointManager.Instance != null) CheckpointManager.Instance.Reach(this, player);
        }

        public void SetActivated(bool activated, bool playEffect)
        {
            IsActivated = activated;
            ApplyColor(activated ? activeColor : inactiveColor);
            if (activated && playEffect && activateEffect != null)
            {
                activateEffect.Clear(true);
                activateEffect.Play(true);
            }
        }

        void ApplyColor(Color c)
        {
            if (indicatorRenderers == null) return;
            if (block == null) block = new MaterialPropertyBlock();
            foreach (var r in indicatorRenderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(block);
                block.SetColor("_Color", c);
                block.SetColor("_EmissionColor", c * 1.5f);
                r.SetPropertyBlock(block);
            }
        }

        public void ApplyLevelProperties(LevelObjectData data)
        {
            order = data.GetInt("order", order);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            All.Clear();
        }
    }
}

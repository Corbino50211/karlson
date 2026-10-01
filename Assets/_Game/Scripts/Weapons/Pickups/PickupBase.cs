using Momentum.Audio;
using Momentum.Levels;
using Momentum.PlayerSystems;
using UnityEngine;

namespace Momentum.Weapons
{
    /// <summary>
    /// Base for world pickups: collected by walking into them or pressing interact while looking at them.
    /// Optionally respawn after a delay.
    /// </summary>
    public abstract class PickupBase : MonoBehaviour, IInteractable, ILevelObjectConfigurable
    {
        [SerializeField] protected GameObject visualRoot;
        [SerializeField] protected bool collectOnTouch = true;
        [SerializeField] protected bool respawns = true;
        [SerializeField] protected float respawnTime = 15f;
        [SerializeField] protected SoundId collectSound = SoundId.AmmoPickup;

        bool available = true;
        float respawnAt;

        public bool IsAvailable => available;

        protected virtual void OnEnable()
        {
            available = true;
            SetVisible(true);
        }

        void OnTriggerEnter(Collider other)
        {
            if (!available || !collectOnTouch) return;
            var player = other.GetComponentInParent<PlayerController>();
            if (player != null && !player.IsDead) TryCollect(player);
        }

        public bool TryCollect(PlayerController player)
        {
            if (!available || player == null) return false;
            if (!Apply(player)) return false;
            available = false;
            SetVisible(false);
            AudioManager.Play(collectSound, transform.position, 0.8f);
            var registry = PrefabRegistry.Instance;
            if (registry != null && registry.pickupBurst != null) PoolManager.Spawn(registry.pickupBurst, transform.position + Vector3.up * 0.8f, Quaternion.identity);
            if (respawns) respawnAt = Time.time + respawnTime;
            return true;
        }

        /// <summary>Gives the pickup's contents. Return false if the player could not use it (stays available).</summary>
        protected abstract bool Apply(PlayerController player);

        protected abstract string PromptText { get; }

        protected virtual void Update()
        {
            if (!available && respawns && Time.time >= respawnAt)
            {
                available = true;
                SetVisible(true);
            }
        }

        protected void SetVisible(bool visible)
        {
            if (visualRoot != null && visualRoot.activeSelf != visible) visualRoot.SetActive(visible);
        }

        public string GetInteractPrompt(PlayerController player) => available ? PromptText : null;
        public bool CanInteract(PlayerController player) => available;
        public void Interact(PlayerController player) => TryCollect(player);

        public void ApplyLevelProperties(LevelObjectData data)
        {
            respawns = data.GetBool("respawn", respawns);
            respawnTime = data.GetFloat("respawnTime", respawnTime);
            ApplyCustomProperties(data);
        }

        protected virtual void ApplyCustomProperties(LevelObjectData data) { }
    }
}

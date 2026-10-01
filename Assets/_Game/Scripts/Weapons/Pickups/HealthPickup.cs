using Momentum.Audio;
using Momentum.Levels;
using Momentum.PlayerSystems;
using UnityEngine;

namespace Momentum.Weapons
{
    /// <summary>Restores health. Not consumed when the player is already at full health.</summary>
    public class HealthPickup : PickupBase
    {
        [SerializeField] float amount = 40f;

        void Reset()
        {
            collectSound = SoundId.HealthPickup;
        }

        protected override string PromptText => "Take health";

        protected override bool Apply(PlayerController player)
        {
            var health = player.Health;
            if (health == null || !health.IsAlive || health.Current >= health.Max - 0.5f) return false;
            health.HealAndNotify(amount);
            AudioManager.Play2D(SoundId.HealthPickup, 0.7f);
            GameEvents.RaiseNotification("+" + Mathf.RoundToInt(amount) + " HEALTH", new Color(0.3f, 1f, 0.45f));
            return true;
        }

        protected override void ApplyCustomProperties(LevelObjectData data)
        {
            amount = Mathf.Max(1f, data.GetFloat("amount", amount));
        }
    }
}

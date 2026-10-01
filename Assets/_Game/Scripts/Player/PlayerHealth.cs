using Momentum.Audio;
using UnityEngine;

namespace Momentum.PlayerSystems
{
    /// <summary>
    /// Player health with optional regeneration and post-respawn invulnerability.
    /// Death and respawn flow is coordinated by PlayerController and LevelManager.
    /// </summary>
    public class PlayerHealth : Health
    {
        [SerializeField] float regenDelay = 4f;
        [SerializeField] float regenRate = 15f;
        [SerializeField] float respawnInvulnerability = 1.2f;

        float lastDamageTime = -999f;
        float invulnerableUntil = -999f;

        public bool IsDead => !IsAlive;
        public bool IsTemporarilyInvulnerable => Time.time < invulnerableUntil;

        public override void TakeDamage(DamageInfo info)
        {
            if (Time.time < invulnerableUntil && info.type != DamageType.Fall) return;
            base.TakeDamage(info);
        }

        protected override void OnDamaged(DamageInfo info)
        {
            lastDamageTime = Time.time;
            GameEvents.RaisePlayerDamaged(info);
            GameEvents.RaisePlayerHealthChanged(Current, Max);
            if (IsAlive)
            {
                AudioManager.Play2D(SoundId.PlayerHurt, 0.7f);
                CameraShaker.Shake(Mathf.Clamp01(info.amount / 60f) * 0.5f);
            }
        }

        protected override void OnDeath(DamageInfo info)
        {
            AudioManager.Play2D(SoundId.PlayerDeath);
        }

        void Update()
        {
            if (!IsAlive || regenRate <= 0f) return;
            if (Current < Max && Time.time - lastDamageTime > regenDelay)
            {
                Heal(regenRate * Time.deltaTime);
                GameEvents.RaisePlayerHealthChanged(Current, Max);
            }
        }

        public void OnRespawned()
        {
            ResetHealth();
            invulnerableUntil = Time.time + respawnInvulnerability;
            lastDamageTime = -999f;
            GameEvents.RaisePlayerHealthChanged(Current, Max);
        }

        public void HealAndNotify(float amount)
        {
            Heal(amount);
            GameEvents.RaisePlayerHealthChanged(Current, Max);
        }
    }
}

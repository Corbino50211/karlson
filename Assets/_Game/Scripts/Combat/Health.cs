using System;
using UnityEngine;

namespace Momentum
{
    /// <summary>
    /// Reusable health component. Player, enemies and bosses derive from or use this.
    /// </summary>
    public class Health : MonoBehaviour, IDamageable
    {
        [SerializeField] protected float maxHealth = 100f;
        [SerializeField] protected bool invulnerable;
        [Tooltip("Damage taken is multiplied by this value (per damage type multipliers can be added in subclasses).")]
        [SerializeField] protected float damageMultiplier = 1f;
        [Tooltip("Multiplier applied to explosion damage.")]
        [SerializeField] protected float explosionMultiplier = 1f;

        public float Max => maxHealth;
        public float Current { get; protected set; }
        public float Normalized => maxHealth > 0f ? Current / maxHealth : 0f;
        public bool IsAlive => Current > 0f;
        public bool Invulnerable
        {
            get => invulnerable;
            set => invulnerable = value;
        }

        public DamageInfo LastDamage { get; private set; }

        /// <summary>Raised after damage is applied (info.amount is the final amount).</summary>
        public event Action<DamageInfo> Damaged;
        public event Action<DamageInfo> Died;
        public event Action<float, float> Changed;

        protected virtual void Awake()
        {
            Current = maxHealth;
        }

        public virtual void TakeDamage(DamageInfo info)
        {
            if (!IsAlive || invulnerable || info.amount <= 0f) return;
            float amount = ModifyDamage(info);
            if (amount <= 0f) return;
            info.amount = amount;
            LastDamage = info;
            Current = Mathf.Max(0f, Current - amount);
            OnDamaged(info);
            Damaged?.Invoke(info);
            Changed?.Invoke(Current, maxHealth);
            if (Current <= 0f)
            {
                OnDeath(info);
                Died?.Invoke(info);
            }
        }

        protected virtual float ModifyDamage(DamageInfo info)
        {
            float amount = info.amount * damageMultiplier;
            if (info.type == DamageType.Explosion) amount *= explosionMultiplier;
            return amount;
        }

        protected virtual void OnDamaged(DamageInfo info) { }
        protected virtual void OnDeath(DamageInfo info) { }

        public void Heal(float amount)
        {
            if (!IsAlive || amount <= 0f) return;
            Current = Mathf.Min(maxHealth, Current + amount);
            Changed?.Invoke(Current, maxHealth);
        }

        public void ResetHealth()
        {
            Current = maxHealth;
            Changed?.Invoke(Current, maxHealth);
        }

        public void SetMaxHealth(float value, bool refill)
        {
            maxHealth = Mathf.Max(1f, value);
            if (refill) Current = maxHealth;
            else Current = Mathf.Min(Current, maxHealth);
            Changed?.Invoke(Current, maxHealth);
        }

        /// <summary>Instantly kills regardless of invulnerability.</summary>
        public void Kill(DamageInfo info)
        {
            if (!IsAlive) return;
            info.amount = Current;
            LastDamage = info;
            Current = 0f;
            Damaged?.Invoke(info);
            Changed?.Invoke(Current, maxHealth);
            OnDeath(info);
            Died?.Invoke(info);
        }
    }
}

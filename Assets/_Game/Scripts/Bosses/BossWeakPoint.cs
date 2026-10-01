using System;
using Momentum.Audio;
using UnityEngine;

namespace Momentum.Bosses
{
    /// <summary>Destructible weak point (The Core, stage 4). Has its own health and reports its destruction.</summary>
    [RequireComponent(typeof(Health))]
    public class BossWeakPoint : MonoBehaviour
    {
        [SerializeField] float health = 150f;

        Health hp;
        HitFlash flash;

        public event Action<BossWeakPoint> Destroyed;
        public Health Health => hp;

        void Awake()
        {
            hp = GetComponent<Health>();
            flash = GetComponent<HitFlash>();
        }

        void OnEnable()
        {
            hp.Damaged += OnDamaged;
            hp.Died += OnDied;
        }

        void OnDisable()
        {
            hp.Damaged -= OnDamaged;
            hp.Died -= OnDied;
        }

        void Start()
        {
            hp.SetMaxHealth(health, true);
        }

        public void SetHealth(float value)
        {
            health = value;
            if (hp != null) hp.SetMaxHealth(value, true);
        }

        void OnDamaged(DamageInfo info)
        {
            if (flash != null) flash.Flash();
        }

        void OnDied(DamageInfo info)
        {
            var registry = PrefabRegistry.Instance;
            if (registry != null && registry.smallExplosion != null) PoolManager.Spawn(registry.smallExplosion, transform.position, Quaternion.identity);
            AudioManager.Play(SoundId.Explosion, transform.position, 0.8f);
            Destroyed?.Invoke(this);
            Destroy(gameObject);
        }
    }
}

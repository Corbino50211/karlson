using UnityEngine;

namespace Momentum
{
    /// <summary>
    /// Extra collider (head, weak point...) that forwards damage to the owning Health with a multiplier.
    /// </summary>
    public class Hitbox : MonoBehaviour, IDamageable
    {
        [SerializeField] Health owner;
        [SerializeField] float damageMultiplier = 2f;
        [SerializeField] bool critical = true;

        public Health Owner => owner;
        public bool IsAlive => owner != null && owner.IsAlive;

        void Awake()
        {
            if (owner == null && transform.parent != null) owner = transform.parent.GetComponentInParent<Health>();
        }

        public void Configure(Health health, float multiplier, bool isCritical)
        {
            owner = health;
            damageMultiplier = multiplier;
            critical = isCritical;
        }

        public void TakeDamage(DamageInfo info)
        {
            if (owner == null) return;
            info.amount *= damageMultiplier;
            info.isCritical |= critical;
            owner.TakeDamage(info);
        }
    }
}

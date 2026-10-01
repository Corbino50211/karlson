using UnityEngine;

namespace Momentum.Enemies
{
    /// <summary>Enemy health. Adds a short invulnerability toggle (used by shielded turrets) on top of Health.</summary>
    public class EnemyHealth : Health
    {
        [Tooltip("Multiplier for damage coming from other enemies (friendly fire).")]
        [SerializeField] float friendlyFireMultiplier = 0.25f;

        protected override float ModifyDamage(DamageInfo info)
        {
            float amount = base.ModifyDamage(info);
            if (!info.fromPlayer && info.source != null && info.source.GetComponentInParent<EnemyBase>() != null)
            {
                amount *= friendlyFireMultiplier;
            }
            return amount;
        }
    }
}

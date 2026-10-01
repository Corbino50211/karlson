namespace Momentum.Bosses
{
    /// <summary>Boss health: applies the boss's current damage-taken multiplier (vulnerable windows).</summary>
    public class BossHealth : Health
    {
        public BossBase Owner { get; set; }

        protected override float ModifyDamage(DamageInfo info)
        {
            float amount = base.ModifyDamage(info);
            if (Owner != null) amount *= Owner.DamageTakenMultiplier;
            // Bosses take reduced damage from their own minions.
            if (!info.fromPlayer && info.source != null && info.source.GetComponentInParent<Enemies.EnemyBase>() != null) amount *= 0.1f;
            return amount;
        }

        /// <summary>Damage that ignores invulnerability (scripted damage such as destroyed weak points).</summary>
        public void ForceDamage(DamageInfo info)
        {
            bool wasInvulnerable = Invulnerable;
            Invulnerable = false;
            info.fromPlayer = true;
            TakeDamage(info);
            Invulnerable = wasInvulnerable && IsAlive;
        }
    }
}

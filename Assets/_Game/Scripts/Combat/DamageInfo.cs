using UnityEngine;

namespace Momentum
{
    public enum DamageType
    {
        Generic,
        Bullet,
        Explosion,
        Melee,
        Laser,
        Fall,
        Hazard,
        Crush
    }

    /// <summary>Describes a single instance of damage.</summary>
    public struct DamageInfo
    {
        public float amount;
        public DamageType type;
        public GameObject source;
        public Vector3 point;
        public Vector3 direction;
        public Vector3 normal;
        /// <summary>Velocity change applied to the victim (if it can be knocked back).</summary>
        public float knockback;
        public bool isCritical;
        public bool fromPlayer;

        public DamageInfo(float amount, DamageType type, GameObject source, Vector3 point, Vector3 direction, bool fromPlayer = false)
        {
            this.amount = amount;
            this.type = type;
            this.source = source;
            this.point = point;
            this.direction = direction.sqrMagnitude > 0f ? direction.normalized : Vector3.forward;
            normal = -this.direction;
            knockback = 0f;
            isCritical = false;
            this.fromPlayer = fromPlayer;
        }

        public static DamageInfo Simple(float amount, DamageType type)
        {
            return new DamageInfo(amount, type, null, Vector3.zero, Vector3.down);
        }
    }

    /// <summary>Anything that can receive damage.</summary>
    public interface IDamageable
    {
        bool IsAlive { get; }
        void TakeDamage(DamageInfo info);
    }

    /// <summary>Anything that can be pushed by weapons/explosions (player, enemies).</summary>
    public interface IKnockbackable
    {
        /// <param name="velocityChange">Instant velocity change in world space.</param>
        /// <param name="stunDuration">How long the victim loses control (enemies only).</param>
        void ApplyKnockback(Vector3 velocityChange, float stunDuration);
    }
}

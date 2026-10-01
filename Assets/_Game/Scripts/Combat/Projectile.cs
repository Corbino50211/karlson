using Momentum.Audio;
using UnityEngine;

namespace Momentum
{
    /// <summary>Launch parameters for a swept projectile.</summary>
    public struct ProjectileData
    {
        public Vector3 direction;
        public float speed;
        public float gravity;
        public float lifetime;
        public float damage;
        public float knockback;
        public float stun;
        public float explosionRadius;
        public float explosionDamage;
        public float explosionForce;
        public float selfDamageMultiplier;
        public float playerForceMultiplier;
        public int hitMask;
        public GameObject owner;
        public bool fromPlayer;
        public Transform homingTarget;
        public float homingTurnRate;
        public float homingDelay;
        public bool explodeOnTimeout;
        public DamageType damageType;

        public static ProjectileData Bullet(Vector3 direction, float speed, float damage, GameObject owner, bool fromPlayer)
        {
            return new ProjectileData
            {
                direction = direction,
                speed = speed,
                damage = damage,
                lifetime = 5f,
                owner = owner,
                fromPlayer = fromPlayer,
                hitMask = fromPlayer ? Layers.PlayerShootMask : Layers.EnemyShootMask,
                selfDamageMultiplier = 0.1f,
                playerForceMultiplier = 1f,
                damageType = DamageType.Bullet
            };
        }
    }

    /// <summary>
    /// Pooled, raycast-swept projectile (enemy bullets, rockets, homing missiles, thrown rocks).
    /// Sweeping every frame means fast projectiles never tunnel through thin walls.
    /// If 'shootable' is enabled (and the prefab has a collider), players can shoot it out of the air.
    /// </summary>
    public class Projectile : MonoBehaviour, IPoolable, IDamageable
    {
        static int nextLaunchId;

        [SerializeField] float radius = 0.1f;
        [SerializeField] bool alignToVelocity = true;
        [SerializeField] bool shootable;
        [SerializeField] float shootableHealth = 1f;
        [SerializeField] SoundId impactSound = SoundId.BulletImpact;
        [SerializeField] float impactVolume = 0.4f;
        [SerializeField] GameObject impactEffect;

        ProjectileData data;
        Vector3 velocity;
        float age;
        bool alive;
        bool pendingDetonation;
        float health;
        Transform ownerRoot;
        TrailRenderer[] trails;

        public int LaunchId { get; private set; }
        public bool IsAlive => alive;
        public Vector3 Velocity => velocity;
        public bool FromPlayer => data.fromPlayer;

        void Awake()
        {
            trails = GetComponentsInChildren<TrailRenderer>(true);
        }

        public void Launch(ProjectileData launchData)
        {
            data = launchData;
            if (data.lifetime <= 0f) data.lifetime = 5f;
            if (data.hitMask == 0) data.hitMask = data.fromPlayer ? Layers.PlayerShootMask : Layers.EnemyShootMask;
            velocity = data.direction.normalized * data.speed;
            age = 0f;
            alive = true;
            pendingDetonation = false;
            health = shootableHealth;
            ownerRoot = data.owner != null ? data.owner.transform : null;
            LaunchId = ++nextLaunchId;
            if (velocity.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(velocity);
            if (trails != null)
            {
                foreach (var t in trails)
                {
                    if (t != null) t.Clear();
                }
            }
        }

        public void OnSpawned()
        {
            alive = false;
            pendingDetonation = false;
        }

        public void OnDespawned()
        {
            alive = false;
        }

        void Update()
        {
            if (!alive) return;
            if (pendingDetonation)
            {
                Detonate();
                return;
            }

            float dt = Time.deltaTime;
            age += dt;
            if (age >= data.lifetime)
            {
                if (data.explodeOnTimeout || data.explosionRadius > 0f) Detonate();
                else Despawn();
                return;
            }

            if (data.homingTarget != null && data.homingTurnRate > 0f && age >= data.homingDelay)
            {
                Vector3 to = data.homingTarget.position - transform.position;
                if (to.sqrMagnitude > 0.01f)
                {
                    Vector3 desired = to.normalized * velocity.magnitude;
                    velocity = Vector3.RotateTowards(velocity, desired, data.homingTurnRate * Mathf.Deg2Rad * dt, 0f);
                }
            }

            velocity += Vector3.down * data.gravity * dt;
            Vector3 step = velocity * dt;
            float dist = step.magnitude;
            if (dist > 0.0001f &&
                PhysicsUtil.SweepClosest(transform.position, step / dist, dist, radius, data.hitMask, ownerRoot, transform, out RaycastHit hit))
            {
                transform.position = hit.point;
                OnHit(hit);
                return;
            }

            transform.position += step;
            if (alignToVelocity && velocity.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(velocity);
        }

        void OnHit(RaycastHit hit)
        {
            if (data.explosionRadius > 0f)
            {
                Explode(hit.point + hit.normal * 0.15f);
                return;
            }

            Vector3 dir = velocity.sqrMagnitude > 0.001f ? velocity.normalized : transform.forward;
            var damageable = hit.collider.GetComponentInParent<IDamageable>();
            if (damageable != null && damageable.IsAlive && data.damage > 0f)
            {
                var info = new DamageInfo(data.damage, data.damageType, data.owner, hit.point, dir, data.fromPlayer)
                {
                    normal = hit.normal,
                    knockback = data.knockback
                };
                if (damageable is Hitbox) info.isCritical = true;
                damageable.TakeDamage(info);
                if (data.fromPlayer) GameEvents.RaiseHitConfirmed(!damageable.IsAlive, info.isCritical);
            }

            var rb = hit.rigidbody;
            if (rb != null && data.knockback > 0f)
            {
                var knock = rb.GetComponent<IKnockbackable>();
                if (knock != null) knock.ApplyKnockback(dir * data.knockback, data.stun);
                else if (!rb.isKinematic) rb.AddForceAtPosition(dir * data.knockback, hit.point, ForceMode.Impulse);
            }

            var registry = PrefabRegistry.Instance;
            var fx = impactEffect != null ? impactEffect : (registry != null ? (damageable != null ? registry.impactEnemy : registry.impactEnvironment) : null);
            if (fx != null) PoolManager.Spawn(fx, hit.point + hit.normal * 0.02f, Quaternion.LookRotation(hit.normal));
            if (impactSound != SoundId.None) AudioManager.Play(impactSound, hit.point, impactVolume);
            Despawn();
        }

        /// <summary>Explodes at the current position (remote detonation, timeouts, being shot).</summary>
        public void Detonate()
        {
            if (!alive) return;
            if (data.explosionRadius > 0f) Explode(transform.position);
            else Despawn();
        }

        void Explode(Vector3 position)
        {
            alive = false;
            var p = ExplosionParams.Create(position, data.explosionRadius, data.explosionDamage, data.explosionForce, data.owner, data.fromPlayer);
            p.selfDamageMultiplier = data.selfDamageMultiplier;
            p.playerForceMultiplier = data.playerForceMultiplier;
            // Enemy/boss explosives never hurt their owner (player rockets must still push the player).
            if (!data.fromPlayer) p.ignoreRoot = ownerRoot;
            Explosions.Explode(p);
            Despawn();
        }

        void Despawn()
        {
            alive = false;
            PoolManager.Despawn(gameObject);
        }

        // Shootable projectiles (boss missiles) can be destroyed by player fire.
        public void TakeDamage(DamageInfo info)
        {
            if (!alive || !shootable || !info.fromPlayer) return;
            health -= info.amount;
            if (health <= 0f)
            {
                pendingDetonation = true;
                if (info.fromPlayer) GameEvents.RaiseHitConfirmed(true, false);
            }
        }
    }
}

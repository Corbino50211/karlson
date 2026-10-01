using Momentum.Audio;
using UnityEngine;

namespace Momentum
{
    /// <summary>
    /// Bouncing physics grenade with a fuse. Explodes on direct enemy contact, when the fuse runs out or
    /// when remotely detonated with the grenade launcher's alt-fire.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Grenade : MonoBehaviour, IPoolable
    {
        static int nextLaunchId;

        Rigidbody rb;
        float fuse;
        float age;
        bool alive;
        float radius;
        float damage;
        float force;
        float selfDamage;
        float playerForce;
        float directDamage;
        GameObject owner;
        float lastBounceSound;

        public int LaunchId { get; private set; }
        public bool IsAlive => alive;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        public void Launch(Vector3 velocity, float fuseTime, float explosionRadius, float explosionDamage, float explosionForce,
            float selfDamageMultiplier, float playerForceMultiplier, float directHitDamage, GameObject ownerObject)
        {
            fuse = fuseTime;
            radius = explosionRadius;
            damage = explosionDamage;
            force = explosionForce;
            selfDamage = selfDamageMultiplier;
            playerForce = playerForceMultiplier;
            directDamage = directHitDamage;
            owner = ownerObject;
            age = 0f;
            alive = true;
            LaunchId = ++nextLaunchId;
            rb.isKinematic = false;
            rb.velocity = velocity;
            rb.angularVelocity = Random.insideUnitSphere * 10f;
        }

        public void OnSpawned()
        {
            alive = false;
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        public void OnDespawned()
        {
            alive = false;
        }

        void Update()
        {
            if (!alive) return;
            age += Time.deltaTime;
            if (age >= fuse) Detonate();
        }

        void OnCollisionEnter(Collision collision)
        {
            if (!alive) return;
            if (collision.collider.gameObject.layer == Layers.Enemy)
            {
                var damageable = collision.collider.GetComponentInParent<IDamageable>();
                if (damageable != null && directDamage > 0f)
                {
                    var info = new DamageInfo(directDamage, DamageType.Bullet, owner, transform.position, rb.velocity, true);
                    damageable.TakeDamage(info);
                }
                Detonate();
                return;
            }
            if (Time.time - lastBounceSound > 0.15f && collision.relativeVelocity.magnitude > 3f)
            {
                lastBounceSound = Time.time;
                AudioManager.Play(SoundId.BulletImpact, transform.position, 0.25f, 0.6f);
            }
        }

        public void Detonate()
        {
            if (!alive) return;
            alive = false;
            var p = ExplosionParams.Create(transform.position, radius, damage, force, owner, true);
            p.selfDamageMultiplier = selfDamage;
            p.playerForceMultiplier = playerForce;
            Explosions.Explode(p);
            PoolManager.Despawn(gameObject);
        }
    }
}

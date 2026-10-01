using System.Collections.Generic;
using Momentum.Audio;
using Momentum.PlayerSystems;
using UnityEngine;

namespace Momentum
{
    /// <summary>Parameters for a single explosion.</summary>
    public struct ExplosionParams
    {
        public Vector3 position;
        public float radius;
        public float damage;
        /// <summary>Velocity change (m/s) applied at the center of the blast. Falls off with distance.</summary>
        public float force;
        public float upwardsBias;
        public GameObject source;
        public bool fromPlayer;
        /// <summary>Multiplier for damage dealt to the player by their own explosions.</summary>
        public float selfDamageMultiplier;
        /// <summary>Multiplier for knockback applied to the player (rocket jump strength).</summary>
        public float playerForceMultiplier;
        public float enemyStunDuration;
        public GameObject effectPrefab;
        public float effectScale;
        public float cameraShake;
        public SoundId sound;
        /// <summary>Colliders under this transform are not affected (e.g. a boss's own slam).</summary>
        public Transform ignoreRoot;

        public static ExplosionParams Create(Vector3 position, float radius, float damage, float force, GameObject source, bool fromPlayer)
        {
            return new ExplosionParams
            {
                position = position,
                radius = radius,
                damage = damage,
                force = force,
                upwardsBias = 0.35f,
                source = source,
                fromPlayer = fromPlayer,
                selfDamageMultiplier = 0.1f,
                playerForceMultiplier = 1f,
                enemyStunDuration = 0.6f,
                effectPrefab = null,
                effectScale = Mathf.Max(0.3f, radius / 5f),
                cameraShake = 0.6f,
                sound = SoundId.Explosion,
                ignoreRoot = null
            };
        }
    }

    /// <summary>
    /// Explosion resolution: damage with falloff, knockback for the player (rocket jumping), enemies and
    /// physics props, camera shake, sound and VFX.
    /// </summary>
    public static class Explosions
    {

        public static void Explode(ExplosionParams p)
        {
            var registry = PrefabRegistry.Instance;
            var effect = p.effectPrefab != null ? p.effectPrefab : (registry != null ? registry.explosion : null);
            if (effect != null)
            {
                var fx = PoolManager.Spawn(effect, p.position, Quaternion.identity);
                if (fx != null) fx.transform.localScale = Vector3.one * p.effectScale;
            }
            if (p.sound != SoundId.None) AudioManager.Play(p.sound, p.position, Mathf.Clamp(p.radius / 5f, 0.5f, 1.2f));
            if (p.cameraShake > 0f) CameraShaker.ShakeAt(p.position, p.cameraShake, p.radius * 4f);
            GameEvents.RaiseNoise(p.position, p.radius * 6f);

            // Copy results: damage handlers can trigger nested explosions that reuse the shared buffer.
            int count = PhysicsUtil.OverlapSphere(p.position, p.radius, Layers.ExplosionMask, QueryTriggerInteraction.Ignore);
            var local = new Collider[count];
            System.Array.Copy(PhysicsUtil.OverlapBuffer, local, count);
            var processed = new HashSet<Object>();

            foreach (var c in local)
            {
                if (c == null) continue;
                if (p.ignoreRoot != null && c.transform.IsChildOf(p.ignoreRoot)) continue;
                var rb = c.attachedRigidbody;
                IDamageable damageable;
                Object key;
                if (rb != null)
                {
                    key = rb;
                    damageable = rb.GetComponent<IDamageable>();
                    if (damageable == null) damageable = c.GetComponentInParent<IDamageable>();
                }
                else
                {
                    damageable = c.GetComponentInParent<IDamageable>();
                    key = damageable as Component;
                    if (key == null) key = c;
                }
                if (damageable is Hitbox hb) damageable = hb.Owner;
                if (!processed.Add(key)) continue;

                Vector3 closest = c.bounds.ClosestPoint(p.position);
                float dist = Vector3.Distance(closest, p.position);
                float falloff = 1f - Mathf.Clamp01(dist / Mathf.Max(0.01f, p.radius));
                Vector3 center = rb != null ? rb.worldCenterOfMass : c.bounds.center;
                Vector3 dir = center - p.position;
                if (dir.sqrMagnitude < 0.0001f) dir = Vector3.up;
                dir.Normalize();

                bool isPlayer = c.gameObject.layer == Layers.Player;
                var knock = rb != null ? rb.GetComponent<IKnockbackable>() : null;

                // Damage
                if (damageable != null && damageable.IsAlive && p.damage > 0f)
                {
                    float dmg = p.damage * Mathf.Lerp(0.3f, 1f, falloff);
                    if (isPlayer && p.fromPlayer) dmg *= p.selfDamageMultiplier;
                    if (dmg > 0.01f)
                    {
                        var info = new DamageInfo(dmg, DamageType.Explosion, p.source, closest, dir, p.fromPlayer);
                        damageable.TakeDamage(info);
                    }
                }

                // Knockback
                if (p.force > 0f)
                {
                    Vector3 pushDir = (dir + Vector3.up * p.upwardsBias).normalized;
                    if (knock != null)
                    {
                        float strength = p.force * Mathf.Lerp(0.35f, 1f, falloff);
                        if (isPlayer) strength *= p.playerForceMultiplier;
                        else strength *= 0.85f;
                        knock.ApplyKnockback(pushDir * strength, isPlayer ? 0f : p.enemyStunDuration * falloff);
                    }
                    else if (rb != null && !rb.isKinematic)
                    {
                        rb.AddExplosionForce(p.force * 1.2f, p.position, p.radius * 1.2f, 0.6f, ForceMode.VelocityChange);
                    }
                }
            }
        }
    }
}

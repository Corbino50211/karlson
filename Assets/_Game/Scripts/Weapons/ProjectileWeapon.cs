using System.Collections.Generic;
using UnityEngine;

namespace Momentum.Weapons
{
    /// <summary>
    /// Fires physical projectiles: rockets (straight, explode on impact) or grenades (bouncing, fused).
    /// Explosions push the player, so rocket jumping and grenade jumping preserve momentum.
    /// Alt-fire remotely detonates everything this weapon has in flight.
    /// </summary>
    public class ProjectileWeapon : WeaponBase
    {
        struct Tracked<T>
        {
            public T item;
            public int id;
        }

        [SerializeField] bool grenadeLauncher;
        [SerializeField] float spawnForwardOffset = 0.75f;
        [SerializeField] float spawnDownOffset = 0.15f;
        [SerializeField] float grenadeUpwardBoost = 2.5f;
        [SerializeField] float grenadeInheritVelocity = 0.35f;

        readonly List<Tracked<Projectile>> rockets = new List<Tracked<Projectile>>();
        readonly List<Tracked<Grenade>> grenades = new List<Tracked<Grenade>>();

        protected override float OnFire(Vector3 origin, Vector3 direction, float spread, float multiplier)
        {
            Prune();
            Vector3 dir = MathUtil.RandomInCone(direction, spread);
            Vector3 spawn = origin + dir * spawnForwardOffset - ctx.aim.up * spawnDownOffset;
            if (definition.projectilePrefab == null)
            {
                Debug.LogWarning($"[Momentum] {definition.displayName} has no projectile prefab assigned.");
                return float.PositiveInfinity;
            }

            // Point-blank: if a surface is closer than the spawn point, explode right there (reliable rocket jumps).
            if (!grenadeLauncher &&
                PhysicsUtil.SweepClosest(origin, dir, spawnForwardOffset + 0.25f, 0f, Layers.PlayerShootMask, ctx.ownerRoot, out RaycastHit close))
            {
                var p = ExplosionParams.Create(close.point + close.normal * 0.15f, definition.explosionRadius, definition.explosionDamage, definition.explosionForce, ctx.owner, true);
                p.selfDamageMultiplier = definition.selfDamageMultiplier;
                p.playerForceMultiplier = definition.playerExplosionForceMultiplier;
                Explosions.Explode(p);
                return close.distance;
            }

            if (grenadeLauncher)
            {
                var go = PoolManager.Spawn(definition.projectilePrefab, spawn, Quaternion.LookRotation(dir));
                var grenade = go != null ? go.GetComponent<Grenade>() : null;
                if (grenade != null)
                {
                    Vector3 inherit = ctx.movement != null ? ctx.movement.Velocity * grenadeInheritVelocity : Vector3.zero;
                    grenade.Launch(dir * definition.projectileSpeed + Vector3.up * grenadeUpwardBoost + inherit, definition.grenadeFuse,
                        definition.explosionRadius, definition.explosionDamage, definition.explosionForce,
                        definition.selfDamageMultiplier, definition.playerExplosionForceMultiplier, definition.damage, ctx.owner);
                    grenades.Add(new Tracked<Grenade> { item = grenade, id = grenade.LaunchId });
                }
            }
            else
            {
                var go = PoolManager.Spawn(definition.projectilePrefab, spawn, Quaternion.LookRotation(dir));
                var rocket = go != null ? go.GetComponent<Projectile>() : null;
                if (rocket != null)
                {
                    var data = ProjectileData.Bullet(dir, definition.projectileSpeed, definition.damage, ctx.owner, true);
                    data.gravity = definition.projectileGravity;
                    data.lifetime = definition.projectileLifetime;
                    data.explosionRadius = definition.explosionRadius;
                    data.explosionDamage = definition.explosionDamage;
                    data.explosionForce = definition.explosionForce;
                    data.selfDamageMultiplier = definition.selfDamageMultiplier;
                    data.playerForceMultiplier = definition.playerExplosionForceMultiplier;
                    data.explodeOnTimeout = true;
                    data.damageType = DamageType.Explosion;
                    rocket.Launch(data);
                    rockets.Add(new Tracked<Projectile> { item = rocket, id = rocket.LaunchId });
                }
            }
            return float.PositiveInfinity;
        }

        protected override void OnAltFirePressed()
        {
            if (definition.altFire != AltFireMode.Detonate) return;
            Prune();
            var rocketCopy = rockets.ToArray();
            var grenadeCopy = grenades.ToArray();
            rockets.Clear();
            grenades.Clear();
            foreach (var r in rocketCopy)
            {
                if (r.item != null && r.item.IsAlive && r.item.LaunchId == r.id) r.item.Detonate();
            }
            foreach (var g in grenadeCopy)
            {
                if (g.item != null && g.item.IsAlive && g.item.LaunchId == g.id) g.item.Detonate();
            }
        }

        void Prune()
        {
            rockets.RemoveAll(r => r.item == null || !r.item.IsAlive || r.item.LaunchId != r.id);
            grenades.RemoveAll(g => g.item == null || !g.item.IsAlive || g.item.LaunchId != g.id);
        }
    }
}

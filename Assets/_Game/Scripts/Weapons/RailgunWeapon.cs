using System.Collections.Generic;
using UnityEngine;

namespace Momentum.Weapons
{
    /// <summary>
    /// High-damage precision weapon: instant ray that can penetrate several enemies, leaves a bright trail,
    /// has heavy recoil and a small self-knockback.
    /// </summary>
    public class RailgunWeapon : WeaponBase
    {
        readonly HashSet<Object> damagedThisShot = new HashSet<Object>();

        protected override float OnFire(Vector3 origin, Vector3 direction, float spread, float multiplier)
        {
            Vector3 dir = MathUtil.RandomInCone(direction, spread);
            var ray = new Ray(origin, dir);
            int count = PhysicsUtil.RaycastAllSorted(ray, definition.range, Layers.PlayerShootMask, out RaycastHit[] shared);
            // Copy: damage handlers may run other physics queries that reuse the shared buffer.
            var hits = new RaycastHit[count];
            System.Array.Copy(shared, hits, count);
            Vector3 end = origin + dir * definition.range;
            float firstSurface = float.PositiveInfinity;
            int pierced = 0;
            damagedThisShot.Clear();

            for (int i = 0; i < count; i++)
            {
                var hit = hits[i];
                if (hit.collider == null) continue;
                if (ctx.ownerRoot != null && hit.collider.transform.IsChildOf(ctx.ownerRoot)) continue;

                var damageable = hit.collider.GetComponentInParent<IDamageable>();
                if (damageable != null)
                {
                    Object key = damageable is Hitbox hb && hb.Owner != null ? (Object)hb.Owner : damageable as Component;
                    if (key != null && !damagedThisShot.Add(key)) continue;
                    ProcessHit(hit, dir, definition.damage, definition.targetKnockback);
                    pierced++;
                    if (pierced > definition.penetration)
                    {
                        end = hit.point;
                        break;
                    }
                }
                else
                {
                    ProcessHit(hit, dir, definition.damage, definition.targetKnockback);
                    end = hit.point;
                    firstSurface = Mathf.Min(firstSurface, hit.distance);
                    break;
                }
            }

            var registry = PrefabRegistry.Instance;
            if (registry != null && registry.railTrail != null)
            {
                var go = PoolManager.Spawn(registry.railTrail, MuzzlePosition, Quaternion.identity);
                var tracer = go != null ? go.GetComponent<Tracer>() : null;
                if (tracer != null) tracer.Play(MuzzlePosition, end, definition.tracerColor, definition.trailWidth, true);
            }
            return firstSurface;
        }
    }
}

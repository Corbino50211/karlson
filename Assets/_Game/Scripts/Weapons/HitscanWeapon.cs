using UnityEngine;

namespace Momentum.Weapons
{
    /// <summary>
    /// Instant raycast weapon (pistol, SMG, assault rifle, revolver). Supports multiple pellets per shot.
    /// </summary>
    public class HitscanWeapon : WeaponBase
    {
        protected override float OnFire(Vector3 origin, Vector3 direction, float spread, float multiplier)
        {
            int pellets = Mathf.Max(1, Mathf.RoundToInt(definition.pelletsPerShot * multiplier));
            float nearestSurface = float.PositiveInfinity;
            for (int i = 0; i < pellets; i++)
            {
                Vector3 dir = MathUtil.RandomInCone(direction, spread);
                float d = FireRay(origin, dir, definition.damage, definition.targetKnockback);
                if (d < nearestSurface) nearestSurface = d;
            }
            return nearestSurface;
        }

        /// <summary>Fires one ray, applies the hit and spawns a tracer. Returns hit distance or infinity.</summary>
        protected float FireRay(Vector3 origin, Vector3 dir, float damage, float knockback)
        {
            Vector3 end;
            float distance = float.PositiveInfinity;
            if (PhysicsUtil.SweepClosest(origin, dir, definition.range, 0f, Layers.PlayerShootMask, ctx.ownerRoot, out RaycastHit hit))
            {
                ProcessHit(hit, dir, damage, knockback);
                end = hit.point;
                distance = hit.distance;
            }
            else
            {
                end = origin + dir * definition.range;
            }
            if (Random.value <= definition.tracerChance) SpawnTracer(MuzzlePosition, end);
            return distance;
        }
    }
}

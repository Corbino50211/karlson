using UnityEngine;

namespace Momentum.Weapons
{
    /// <summary>
    /// Pellet shotgun with heavy self-knockback. Shoot the ground to boost upward, shoot walls to boost
    /// sideways, shoot mid-air to extend jumps. Alt-fire fires both barrels for a much bigger boost.
    /// </summary>
    public class ShotgunWeapon : HitscanWeapon
    {
        [SerializeField] float doubleShotMultiplier = 1.8f;

        protected override void OnAltFirePressed()
        {
            if (!CanFireNow) return;
            if (IsReloading)
            {
                if (AmmoInMagazine >= 2) CancelReload();
                else return;
            }
            if (AmmoInMagazine >= 2)
            {
                FireShot(doubleShotMultiplier, 2, CurrentSpread() * 1.25f);
            }
            else
            {
                TryFire();
            }
        }
    }
}

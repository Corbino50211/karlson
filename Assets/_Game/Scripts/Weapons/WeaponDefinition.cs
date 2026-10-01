using Momentum.Audio;
using UnityEngine;

namespace Momentum.Weapons
{
    public enum WeaponFireMode
    {
        SemiAuto,
        FullAuto
    }

    public enum AltFireMode
    {
        None,
        /// <summary>Hold to zoom (reduced FOV and spread).</summary>
        Zoom,
        /// <summary>Fire two shells at once (shotgun).</summary>
        DoubleShot,
        /// <summary>Rapidly fire the remaining rounds (revolver).</summary>
        Fan,
        /// <summary>Remotely detonate fired rockets/grenades.</summary>
        Detonate
    }

    /// <summary>
    /// Data for one weapon. Behaviour comes from the WeaponBase subclass on the view-model prefab
    /// (HitscanWeapon, ShotgunWeapon, ProjectileWeapon, RailgunWeapon); everything tunable lives here.
    /// </summary>
    [CreateAssetMenu(menuName = "Momentum/Weapon Definition", fileName = "Weapon")]
    public class WeaponDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string id = "pistol";
        public string displayName = "Pistol";
        [Range(1, 9)] public int slot = 1;
        public Color accentColor = new Color(1f, 0.45f, 0.1f);
        public SoundId fireSound = SoundId.Pistol;

        [Header("Prefabs (generated)")]
        public GameObject viewModelPrefab;
        public GameObject worldModelPrefab;
        public GameObject projectilePrefab;

        [Header("Firing")]
        public WeaponFireMode fireMode = WeaponFireMode.SemiAuto;
        public float damage = 20f;
        [Tooltip("Shots per second.")]
        public float fireRate = 5f;
        public int pelletsPerShot = 1;
        [Tooltip("Hip-fire spread cone half-angle in degrees.")]
        public float spread = 0.8f;
        [Tooltip("Spread while zoomed.")]
        public float aimSpread = 0.2f;
        public float range = 250f;
        public int ammoPerShot = 1;
        [Range(0f, 1f)] public float tracerChance = 1f;
        public Color tracerColor = new Color(1f, 0.85f, 0.5f);

        [Header("Ammo")]
        public int magazineSize = 12;
        public int maxReserve = 96;
        public int startingReserve = 48;
        public bool infiniteReserve;
        public float reloadTime = 1.2f;
        [Tooltip("Ammo granted when walking over this weapon again / from ammo pickups (fraction of max for ammo boxes).")]
        public int pickupAmmo = 24;

        [Header("Recoil & Feel")]
        [Tooltip("Camera kick up in degrees per shot.")]
        public float recoilPitch = 1.5f;
        [Tooltip("Random horizontal camera kick in degrees.")]
        public float recoilYaw = 0.4f;
        [Tooltip("View-model kick back in meters.")]
        public float viewKick = 0.05f;
        [Tooltip("View-model kick up in degrees.")]
        public float viewKickRotation = 6f;
        public float cameraShake = 0.08f;
        public float equipTime = 0.25f;

        [Header("Knockback")]
        [Tooltip("Velocity change applied to enemies hit (per pellet).")]
        public float targetKnockback = 2f;
        [Tooltip("Velocity change applied to the player opposite the aim direction (shotgun boosting).")]
        public float selfKnockback;
        [Tooltip("Self knockback multiplier while grounded.")]
        public float groundedSelfKnockbackMultiplier = 1f;
        [Tooltip("Extra self knockback multiplier when shooting a surface within this range (0 = disabled).")]
        public float surfaceBoostRange;
        public float surfaceBoostMultiplier = 1.25f;

        [Header("Projectiles")]
        public float projectileSpeed = 40f;
        public float projectileGravity;
        public float projectileLifetime = 6f;
        public float explosionRadius;
        public float explosionDamage;
        [Tooltip("Velocity change at the blast center (rocket jump strength).")]
        public float explosionForce = 18f;
        public float selfDamageMultiplier = 0.1f;
        public float playerExplosionForceMultiplier = 1f;
        public float grenadeFuse = 2.2f;

        [Header("Railgun")]
        [Tooltip("How many enemies a shot can pass through.")]
        public int penetration;
        public float trailWidth = 0.15f;

        [Header("Movement Interaction")]
        [Tooltip("Player speed multiplier while this weapon is equipped.")]
        public float moveSpeedMultiplier = 1f;
        public float airborneSpreadMultiplier = 1.4f;
        [Tooltip("Extra spread (degrees) per 10 m/s of player speed.")]
        public float speedSpread = 0.2f;

        [Header("Alt Fire")]
        public AltFireMode altFire = AltFireMode.None;
        [Tooltip("FOV multiplier while zoomed.")]
        public float zoomFovMultiplier = 0.65f;
        public int fanShots = 3;
        public float fanInterval = 0.07f;
        public float fanSpread = 3f;

        public float FireInterval => fireRate > 0f ? 1f / fireRate : 0.5f;
        public bool IsExplosive => explosionRadius > 0f;
    }
}

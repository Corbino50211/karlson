using System.Collections;
using Momentum.Audio;
using Momentum.PlayerSystems;
using UnityEngine;

namespace Momentum.Weapons
{
    /// <summary>
    /// Base class for every player weapon. Handles ammo, fire rate, reloading, equip timing, recoil,
    /// self-knockback (shotgun boosting), alt-fire modes, muzzle effects and hit processing.
    /// Subclasses only implement how a shot travels (OnFire).
    /// </summary>
    public abstract class WeaponBase : MonoBehaviour
    {
        [SerializeField] protected WeaponDefinition definition;
        [Tooltip("Barrel tip of the view model (tracers start here).")]
        [SerializeField] protected Transform muzzle;
        [SerializeField] protected ParticleSystem muzzleFlash;
        [SerializeField] protected Light muzzleLight;

        protected WeaponContext ctx;
        protected float nextFireTime;
        protected float equipReadyTime;
        Coroutine reloadRoutine;
        Coroutine fanRoutine;
        float reloadStartTime;
        float muzzleLightTimer;

        public WeaponDefinition Definition => definition;
        public Transform Muzzle => muzzle != null ? muzzle : transform;
        public int AmmoInMagazine { get; protected set; }
        public int ReserveAmmo { get; protected set; }
        public bool IsReloading { get; private set; }
        public bool IsEquipped { get; private set; }
        public bool IsAiming { get; private set; }
        public bool IsInitialized => ctx != null;
        public float ReloadProgress => IsReloading && definition.reloadTime > 0f
            ? Mathf.Clamp01((Time.time - reloadStartTime) / definition.reloadTime)
            : 0f;
        public bool HasInfiniteReserve => definition != null && definition.infiniteReserve;
        public bool NeedsAmmo => definition != null && !definition.infiniteReserve && ReserveAmmo < definition.maxReserve;

        protected virtual void Awake()
        {
            if (muzzleLight != null) muzzleLight.enabled = false;
        }

        public virtual void Initialize(WeaponContext context, WeaponDefinition def)
        {
            ctx = context;
            if (def != null) definition = def;
            AmmoInMagazine = definition.magazineSize;
            ReserveAmmo = Mathf.Min(definition.startingReserve, definition.maxReserve);
            gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ Equip

        public virtual void Equip()
        {
            IsEquipped = true;
            gameObject.SetActive(true);
            equipReadyTime = Time.time + definition.equipTime;
            if (ctx.sway != null) ctx.sway.PlayEquip();
            if (ctx.movement != null) ctx.movement.SpeedMultiplier = definition.moveSpeedMultiplier;
            GameEvents.RaiseWeaponEquipped(this);
            GameEvents.RaiseAmmoChanged(this);
        }

        public virtual void Unequip()
        {
            CancelReload();
            StopFan();
            SetAiming(false);
            IsEquipped = false;
            if (muzzleLight != null) muzzleLight.enabled = false;
            gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ Per-frame

        /// <summary>Called by PlayerCombat every frame while equipped.</summary>
        public virtual void Tick(PlayerInputHandler input)
        {
            if (!IsEquipped || input == null) return;

            HandleAltFire(input);

            bool wantsFire = definition.fireMode == WeaponFireMode.FullAuto ? input.FireHeld : input.FirePressed;
            if (wantsFire) TryFire();
            if (input.ReloadPressed) StartReload();
        }

        protected virtual void Update()
        {
            if (muzzleLight != null && muzzleLight.enabled)
            {
                muzzleLightTimer -= Time.deltaTime;
                if (muzzleLightTimer <= 0f) muzzleLight.enabled = false;
            }
        }

        protected virtual void HandleAltFire(PlayerInputHandler input)
        {
            switch (definition.altFire)
            {
                case AltFireMode.Zoom:
                    SetAiming(input.AltFireHeld && !IsReloading);
                    break;
                case AltFireMode.Fan:
                    if (input.AltFirePressed) StartFan();
                    break;
                case AltFireMode.DoubleShot:
                case AltFireMode.Detonate:
                    if (input.AltFirePressed) OnAltFirePressed();
                    break;
            }
        }

        /// <summary>Alt-fire hook for DoubleShot / Detonate modes.</summary>
        protected virtual void OnAltFirePressed() { }

        public void SetAiming(bool aiming)
        {
            if (IsAiming == aiming) return;
            IsAiming = aiming;
            if (ctx == null) return;
            if (ctx.playerCamera != null) ctx.playerCamera.SetZoom(aiming ? definition.zoomFovMultiplier : 1f);
            if (ctx.sway != null) ctx.sway.SetAiming(aiming);
        }

        // ------------------------------------------------------------------ Firing

        public bool CanFireNow => IsEquipped && Time.time >= equipReadyTime && Time.time >= nextFireTime;

        public bool TryFire()
        {
            if (!IsEquipped || Time.time < equipReadyTime || Time.time < nextFireTime) return false;
            if (IsReloading)
            {
                if (AmmoInMagazine >= definition.ammoPerShot) CancelReload();
                else return false;
            }
            if (AmmoInMagazine < definition.ammoPerShot)
            {
                if (ReserveAmmo > 0 || definition.infiniteReserve) StartReload();
                else DryFire();
                return false;
            }
            FireShot(1f, definition.ammoPerShot, CurrentSpread());
            return true;
        }

        protected void DryFire()
        {
            nextFireTime = Time.time + 0.25f;
            AudioManager.Play2D(SoundId.DryFire, 0.6f);
        }

        /// <summary>Consumes ammo, fires and applies all feedback. multiplier scales pellets/knockback/recoil.</summary>
        protected void FireShot(float multiplier, int ammoCost, float spread)
        {
            AmmoInMagazine = Mathf.Max(0, AmmoInMagazine - ammoCost);
            nextFireTime = Time.time + definition.FireInterval * Mathf.Max(1f, multiplier * 0.75f);

            Vector3 origin = ctx.aim.position;
            Vector3 direction = ctx.aim.forward;
            float surfaceDistance = OnFire(origin, direction, spread, multiplier);

            ApplyFeedback(multiplier);
            ApplySelfKnockback(direction, multiplier, surfaceDistance);

            GameEvents.RaiseAmmoChanged(this);
            GameEvents.RaiseNoise(origin, 40f);

            if (AmmoInMagazine <= 0 && (ReserveAmmo > 0 || definition.infiniteReserve)) StartCoroutine(AutoReloadAfter(0.15f));
        }

        IEnumerator AutoReloadAfter(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (IsEquipped && AmmoInMagazine <= 0) StartReload();
        }

        /// <summary>
        /// Performs the shot. Returns the distance to the nearest surface hit along the aim direction
        /// (used for surface boosting), or float.PositiveInfinity.
        /// </summary>
        protected abstract float OnFire(Vector3 origin, Vector3 direction, float spread, float multiplier);

        protected virtual float CurrentSpread()
        {
            float s = IsAiming ? definition.aimSpread : definition.spread;
            if (ctx.movement != null)
            {
                if (!ctx.movement.IsGrounded) s *= definition.airborneSpreadMultiplier;
                s += definition.speedSpread * ctx.movement.HorizontalSpeed / 10f * (IsAiming ? 0.3f : 1f);
            }
            return s;
        }

        void ApplyFeedback(float multiplier)
        {
            if (ctx.playerCamera != null)
            {
                float aimScale = IsAiming ? 0.6f : 1f;
                ctx.playerCamera.AddRecoil(definition.recoilPitch * multiplier * aimScale, Random.Range(-1f, 1f) * definition.recoilYaw * multiplier);
            }
            if (ctx.sway != null)
            {
                ctx.sway.AddKick(new Vector3(0f, 0f, -definition.viewKick * multiplier), new Vector3(-definition.viewKickRotation * multiplier, Random.Range(-1f, 1f) * definition.viewKickRotation * 0.2f, 0f));
            }
            CameraShaker.Shake(definition.cameraShake * multiplier);
            if (muzzleFlash != null)
            {
                muzzleFlash.Clear(true);
                muzzleFlash.Play(true);
            }
            if (muzzleLight != null)
            {
                muzzleLight.enabled = true;
                muzzleLightTimer = 0.05f;
            }
            AudioManager.Play2D(definition.fireSound, Mathf.Clamp(0.8f + 0.1f * multiplier, 0.5f, 1.2f));
        }

        void ApplySelfKnockback(Vector3 direction, float multiplier, float surfaceDistance)
        {
            if (definition.selfKnockback <= 0f || ctx.movement == null) return;
            float strength = definition.selfKnockback * multiplier;
            if (ctx.movement.IsGrounded) strength *= definition.groundedSelfKnockbackMultiplier;
            if (definition.surfaceBoostRange > 0f && surfaceDistance <= definition.surfaceBoostRange) strength *= definition.surfaceBoostMultiplier;
            ctx.movement.AddImpulse(-direction * strength);
        }

        // ------------------------------------------------------------------ Hit processing

        /// <summary>Applies damage, knockback and impact effects for a hitscan hit. Returns true if a damageable was hit.</summary>
        protected bool ProcessHit(RaycastHit hit, Vector3 direction, float damage, float knockback, DamageType type = DamageType.Bullet)
        {
            var registry = PrefabRegistry.Instance;
            var damageable = hit.collider.GetComponentInParent<IDamageable>();
            bool hitDamageable = false;
            if (damageable != null && damageable.IsAlive)
            {
                var info = new DamageInfo(damage, type, ctx.owner, hit.point, direction, true)
                {
                    normal = hit.normal,
                    knockback = knockback
                };
                if (damageable is Hitbox) info.isCritical = true;
                damageable.TakeDamage(info);
                bool killed = !damageable.IsAlive;
                GameEvents.RaiseHitConfirmed(killed, info.isCritical);
                hitDamageable = true;
            }

            var rb = hit.rigidbody;
            if (rb != null && knockback > 0f)
            {
                var knock = rb.GetComponent<IKnockbackable>();
                if (knock != null) knock.ApplyKnockback(direction * knockback, knockback > 6f ? 0.25f : 0f);
                else if (!rb.isKinematic) rb.AddForceAtPosition(direction * knockback * 1.5f, hit.point, ForceMode.Impulse);
            }

            if (registry != null)
            {
                var fx = hitDamageable ? registry.impactEnemy : registry.impactEnvironment;
                if (fx != null) PoolManager.Spawn(fx, hit.point + hit.normal * 0.02f, Quaternion.LookRotation(hit.normal));
            }
            if (!hitDamageable) AudioManager.Play(SoundId.BulletImpact, hit.point, 0.35f);
            return hitDamageable;
        }

        protected void SpawnTracer(Vector3 from, Vector3 to)
        {
            var registry = PrefabRegistry.Instance;
            if (registry == null || registry.tracer == null) return;
            var go = PoolManager.Spawn(registry.tracer, from, Quaternion.identity);
            if (go == null) return;
            var tracer = go.GetComponent<Tracer>();
            if (tracer != null) tracer.Play(from, to, definition.tracerColor, 0.03f, false);
        }

        protected Vector3 MuzzlePosition => muzzle != null ? muzzle.position : ctx.aim.position + ctx.aim.forward * 0.5f;

        // ------------------------------------------------------------------ Reload & ammo

        public void StartReload()
        {
            if (!IsEquipped || IsReloading) return;
            if (AmmoInMagazine >= definition.magazineSize) return;
            if (ReserveAmmo <= 0 && !definition.infiniteReserve) return;
            StopFan();
            SetAiming(false);
            reloadRoutine = StartCoroutine(ReloadRoutine());
        }

        IEnumerator ReloadRoutine()
        {
            IsReloading = true;
            reloadStartTime = Time.time;
            if (ctx.sway != null) ctx.sway.SetReloading(true);
            AudioManager.Play2D(SoundId.Reload, 0.7f);
            GameEvents.RaiseAmmoChanged(this);
            yield return new WaitForSeconds(definition.reloadTime);
            int needed = definition.magazineSize - AmmoInMagazine;
            int take = definition.infiniteReserve ? needed : Mathf.Min(needed, ReserveAmmo);
            AmmoInMagazine += take;
            if (!definition.infiniteReserve) ReserveAmmo -= take;
            IsReloading = false;
            reloadRoutine = null;
            if (ctx.sway != null) ctx.sway.SetReloading(false);
            GameEvents.RaiseAmmoChanged(this);
        }

        public void CancelReload()
        {
            if (reloadRoutine != null) StopCoroutine(reloadRoutine);
            reloadRoutine = null;
            if (IsReloading && ctx != null && ctx.sway != null) ctx.sway.SetReloading(false);
            IsReloading = false;
        }

        /// <summary>Adds reserve ammo. Returns the amount actually added.</summary>
        public int AddAmmo(int amount)
        {
            if (definition.infiniteReserve || amount <= 0) return 0;
            int before = ReserveAmmo;
            ReserveAmmo = Mathf.Min(definition.maxReserve, ReserveAmmo + amount);
            int added = ReserveAmmo - before;
            if (added > 0) GameEvents.RaiseAmmoChanged(this);
            return added;
        }

        public void RefillMagazine()
        {
            AmmoInMagazine = definition.magazineSize;
            GameEvents.RaiseAmmoChanged(this);
        }

        // ------------------------------------------------------------------ Revolver fan

        void StartFan()
        {
            if (fanRoutine != null || !IsEquipped || IsReloading) return;
            if (AmmoInMagazine <= 0)
            {
                TryFire();
                return;
            }
            fanRoutine = StartCoroutine(FanRoutine());
        }

        IEnumerator FanRoutine()
        {
            int shots = Mathf.Min(definition.fanShots, AmmoInMagazine);
            for (int i = 0; i < shots; i++)
            {
                while (Time.time < equipReadyTime) yield return null;
                if (AmmoInMagazine <= 0 || !IsEquipped) break;
                FireShot(1f, 1, CurrentSpread() + definition.fanSpread);
                nextFireTime = Time.time + definition.fanInterval;
                yield return new WaitForSeconds(definition.fanInterval);
            }
            nextFireTime = Time.time + definition.FireInterval;
            fanRoutine = null;
        }

        void StopFan()
        {
            if (fanRoutine != null) StopCoroutine(fanRoutine);
            fanRoutine = null;
        }
    }
}

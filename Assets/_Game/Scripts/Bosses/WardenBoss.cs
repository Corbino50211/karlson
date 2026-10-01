using System.Collections;
using System.Collections.Generic;
using Momentum.Audio;
using Momentum.PlayerSystems;
using UnityEngine;

namespace Momentum.Bosses
{
    /// <summary>
    /// THE WARDEN — large humanoid robot.
    /// Attacks: leaping ground slam, shotgun blast, charge (stuns itself on walls: damage window),
    /// alternating low/high shockwaves (jump the low ones, slide under the high ones) and a homing missile barrage.
    /// Phases: 1 (slam/shotgun/charge), 2 (+shockwaves, missiles), 3 (rage: faster everything).
    /// </summary>
    public class WardenBoss : BossBase
    {
        [Header("Warden Parts")]
        [SerializeField] Transform shotgunMuzzle;
        [SerializeField] Transform missilePod;

        [Header("Movement")]
        [SerializeField] float walkSpeed = 4.5f;
        [SerializeField] float preferredDistance = 12f;

        [Header("Ground Slam")]
        [SerializeField] float slamRadius = 6.5f;
        [SerializeField] float slamDamage = 28f;
        [SerializeField] float slamForce = 16f;
        [SerializeField] float leapDuration = 0.85f;
        [SerializeField] float leapHeight = 9f;

        [Header("Shotgun")]
        [SerializeField] int shotgunPellets = 12;
        [SerializeField] float shotgunSpread = 16f;
        [SerializeField] float pelletSpeed = 24f;
        [SerializeField] float pelletDamage = 7f;

        [Header("Charge")]
        [SerializeField] float chargeSpeed = 26f;
        [SerializeField] float chargeDamage = 30f;
        [SerializeField] float chargeKnockback = 22f;
        [SerializeField] float chargeMaxTime = 2.2f;
        [SerializeField] float wallStunTime = 2.6f;

        [Header("Shockwaves")]
        [SerializeField] float shockwaveSpeed = 16f;
        [SerializeField] float shockwaveRadius = 42f;
        [SerializeField] float shockwaveDamage = 18f;

        [Header("Missiles")]
        [SerializeField] int missileCount = 6;
        [SerializeField] float missileSpeed = 16f;
        [SerializeField] float missileTurnRate = 75f;
        [SerializeField] float missileDamage = 14f;

        static readonly Color SlamColor = new Color(1f, 0.25f, 0.1f);
        static readonly Color LowRingColor = new Color(1f, 0.55f, 0.1f);
        static readonly Color HighRingColor = new Color(0.75f, 0.2f, 1f);

        float groundY;
        Vector3 visualBaseLocal;
        Quaternion visualBaseRotation;

        float SpeedMultiplier => Phase >= 2 ? 1.25f : 1f;
        protected override float CooldownMultiplier => Phase >= 2 ? 0.6f : (Phase == 1 ? 0.85f : 1f);

        protected override void Start()
        {
            base.Start();
            groundY = transform.position.y;
            if (visualRoot != null)
            {
                visualBaseLocal = visualRoot.localPosition;
                visualBaseRotation = visualRoot.localRotation;
            }
        }

        public override void PopulateDefaultAttacks()
        {
            bossName = "THE WARDEN";
            bossType = BossType.Warden;
            phaseThresholds = new[] { 0.66f, 0.33f };
            attacks = new List<BossAttack>
            {
                new BossAttack("slam", 7f, 0, 1.2f, 5f, 45f),
                new BossAttack("shotgun", 4f, 0, 1.5f, 0f, 28f),
                new BossAttack("charge", 8f, 0, 1f, 8f, 70f),
                new BossAttack("shockwave", 9f, 1, 1.3f),
                new BossAttack("missiles", 12f, 1, 1f, 8f, 999f)
            };
        }

        protected override void UpdateBoss(float dt)
        {
            if (busy) return;
            var target = Target;
            if (target == null) return;
            RotateBodyTowards(target.transform.position, 140f, dt);
            if (DistanceToTarget > preferredDistance)
            {
                Vector3 dir = target.transform.position - BodyPosition;
                dir.y = 0f;
                Vector3 next = ClampToArena(BodyPosition + dir.normalized * walkSpeed * SpeedMultiplier * dt, 3f);
                next.y = groundY;
                MoveBody(next);
            }
        }

        protected override IEnumerator PerformAttack(string attackId)
        {
            switch (attackId)
            {
                case "slam": yield return StartCoroutine(Slam()); break;
                case "shotgun": yield return StartCoroutine(ShotgunBlast()); break;
                case "charge": yield return StartCoroutine(Charge()); break;
                case "shockwave": yield return StartCoroutine(Shockwaves()); break;
                case "missiles": yield return StartCoroutine(Missiles()); break;
            }
        }

        IEnumerator Slam()
        {
            if (Target == null) yield break;
            Vector3 destination = ClampToArena(TargetFeet, 3f);
            destination.y = groundY;
            TelegraphCircle(destination, slamRadius, leapDuration + 0.45f, SlamColor);

            yield return StartCoroutine(Crouch(0.4f, 0.8f));
            AudioManager.Play(SoundId.BossRoar, BodyPosition, 0.5f, 1.3f);

            Vector3 start = BodyPosition;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / leapDuration;
                float k = Mathf.Clamp01(t);
                Vector3 p = Vector3.Lerp(start, destination, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * leapHeight;
                MoveBody(p);
                RotateBodyTowards(destination + (destination - start), 360f, Time.deltaTime);
                yield return null;
            }
            MoveBody(destination);
            AudioManager.Play(SoundId.BossSlam, destination, 1f);
            CameraShaker.ShakeAt(destination, 0.9f, 45f);
            AreaBlast(destination + Vector3.up * 0.5f, slamRadius, slamDamage, slamForce);
            SpawnShockwave(destination, shockwaveRadius * 0.7f, shockwaveSpeed, 0.9f, 0f, shockwaveDamage * 0.7f, 10f, LowRingColor);
            yield return new WaitForSeconds(0.7f);
        }

        IEnumerator ShotgunBlast()
        {
            float windup = Phase >= 2 ? 0.35f : 0.55f;
            if (hitFlash != null) hitFlash.Flash();
            AudioManager.Play(SoundId.EnemyAlert, BodyPosition + Vector3.up * 3f, 0.8f, 0.6f);
            float t = 0f;
            while (t < windup)
            {
                t += Time.deltaTime;
                if (Target != null) RotateBodyTowards(Target.transform.position, 360f, Time.deltaTime);
                yield return null;
            }

            int blasts = Phase >= 1 ? 2 : 1;
            var registry = PrefabRegistry.Instance;
            for (int b = 0; b < blasts; b++)
            {
                var target = Target;
                if (target == null) yield break;
                Vector3 from = shotgunMuzzle != null ? shotgunMuzzle.position : BodyPosition + Vector3.up * 3f;
                Vector3 aim = MathUtil.PredictPosition(from, target.AimPoint, target.Velocity, pelletSpeed, 0.3f);
                Vector3 dir = (aim - from).normalized;
                for (int i = 0; i < shotgunPellets; i++)
                {
                    FireProjectile(registry != null ? registry.enemyPellet : null, from, MathUtil.RandomInCone(dir, shotgunSpread), pelletSpeed, pelletDamage, 3f);
                }
                AudioManager.Play(SoundId.Shotgun, from, 1f, 0.7f);
                CameraShaker.ShakeAt(from, 0.3f, 30f);
                if (registry != null && registry.muzzleFlash != null) PoolManager.Spawn(registry.muzzleFlash, from, Quaternion.LookRotation(dir));
                yield return new WaitForSeconds(0.45f);
            }
        }

        IEnumerator Charge()
        {
            var target = Target;
            if (target == null) yield break;
            Vector3 dir = target.transform.position - BodyPosition;
            dir.y = 0f;
            dir = dir.sqrMagnitude > 0.01f ? dir.normalized : transform.forward;
            TelegraphLane(BodyPosition, dir, 45f, 4.5f, 0.9f, SlamColor);
            AudioManager.Play(SoundId.BossRoar, BodyPosition, 0.8f, 1.1f);

            float t = 0f;
            while (t < 0.9f)
            {
                t += Time.deltaTime;
                RotateBodyTowards(BodyPosition + dir * 5f, 400f, Time.deltaTime);
                yield return null;
            }

            bool hitPlayer = false;
            bool crashed = false;
            t = 0f;
            float speed = chargeSpeed * SpeedMultiplier;
            while (t < chargeMaxTime)
            {
                float dt = Time.deltaTime;
                t += dt;
                Vector3 next = BodyPosition + dir * speed * dt;
                next.y = groundY;

                if (Physics.SphereCast(BodyPosition + Vector3.up * 2.5f, 1.4f, dir, out RaycastHit hit, speed * dt + 1.5f, Layers.SolidMask, QueryTriggerInteraction.Ignore) &&
                    !hit.collider.transform.IsChildOf(transform) && Mathf.Abs(hit.normal.y) < 0.5f)
                {
                    crashed = true;
                    break;
                }
                if (IsOutsideArena(next, 2f))
                {
                    crashed = true;
                    break;
                }

                var p = Target;
                if (!hitPlayer && p != null)
                {
                    Vector3 flat = p.transform.position - next;
                    flat.y = 0f;
                    if (flat.magnitude < 3.2f && p.FeetPosition.y < groundY + 4.5f)
                    {
                        hitPlayer = true;
                        p.Health.TakeDamage(new DamageInfo(chargeDamage, DamageType.Melee, gameObject, p.AimPoint, dir));
                        p.ApplyKnockback(dir * chargeKnockback + Vector3.up * 8f, 0f);
                        CameraShaker.Shake(0.6f);
                    }
                }
                MoveBody(next);
                yield return null;
            }

            if (crashed)
            {
                AudioManager.Play(SoundId.BossSlam, BodyPosition, 1f, 0.8f);
                CameraShaker.ShakeAt(BodyPosition, 0.8f, 50f);
                GameEvents.RaiseNotification("WARDEN STUNNED", new Color(1f, 0.9f, 0.3f));
                yield return StartCoroutine(Stunned(wallStunTime));
            }
            else
            {
                yield return new WaitForSeconds(0.4f);
            }
        }

        IEnumerator Shockwaves()
        {
            int count = Phase >= 2 ? 4 : 3;
            for (int i = 0; i < count; i++)
            {
                bool high = i % 2 == 1;
                yield return StartCoroutine(Crouch(0.25f, 0.6f));
                Vector3 center = new Vector3(BodyPosition.x, groundY, BodyPosition.z);
                if (high) SpawnShockwave(center, shockwaveRadius, shockwaveSpeed * 0.85f, 1.4f, 1.25f, shockwaveDamage, 12f, HighRingColor);
                else SpawnShockwave(center, shockwaveRadius, shockwaveSpeed, 0.9f, 0f, shockwaveDamage, 12f, LowRingColor);
                AudioManager.Play(SoundId.BossSlam, center, 0.8f, 1.2f);
                CameraShaker.ShakeAt(center, 0.4f, 40f);
                yield return new WaitForSeconds(0.95f);
            }
        }

        IEnumerator Missiles()
        {
            var registry = PrefabRegistry.Instance;
            var target = Target;
            if (registry == null || registry.missile == null || target == null) yield break;
            int count = Phase >= 2 ? 10 : missileCount;
            Vector3 from = missilePod != null ? missilePod.position : BodyPosition + Vector3.up * 5f;
            for (int i = 0; i < count; i++)
            {
                target = Target;
                if (target == null) break;
                Vector3 dir = (Vector3.up + Random.insideUnitSphere * 0.6f).normalized;
                var data = ProjectileData.Bullet(dir, missileSpeed, 0f, gameObject, false);
                data.homingTarget = target.transform;
                data.homingTurnRate = missileTurnRate;
                data.homingDelay = 0.5f;
                data.lifetime = 7f;
                data.explosionRadius = 2.6f;
                data.explosionDamage = missileDamage;
                data.explosionForce = 10f;
                data.explodeOnTimeout = true;
                FireProjectile(registry.missile, data, from);
                AudioManager.Play(SoundId.RocketLaunch, from, 0.6f, 1.2f);
                yield return new WaitForSeconds(0.12f);
            }
            yield return new WaitForSeconds(0.8f);
        }

        IEnumerator Crouch(float duration, float depth)
        {
            if (visualRoot == null)
            {
                yield return new WaitForSeconds(duration);
                yield break;
            }
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = Mathf.Sin(Mathf.Clamp01(t / duration) * Mathf.PI);
                visualRoot.localPosition = visualBaseLocal + Vector3.down * depth * k;
                yield return null;
            }
            visualRoot.localPosition = visualBaseLocal;
        }

        IEnumerator Stunned(float duration)
        {
            DamageTakenMultiplier = 1.75f;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                if (visualRoot != null)
                {
                    float k = Mathf.Clamp01(t / 0.3f) * Mathf.Clamp01((duration - t) / 0.3f);
                    visualRoot.localRotation = visualBaseRotation * Quaternion.Euler(18f * k, Mathf.Sin(t * 20f) * 3f * k, 0f);
                }
                yield return null;
            }
            if (visualRoot != null) visualRoot.localRotation = visualBaseRotation;
            DamageTakenMultiplier = 1f;
        }

        protected override void OnReset()
        {
            if (visualRoot != null)
            {
                visualRoot.localPosition = visualBaseLocal;
                visualRoot.localRotation = visualBaseRotation;
            }
        }
    }
}

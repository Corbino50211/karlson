using System.Collections;
using System.Collections.Generic;
using Momentum.Audio;
using Momentum.Levels;
using Momentum.PlayerSystems;
using UnityEngine;

namespace Momentum.Bosses
{
    /// <summary>
    /// THE JUGGERNAUT — massive melee brute.
    /// Attacks: charge that smashes through breakable walls/cover (crashes into solid walls and gets stunned),
    /// ground slam (jump to avoid), rock throws, and a rage phase below 35% health (faster, chained charges,
    /// double rocks). If the player hides behind breakable cover it charges straight through it.
    /// </summary>
    public class JuggernautBoss : BossBase
    {
        [Header("Juggernaut Parts")]
        [SerializeField] Transform rockHoldPoint;
        [SerializeField] GameObject heldRockVisual;
        [SerializeField] GameObject rageVisual;

        [Header("Movement")]
        [SerializeField] float walkSpeed = 5.5f;

        [Header("Charge")]
        [SerializeField] float chargeSpeed = 24f;
        [SerializeField] float chargeDamage = 35f;
        [SerializeField] float chargeKnockback = 24f;
        [SerializeField] float chargeMaxTime = 2.4f;
        [SerializeField] float wallStunTime = 2.3f;

        [Header("Slam")]
        [SerializeField] float slamRadius = 7f;
        [SerializeField] float slamDamage = 30f;
        [SerializeField] float slamKnockback = 18f;
        [Tooltip("Players higher than this above the floor avoid the slam.")]
        [SerializeField] float slamAirborneHeight = 1.4f;

        [Header("Rocks")]
        [SerializeField] float rockGravity = 22f;
        [SerializeField] float rockDamage = 25f;
        [SerializeField] float rockRadius = 3.5f;

        static readonly Color DangerColor = new Color(1f, 0.25f, 0.1f);
        static readonly Color RageColor = new Color(1f, 0.05f, 0.05f);

        float groundY;
        Vector3 visualBaseLocal;
        Quaternion visualBaseRotation;
        readonly Collider[] overlap = new Collider[16];

        bool Raging => Phase >= 1;
        float SpeedMultiplier => Raging ? 1.35f : 1f;
        protected override float CooldownMultiplier => Raging ? 0.6f : 1f;
        public override string PhaseLabel => Raging ? "RAGE" : "PHASE 1";

        protected override void Start()
        {
            base.Start();
            groundY = transform.position.y;
            if (visualRoot != null)
            {
                visualBaseLocal = visualRoot.localPosition;
                visualBaseRotation = visualRoot.localRotation;
            }
            if (heldRockVisual != null) heldRockVisual.SetActive(false);
            if (rageVisual != null) rageVisual.SetActive(false);
        }

        public override void PopulateDefaultAttacks()
        {
            bossName = "THE JUGGERNAUT";
            bossType = BossType.Juggernaut;
            phaseThresholds = new[] { 0.35f };
            attacks = new List<BossAttack>
            {
                new BossAttack("charge", 6f, 0, 1.3f, 6f, 80f),
                new BossAttack("slam", 5f, 0, 1.6f, 0f, 9f),
                new BossAttack("rocks", 6f, 0, 1.2f, 9f, 999f)
            };
        }

        protected override BossAttack ChooseAttack()
        {
            // Hiding behind breakable cover? Charge right through it.
            var target = Target;
            if (target != null && IsCoverBetween(target))
            {
                var charge = FindAttack("charge");
                if (charge != null) return charge;
            }
            return base.ChooseAttack();
        }

        bool IsCoverBetween(PlayerController target)
        {
            Vector3 from = BodyPosition + Vector3.up * 2f;
            if (Physics.Linecast(from, target.AimPoint, out RaycastHit hit, Layers.SightBlockMask, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.transform.IsChildOf(transform)) return false;
                var breakable = hit.collider.GetComponentInParent<BreakableObject>();
                return breakable != null && breakable.BossBreakable;
            }
            return false;
        }

        protected override void UpdateBoss(float dt)
        {
            if (busy) return;
            var target = Target;
            if (target == null) return;
            RotateBodyTowards(target.transform.position, 120f, dt);
            if (DistanceToTarget > 4f)
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
                case "charge":
                    yield return StartCoroutine(Charge(false));
                    break;
                case "slam":
                    yield return StartCoroutine(Slam());
                    break;
                case "rocks":
                    yield return StartCoroutine(Rocks());
                    break;
            }
        }

        IEnumerator Charge(bool chained)
        {
            var target = Target;
            if (target == null) yield break;
            Vector3 dir = target.transform.position - BodyPosition;
            dir.y = 0f;
            dir = dir.sqrMagnitude > 0.01f ? dir.normalized : transform.forward;
            float windup = chained ? 0.45f : 0.75f;
            TelegraphLane(BodyPosition, dir, 50f, 5f, windup, Raging ? RageColor : DangerColor);
            AudioManager.Play(SoundId.ChargerRoar, BodyPosition, 1f, 0.6f);

            float t = 0f;
            while (t < windup)
            {
                t += Time.deltaTime;
                RotateBodyTowards(BodyPosition + dir * 5f, 500f, Time.deltaTime);
                yield return null;
            }

            bool hitPlayer = false;
            bool crashed = false;
            float speed = chargeSpeed * SpeedMultiplier;
            t = 0f;
            while (t < chargeMaxTime)
            {
                float dt = Time.deltaTime;
                t += dt;
                Vector3 next = BodyPosition + dir * speed * dt;
                next.y = groundY;

                // Smash breakables in front.
                Vector3 front = BodyPosition + dir * 2.5f + Vector3.up * 2.2f;
                int count = Physics.OverlapBoxNonAlloc(front, new Vector3(2.4f, 2f, 1.6f), overlap, Quaternion.LookRotation(dir), Layers.SolidMask, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < count; i++)
                {
                    var c = overlap[i];
                    if (c == null || c.transform.IsChildOf(transform)) continue;
                    var breakable = c.GetComponentInParent<BreakableObject>();
                    if (breakable != null && breakable.BossBreakable && breakable.IsAlive)
                    {
                        breakable.Break(dir * 14f + Vector3.up * 4f);
                        CameraShaker.ShakeAt(front, 0.5f, 30f);
                        speed *= 0.92f;
                    }
                }

                if (Physics.SphereCast(BodyPosition + Vector3.up * 2f, 1.6f, dir, out RaycastHit hit, speed * dt + 1.6f, Layers.SolidMask, QueryTriggerInteraction.Ignore) &&
                    !hit.collider.transform.IsChildOf(transform) && Mathf.Abs(hit.normal.y) < 0.5f)
                {
                    var breakable = hit.collider.GetComponentInParent<BreakableObject>();
                    var hitBody = hit.rigidbody;
                    if (breakable == null && (hitBody == null || hitBody.isKinematic))
                    {
                        crashed = true;
                        break;
                    }
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
                    if (flat.magnitude < 3.4f && p.FeetPosition.y < groundY + 4f)
                    {
                        hitPlayer = true;
                        p.Health.TakeDamage(new DamageInfo(chargeDamage, DamageType.Melee, gameObject, p.AimPoint, dir));
                        p.ApplyKnockback(dir * chargeKnockback + Vector3.up * 9f, 0f);
                        CameraShaker.Shake(0.7f);
                        AudioManager.Play(SoundId.BossSlam, p.AimPoint, 0.8f, 1.3f);
                    }
                }
                MoveBody(next);
                yield return null;
            }

            if (crashed)
            {
                AudioManager.Play(SoundId.BossSlam, BodyPosition, 1f, 0.7f);
                CameraShaker.ShakeAt(BodyPosition, 0.9f, 50f);
                GameEvents.RaiseNotification("JUGGERNAUT STUNNED", new Color(1f, 0.9f, 0.3f));
                yield return StartCoroutine(Stunned(Raging ? wallStunTime * 0.7f : wallStunTime));
            }
            else if (Raging && !chained)
            {
                yield return new WaitForSeconds(0.3f);
                yield return StartCoroutine(Charge(true));
            }
            else
            {
                yield return new WaitForSeconds(0.5f);
            }
        }

        IEnumerator Slam()
        {
            Vector3 center = new Vector3(BodyPosition.x, groundY, BodyPosition.z);
            float windup = Raging ? 0.45f : 0.65f;
            TelegraphCircle(center, slamRadius, windup, Raging ? RageColor : DangerColor);
            AudioManager.Play(SoundId.ChargerRoar, center, 0.8f, 0.8f);
            float t = 0f;
            while (t < windup)
            {
                t += Time.deltaTime;
                if (visualRoot != null) visualRoot.localPosition = visualBaseLocal + Vector3.up * 0.6f * Mathf.Clamp01(t / windup);
                yield return null;
            }
            if (visualRoot != null) visualRoot.localPosition = visualBaseLocal;

            var registry = PrefabRegistry.Instance;
            if (registry != null && registry.explosion != null)
            {
                var fx = PoolManager.Spawn(registry.explosion, center, Quaternion.identity);
                if (fx != null) fx.transform.localScale = Vector3.one * slamRadius / 5f;
            }
            AudioManager.Play(SoundId.BossSlam, center, 1f);
            CameraShaker.ShakeAt(center, 0.9f, 45f);

            var p = Target;
            if (p != null)
            {
                Vector3 flat = p.FeetPosition - center;
                float height = flat.y;
                flat.y = 0f;
                if (flat.magnitude <= slamRadius && height < slamAirborneHeight)
                {
                    Vector3 outward = flat.sqrMagnitude > 0.01f ? flat.normalized : transform.forward;
                    p.Health.TakeDamage(new DamageInfo(slamDamage, DamageType.Melee, gameObject, p.FeetPosition, outward));
                    p.ApplyKnockback(outward * slamKnockback + Vector3.up * 10f, 0f);
                }
            }
            // Push physics props.
            int count = Physics.OverlapSphereNonAlloc(center, slamRadius, overlap, (1 << Layers.Prop) | (1 << Layers.Debris), QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var rb = overlap[i].attachedRigidbody;
                if (rb != null && !rb.isKinematic) rb.AddExplosionForce(14f, center, slamRadius, 1f, ForceMode.VelocityChange);
            }
            SpawnShockwave(center, 18f, 14f, 0.8f, 0f, 14f, 10f, Raging ? RageColor : new Color(1f, 0.55f, 0.1f));
            yield return new WaitForSeconds(Raging ? 0.4f : 0.7f);
        }

        IEnumerator Rocks()
        {
            var registry = PrefabRegistry.Instance;
            if (registry == null || registry.rock == null) yield break;
            int throws = Raging ? 2 : 1;
            for (int i = 0; i < throws; i++)
            {
                if (heldRockVisual != null) heldRockVisual.SetActive(true);
                float windup = Raging ? 0.5f : 0.8f;
                float t = 0f;
                while (t < windup)
                {
                    t += Time.deltaTime;
                    if (Target != null) RotateBodyTowards(Target.transform.position, 300f, Time.deltaTime);
                    yield return null;
                }
                if (heldRockVisual != null) heldRockVisual.SetActive(false);

                var target = Target;
                if (target == null) yield break;
                Vector3 from = rockHoldPoint != null ? rockHoldPoint.position : BodyPosition + Vector3.up * 6f;
                float dist = Vector3.Distance(from, target.FeetPosition);
                float flight = Mathf.Clamp(dist / 22f, 0.6f, 1.5f);
                Vector3 aim = target.FeetPosition + target.Velocity * flight * 0.5f;
                Vector3 velocity = (aim - from) / flight + Vector3.up * 0.5f * rockGravity * flight;

                var data = ProjectileData.Bullet(velocity.normalized, velocity.magnitude, rockDamage, gameObject, false);
                data.gravity = rockGravity;
                data.lifetime = 6f;
                data.explosionRadius = rockRadius;
                data.explosionDamage = rockDamage;
                data.explosionForce = 12f;
                data.explodeOnTimeout = true;
                FireProjectile(registry.rock, data, from);
                AudioManager.Play(SoundId.ChargerRoar, from, 0.6f, 1.2f);
                yield return new WaitForSeconds(0.35f);
            }
            yield return new WaitForSeconds(0.5f);
        }

        IEnumerator Stunned(float duration)
        {
            DamageTakenMultiplier = 2f;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                if (visualRoot != null)
                {
                    float k = Mathf.Clamp01(t / 0.3f) * Mathf.Clamp01((duration - t) / 0.3f);
                    visualRoot.localRotation = visualBaseRotation * Quaternion.Euler(-14f * k, 0f, Mathf.Sin(t * 18f) * 4f * k);
                }
                yield return null;
            }
            if (visualRoot != null) visualRoot.localRotation = visualBaseRotation;
            DamageTakenMultiplier = 1f;
        }

        protected override void OnPhaseChanged(int phase)
        {
            if (phase >= 1)
            {
                if (rageVisual != null) rageVisual.SetActive(true);
                GameEvents.RaiseNotification("THE JUGGERNAUT IS ENRAGED", RageColor);
                CameraShaker.Shake(0.6f);
            }
        }

        protected override void OnReset()
        {
            if (rageVisual != null) rageVisual.SetActive(false);
            if (heldRockVisual != null) heldRockVisual.SetActive(false);
            if (visualRoot != null)
            {
                visualRoot.localPosition = visualBaseLocal;
                visualRoot.localRotation = visualBaseRotation;
            }
        }
    }
}

using Momentum.Audio;
using Momentum.PlayerSystems;
using UnityEngine;

namespace Momentum.Enemies
{
    /// <summary>
    /// Long-range marksman. Keeps its distance and paints the player with a visible laser sight that
    /// turns from yellow to red before firing a high-damage instant shot. Break line of sight to dodge.
    /// </summary>
    public class SniperEnemy : EnemyBase
    {
        [SerializeField] LineRenderer laser;
        [SerializeField] float aimTime = 1.6f;
        [SerializeField] float lockTime = 0.3f;
        [SerializeField] float laserTrackSpeed = 4f;
        [SerializeField] Color warnColor = new Color(1f, 0.85f, 0.1f);
        [SerializeField] Color lockColor = new Color(1f, 0.1f, 0.1f);

        bool aiming;
        float aimStart;
        Vector3 laserDirection;
        bool chargePlayed;

        protected override void Start()
        {
            base.Start();
            SetLaser(false);
        }

        protected override void UpdateChase()
        {
            var target = Target;
            if (target != null && canSeeTarget && DistanceToTarget <= definition.attackRange)
            {
                SetState(EnemyState.Attack);
                return;
            }
            base.UpdateChase();
        }

        protected override void UpdateAttack()
        {
            var target = Target;
            if (target == null)
            {
                CancelAim();
                SetState(EnemyState.Search);
                return;
            }
            FacePoint(target.AimPoint);
            float dist = DistanceToTarget;

            // Back away if the player gets too close.
            if (!aiming && dist < definition.preferredRange * 0.5f)
            {
                Vector3 away = transform.position - target.transform.position;
                away.y = 0f;
                MoveTo(transform.position + away.normalized * 4f, definition.chaseSpeed);
            }
            else
            {
                StopMoving();
            }

            if (!aiming)
            {
                if (!canSeeTarget && Time.time - lastSeenTime > 1.5f)
                {
                    SetState(EnemyState.Chase);
                    return;
                }
                if (canSeeTarget && Time.time >= nextAttackTime) BeginAim();
                return;
            }

            float t = Time.time - aimStart;
            bool locked = t >= aimTime - lockTime;
            Vector3 from = MuzzlePosition;
            if (!locked)
            {
                Vector3 desired = (target.AimPoint - from).normalized;
                laserDirection = Vector3.Slerp(laserDirection, desired, MathUtil.Damp(laserTrackSpeed, Time.deltaTime)).normalized;
            }
            if (!chargePlayed && locked)
            {
                chargePlayed = true;
                AudioManager.Play(SoundId.LaserCharge, from, 0.5f, 1.6f);
            }
            UpdateLaserVisual(from, Mathf.Clamp01(t / aimTime), locked);

            if (t >= aimTime) Fire(from);
            else if (!canSeeTarget && Time.time - lastSeenTime > 0.5f) CancelAim();
        }

        void BeginAim()
        {
            aiming = true;
            chargePlayed = false;
            aimStart = Time.time;
            laserDirection = (Target.AimPoint - MuzzlePosition).normalized;
            SetLaser(true);
            AudioManager.Play(SoundId.LaserCharge, MuzzlePosition, 0.4f);
        }

        void Fire(Vector3 from)
        {
            aiming = false;
            SetLaser(false);
            nextAttackTime = Time.time + definition.attackCooldown;
            Vector3 end = from + laserDirection * definition.sightRange;
            if (PhysicsUtil.SweepClosest(from, laserDirection, definition.sightRange * 1.2f, 0f, Layers.EnemyShootMask, transform, out RaycastHit hit))
            {
                end = hit.point;
                var player = hit.collider.GetComponentInParent<PlayerController>();
                if (player != null && player.Health != null)
                {
                    var info = new DamageInfo(definition.damage, DamageType.Bullet, gameObject, hit.point, laserDirection);
                    player.Health.TakeDamage(info);
                    player.ApplyKnockback(laserDirection * definition.hitKnockback, 0f);
                }
                else
                {
                    var registry = PrefabRegistry.Instance;
                    if (registry != null && registry.impactEnvironment != null) PoolManager.Spawn(registry.impactEnvironment, hit.point, Quaternion.LookRotation(hit.normal));
                }
            }
            var reg = PrefabRegistry.Instance;
            if (reg != null && reg.railTrail != null)
            {
                var go = PoolManager.Spawn(reg.railTrail, from, Quaternion.identity);
                var tracer = go != null ? go.GetComponent<Tracer>() : null;
                if (tracer != null) tracer.Play(from, end, lockColor, 0.08f, true);
            }
            SpawnMuzzleFlash();
            AudioManager.Play(SoundId.Railgun, from, 0.8f, 1.3f);
            GameEvents.RaiseNoise(from, 30f);
        }

        void CancelAim()
        {
            if (!aiming) return;
            aiming = false;
            SetLaser(false);
            nextAttackTime = Time.time + 0.8f;
        }

        void UpdateLaserVisual(Vector3 from, float progress, bool locked)
        {
            if (laser == null) return;
            Vector3 end = from + laserDirection * definition.sightRange;
            if (Physics.Raycast(from, laserDirection, out RaycastHit hit, definition.sightRange, Layers.EnemyShootMask, QueryTriggerInteraction.Ignore)) end = hit.point;
            laser.SetPosition(0, from);
            laser.SetPosition(1, end);
            Color c = locked ? lockColor : Color.Lerp(warnColor, lockColor, progress * 0.6f);
            float flicker = locked ? (Mathf.Sin(Time.time * 60f) > 0f ? 1f : 0.4f) : 1f;
            c.a = Mathf.Lerp(0.35f, 1f, progress) * flicker;
            laser.startColor = c;
            laser.endColor = c;
            laser.widthMultiplier = Mathf.Lerp(0.02f, 0.06f, progress);
        }

        void SetLaser(bool on)
        {
            if (laser == null) return;
            laser.enabled = on;
            laser.useWorldSpace = true;
            laser.positionCount = 2;
        }

        protected override void OnStateChanged(EnemyState previous, EnemyState current)
        {
            if (current != EnemyState.Attack) CancelAim();
        }
    }
}

using System.Collections;
using UnityEngine;

namespace Momentum.Enemies
{
    /// <summary>
    /// Stationary turret. Sweeps while idle, tracks the player with a limited turn rate and fires bursts
    /// once its barrel lines up. Can be shielded (used by The Core boss).
    /// </summary>
    public class TurretEnemy : EnemyBase
    {
        [SerializeField] float aimToleranceDegrees = 6f;
        [SerializeField] float idleSweepAngle = 60f;
        [SerializeField] GameObject shieldVisual;

        bool firing;
        float baseYaw;
        bool shielded;

        public bool Shielded => shielded;

        protected override void Awake()
        {
            stationary = true;
            base.Awake();
        }

        protected override void Start()
        {
            base.Start();
            baseYaw = transform.eulerAngles.y;
            if (shieldVisual != null) shieldVisual.SetActive(shielded);
        }

        public void SetShielded(bool value)
        {
            shielded = value;
            health.Invulnerable = value;
            if (shieldVisual != null) shieldVisual.SetActive(value);
        }

        protected override void UpdateIdle()
        {
            if (canSeeTarget)
            {
                SetState(EnemyState.Alert);
                return;
            }
            float yaw = baseYaw + Mathf.Sin(Time.time * 0.6f) * idleSweepAngle;
            FacePoint(EyePosition + Quaternion.Euler(0f, yaw, 0f) * Vector3.forward * 10f);
        }

        protected override void UpdatePatrol()
        {
            UpdateIdle();
        }

        protected override void UpdateChase()
        {
            if (Target != null && canSeeTarget)
            {
                SetState(EnemyState.Attack);
                return;
            }
            if (Time.time - lastSeenTime > 2.5f) SetState(EnemyState.Idle);
            else FacePoint(lastKnownTargetPos);
        }

        protected override void UpdateAttack()
        {
            var target = Target;
            if (target == null || (!canSeeTarget && Time.time - lastSeenTime > 1f))
            {
                SetState(EnemyState.Chase);
                return;
            }
            FacePoint(target.AimPoint);
            if (aimPivot == null || firing || Time.time < nextAttackTime) return;
            Vector3 toTarget = target.AimPoint - aimPivot.position;
            if (Vector3.Angle(aimPivot.forward, toTarget) <= aimToleranceDegrees && toTarget.magnitude <= definition.attackRange)
            {
                StartCoroutine(Burst());
            }
        }

        protected override void UpdateAimPivot()
        {
            if (aimPivot == null) return;
            Vector3 target = hasLookTarget ? lookTarget : aimPivot.position + aimPivot.forward;
            Vector3 dir = target - aimPivot.position;
            if (dir.sqrMagnitude < 0.001f) return;
            var desired = Quaternion.LookRotation(dir.normalized, Vector3.up);
            aimPivot.rotation = Quaternion.RotateTowards(aimPivot.rotation, desired, definition.turnSpeed * Time.deltaTime);
        }

        IEnumerator Burst()
        {
            firing = true;
            int shots = Mathf.Max(1, definition.projectileCount);
            for (int i = 0; i < shots; i++)
            {
                if (IsDead || Target == null) break;
                Vector3 dir = MathUtil.RandomInCone(muzzle != null ? muzzle.forward : aimPivot.forward, definition.inaccuracy);
                FireProjectile(dir, definition.projectileSpeed, definition.damage, definition.hitKnockback);
                yield return new WaitForSeconds(definition.burstInterval);
            }
            nextAttackTime = Time.time + definition.attackCooldown;
            firing = false;
        }

        public override void ApplyKnockback(Vector3 velocityChange, float stunDuration)
        {
            // Turrets are bolted down.
        }
    }
}

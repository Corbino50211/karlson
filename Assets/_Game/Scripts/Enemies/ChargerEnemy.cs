using Momentum.Audio;
using Momentum.PlayerSystems;
using UnityEngine;

namespace Momentum.Enemies
{
    /// <summary>
    /// Rushes the player, winds up and performs a high-speed charge. A hit deals damage and knocks the
    /// player far backward; slamming into a wall stuns the charger.
    /// </summary>
    public class ChargerEnemy : EnemyBase
    {
        enum Phase { Approach, Windup, Charging, Recover }

        [SerializeField] float chargeSpeed = 22f;
        [SerializeField] float chargeDuration = 0.75f;
        [SerializeField] float recoverTime = 0.7f;
        [SerializeField] float wallStunTime = 1.6f;
        [SerializeField] float hitRadius = 1.5f;
        [SerializeField] float knockbackUp = 6f;

        Phase phase = Phase.Approach;
        float phaseStart;
        Vector3 chargeDirection;
        bool hitPlayerThisCharge;

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
            if (target == null && phase != Phase.Charging)
            {
                SetState(EnemyState.Search);
                return;
            }

            float t = Time.time - phaseStart;
            switch (phase)
            {
                case Phase.Approach:
                    accelerationOverride = -1f;
                    FacePoint(target.AimPoint);
                    if (!canSeeTarget && Time.time - lastSeenTime > 1f)
                    {
                        SetState(EnemyState.Chase);
                        return;
                    }
                    if (DistanceToTarget > definition.attackRange * 1.2f)
                    {
                        SetState(EnemyState.Chase);
                        return;
                    }
                    if (Time.time >= nextAttackTime && canSeeTarget)
                    {
                        EnterPhase(Phase.Windup);
                        StopMoving();
                        if (hitFlash != null) hitFlash.Flash();
                        AudioManager.Play(SoundId.ChargerRoar, EyePosition, 0.8f);
                    }
                    else
                    {
                        MoveTo(target.transform.position, definition.chaseSpeed);
                    }
                    break;

                case Phase.Windup:
                    StopMoving();
                    if (target != null) FacePoint(target.AimPoint);
                    if (t >= definition.windupTime)
                    {
                        Vector3 dir = target != null ? target.transform.position - transform.position : transform.forward;
                        dir.y = 0f;
                        chargeDirection = dir.sqrMagnitude > 0.01f ? dir.normalized : transform.forward;
                        hitPlayerThisCharge = false;
                        EnterPhase(Phase.Charging);
                    }
                    break;

                case Phase.Charging:
                    accelerationOverride = 120f;
                    desiredVelocity = chargeDirection * chargeSpeed;
                    FacePoint(transform.position + chargeDirection * 5f);
                    CheckChargeHits();
                    if (Physics.SphereCast(transform.position + Vector3.up, 0.6f, chargeDirection, out RaycastHit wall, 1.2f, Layers.SolidMask, QueryTriggerInteraction.Ignore) &&
                        Mathf.Abs(wall.normal.y) < 0.5f && (wall.rigidbody == null || wall.rigidbody.isKinematic))
                    {
                        CameraShaker.ShakeAt(transform.position, 0.4f, 20f);
                        AudioManager.Play(SoundId.BossSlam, transform.position, 0.5f, 1.5f);
                        EndCharge();
                        Stun(wallStunTime);
                        return;
                    }
                    if (t >= chargeDuration)
                    {
                        EndCharge();
                        EnterPhase(Phase.Recover);
                    }
                    break;

                case Phase.Recover:
                    StopMoving();
                    if (t >= recoverTime) EnterPhase(Phase.Approach);
                    break;
            }
        }

        void CheckChargeHits()
        {
            if (hitPlayerThisCharge) return;
            var target = Target;
            if (target == null) return;
            Vector3 toPlayer = target.AimPoint - (transform.position + Vector3.up);
            if (toPlayer.magnitude > hitRadius) return;
            hitPlayerThisCharge = true;
            var info = new DamageInfo(definition.damage, DamageType.Melee, gameObject, target.AimPoint, chargeDirection);
            target.Health.TakeDamage(info);
            target.ApplyKnockback(chargeDirection * definition.hitKnockback + Vector3.up * knockbackUp, 0f);
            CameraShaker.Shake(0.5f);
            AudioManager.Play(SoundId.BossSlam, target.AimPoint, 0.6f, 1.4f);
        }

        void EndCharge()
        {
            accelerationOverride = -1f;
            StopMoving();
            nextAttackTime = Time.time + definition.attackCooldown;
        }

        void EnterPhase(Phase p)
        {
            phase = p;
            phaseStart = Time.time;
        }

        protected override void OnStateChanged(EnemyState previous, EnemyState current)
        {
            if (current != EnemyState.Attack)
            {
                accelerationOverride = -1f;
                phase = Phase.Approach;
            }
        }
    }
}

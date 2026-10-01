using System.Collections;
using Momentum.Audio;
using UnityEngine;

namespace Momentum.Enemies
{
    /// <summary>
    /// Aggressive close-range enemy: rushes into short range, winds up briefly and fires a wide pellet blast
    /// that also knocks the player back.
    /// </summary>
    public class ShotgunnerEnemy : EnemyBase
    {
        [SerializeField] float pelletSpread = 9f;
        bool attacking;

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
                SetState(EnemyState.Search);
                return;
            }
            FacePoint(target.AimPoint);
            float dist = DistanceToTarget;
            if (!attacking)
            {
                if (dist > definition.attackRange * 1.2f || (!canSeeTarget && Time.time - lastSeenTime > 1f))
                {
                    SetState(EnemyState.Chase);
                    return;
                }
                // Keep closing distance.
                if (dist > definition.preferredRange) MoveTo(target.transform.position, definition.chaseSpeed);
                else StopMoving();

                if (canSeeTarget && Time.time >= nextAttackTime) StartCoroutine(Blast());
            }
            else
            {
                StopMoving();
            }
        }

        IEnumerator Blast()
        {
            attacking = true;
            if (hitFlash != null) hitFlash.Flash();
            AudioManager.Play(SoundId.EnemyAlert, EyePosition, 0.4f, 0.7f);
            yield return new WaitForSeconds(definition.windupTime);
            if (!IsDead && Target != null && State == EnemyState.Attack)
            {
                Vector3 baseDir = AimDirection(definition.projectileSpeed, 0f);
                int pellets = Mathf.Max(1, definition.projectileCount);
                for (int i = 0; i < pellets; i++)
                {
                    Vector3 dir = MathUtil.RandomInCone(baseDir, pelletSpread);
                    FireProjectile(dir, definition.projectileSpeed, definition.damage, definition.hitKnockback);
                }
            }
            nextAttackTime = Time.time + definition.attackCooldown;
            attacking = false;
        }

        protected override void OnStateChanged(EnemyState previous, EnemyState current)
        {
            if (current == EnemyState.Stunned || current == EnemyState.Dead)
            {
                StopAllCoroutines();
                attacking = false;
            }
        }
    }
}

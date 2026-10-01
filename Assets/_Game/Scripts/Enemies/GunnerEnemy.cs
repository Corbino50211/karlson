using System.Collections;
using UnityEngine;

namespace Momentum.Enemies
{
    /// <summary>
    /// Basic soldier: chases the player to a comfortable range, strafes and fires bursts of projectiles.
    /// </summary>
    public class GunnerEnemy : EnemyBase
    {
        float strafeDirection = 1f;
        float nextStrafeSwitch;
        bool firing;

        protected override void UpdateAttack()
        {
            var target = Target;
            if (target == null)
            {
                SetState(EnemyState.Search);
                return;
            }
            if (!canSeeTarget && Time.time - lastSeenTime > 1.2f)
            {
                SetState(EnemyState.Chase);
                return;
            }

            FacePoint(target.AimPoint);
            float dist = DistanceToTarget;
            if (dist > definition.attackRange * 1.15f)
            {
                SetState(EnemyState.Chase);
                return;
            }

            // Keep preferred range while strafing.
            if (Time.time >= nextStrafeSwitch)
            {
                strafeDirection = Random.value > 0.5f ? 1f : -1f;
                nextStrafeSwitch = Time.time + Random.Range(1.2f, 2.6f);
            }
            Vector3 toTarget = target.transform.position - transform.position;
            toTarget.y = 0f;
            Vector3 right = Vector3.Cross(Vector3.up, toTarget.normalized);
            Vector3 destination = transform.position + right * strafeDirection * 3f;
            if (dist > definition.preferredRange * 1.2f) destination += toTarget.normalized * 3f;
            else if (dist < definition.preferredRange * 0.6f) destination -= toTarget.normalized * 3f;
            MoveTo(destination, definition.moveSpeed);

            if (!firing && canSeeTarget && Time.time >= nextAttackTime)
            {
                StartCoroutine(Burst());
            }
        }

        IEnumerator Burst()
        {
            firing = true;
            int shots = Mathf.Max(1, definition.projectileCount);
            for (int i = 0; i < shots; i++)
            {
                if (IsDead || Target == null || State == EnemyState.Stunned) break;
                FireProjectile(AimDirection(definition.projectileSpeed), definition.projectileSpeed, definition.damage, definition.hitKnockback);
                yield return new WaitForSeconds(definition.burstInterval);
            }
            nextAttackTime = Time.time + definition.attackCooldown * Random.Range(0.85f, 1.2f);
            firing = false;
        }

        protected override void OnStateChanged(EnemyState previous, EnemyState current)
        {
            if (current == EnemyState.Stunned || current == EnemyState.Dead)
            {
                StopAllCoroutines();
                firing = false;
            }
        }
    }
}

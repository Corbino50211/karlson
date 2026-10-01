using Momentum.Audio;
using UnityEngine;

namespace Momentum.Enemies
{
    /// <summary>
    /// Flying ranged enemy. Orbits the player at height, avoids obstacles and fires energy bolts.
    /// Falls and explodes when destroyed.
    /// </summary>
    public class DroneEnemy : EnemyBase
    {
        [SerializeField] float orbitRadius = 11f;
        [SerializeField] float orbitSpeed = 25f;
        [SerializeField] float deathExplosionRadius = 3.5f;
        [SerializeField] float deathExplosionDamage = 15f;
        [SerializeField] Transform rotor;

        float orbitAngle;
        float orbitDirection = 1f;
        float nextOrbitFlip;

        protected override void Awake()
        {
            flying = true;
            base.Awake();
        }

        protected override void Start()
        {
            base.Start();
            orbitAngle = Random.Range(0f, 360f);
        }

        protected override void Update()
        {
            base.Update();
            if (rotor != null) rotor.Rotate(0f, 900f * Time.deltaTime, 0f, Space.Self);
        }

        protected override void UpdateChase()
        {
            var target = Target;
            if (target == null)
            {
                SetState(EnemyState.Search);
                return;
            }
            if (canSeeTarget && DistanceToTarget <= definition.attackRange)
            {
                SetState(EnemyState.Attack);
                return;
            }
            if (!canSeeTarget && Time.time - lastSeenTime > 4f)
            {
                SetState(EnemyState.Search);
                return;
            }
            Vector3 destination = (canSeeTarget ? target.transform.position : lastKnownTargetPos) + Vector3.up * hoverHeight;
            MoveTo(destination, definition.chaseSpeed);
            if (canSeeTarget) FacePoint(target.AimPoint);
        }

        protected override void UpdateAttack()
        {
            var target = Target;
            if (target == null)
            {
                SetState(EnemyState.Search);
                return;
            }
            if (!canSeeTarget && Time.time - lastSeenTime > 1.5f)
            {
                SetState(EnemyState.Chase);
                return;
            }

            if (Time.time >= nextOrbitFlip)
            {
                orbitDirection = Random.value > 0.3f ? orbitDirection : -orbitDirection;
                nextOrbitFlip = Time.time + Random.Range(2f, 4f);
            }
            orbitAngle += orbitDirection * orbitSpeed * Time.deltaTime;
            Vector3 offset = Quaternion.Euler(0f, orbitAngle, 0f) * Vector3.forward * orbitRadius;
            Vector3 hover = target.transform.position + offset + Vector3.up * hoverHeight;
            MoveTo(hover, definition.moveSpeed);
            FacePoint(target.AimPoint);

            if (canSeeTarget && Time.time >= nextAttackTime)
            {
                FireProjectile(AimDirection(definition.projectileSpeed), definition.projectileSpeed, definition.damage, definition.hitKnockback);
                nextAttackTime = Time.time + definition.attackCooldown * Random.Range(0.8f, 1.2f);
            }
        }

        protected override void UpdateSearch()
        {
            if (canSeeTarget)
            {
                SetState(EnemyState.Chase);
                return;
            }
            MoveTo(lastKnownTargetPos + Vector3.up * hoverHeight, definition.moveSpeed);
            if (TimeInState > definition.searchDuration) SetState(EnemyState.Patrol);
        }

        protected override void OnDeath(DamageInfo info)
        {
            var p = ExplosionParams.Create(transform.position, deathExplosionRadius, deathExplosionDamage, 8f, gameObject, false);
            p.effectPrefab = PrefabRegistry.Instance != null ? PrefabRegistry.Instance.smallExplosion : null;
            p.effectScale = 1f;
            p.cameraShake = 0.3f;
            Explosions.Explode(p);
            AudioManager.Play2D(SoundId.KillConfirm, 0.6f);
            BreakApart(info);
            Destroy(gameObject);
        }
    }
}

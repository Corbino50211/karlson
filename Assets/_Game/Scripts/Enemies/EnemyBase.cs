using System.Collections.Generic;
using Momentum.Audio;
using Momentum.Levels;
using Momentum.PlayerSystems;
using UnityEngine;

namespace Momentum.Enemies
{
    /// <summary>
    /// Reusable enemy architecture: state machine (Idle, Patrol, Alert, Chase, Attack, Search, Stunned, Dead),
    /// perception (sight cone + line of sight + hearing gunshots), NavMesh-assisted Rigidbody movement
    /// (ground or flying), knockback/stun, hit feedback, drops and a physical break-apart death.
    /// Targets are found automatically through PlayerController.Current.
    /// Subclasses implement the combat behaviour in UpdateChase / UpdateAttack.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(EnemyHealth))]
    public abstract class EnemyBase : MonoBehaviour, IKnockbackable, ILevelObjectConfigurable
    {
        public static readonly List<EnemyBase> All = new List<EnemyBase>();

        [SerializeField] protected EnemyDefinition definition;
        [SerializeField] protected Transform eye;
        [SerializeField] protected Transform aimPivot;
        [SerializeField] protected Transform muzzle;
        [SerializeField] protected Transform visualRoot;
        [SerializeField] protected GameObject alertIndicator;
        [SerializeField] protected GameObject projectilePrefab;
        [SerializeField] protected bool flying;
        [SerializeField] protected bool stationary;
        [SerializeField] protected string arenaId = "";
        [SerializeField] protected float patrolRadius = -1f;
        [SerializeField] protected float hoverHeight = 4.5f;

        protected Rigidbody rb;
        protected EnemyHealth health;
        protected HitFlash hitFlash;
        protected readonly EnemyNavigator navigator = new EnemyNavigator();

        protected Vector3 spawnPosition;
        protected Vector3 lastKnownTargetPos;
        protected float lastSeenTime = -999f;
        protected bool canSeeTarget;
        protected Vector3 desiredVelocity;
        protected Vector3 lookTarget;
        protected bool hasLookTarget;
        protected float nextAttackTime;
        protected float accelerationOverride = -1f;

        float stateEnterTime;
        float nextSightCheck;
        float stunnedUntil;
        Vector3 patrolTarget;
        float nextPatrolTime;
        bool registeredWithArena;
        bool started;

        public EnemyDefinition Definition => definition;
        public EnemyState State { get; private set; } = EnemyState.Idle;
        public string ArenaId => arenaId;
        public bool IsDead => State == EnemyState.Dead;
        public Health Health => health;
        public bool IsFlying => flying;
        protected float TimeInState => Time.time - stateEnterTime;
        protected bool IsStunned => Time.time < stunnedUntil;

        protected PlayerController Target
        {
            get
            {
                var p = PlayerController.Current;
                return p != null && !p.IsDead ? p : null;
            }
        }

        protected Vector3 EyePosition => eye != null ? eye.position : transform.position + Vector3.up * 1.6f;
        protected Vector3 MuzzlePosition => muzzle != null ? muzzle.position : EyePosition + transform.forward * 0.5f;

        protected float DistanceToTarget
        {
            get
            {
                var t = Target;
                return t != null ? Vector3.Distance(transform.position, t.transform.position) : float.PositiveInfinity;
            }
        }

        // ------------------------------------------------------------------ Lifecycle

        protected virtual void Awake()
        {
            rb = GetComponent<Rigidbody>();
            health = GetComponent<EnemyHealth>();
            hitFlash = GetComponent<HitFlash>();
            if (alertIndicator != null) alertIndicator.SetActive(false);
        }

        protected virtual void OnEnable()
        {
            if (!All.Contains(this)) All.Add(this);
            health.Damaged += OnDamaged;
            health.Died += OnDied;
            GameEvents.Noise += OnNoise;
            GameEvents.PlayerRespawned += OnPlayerRespawned;
        }

        protected virtual void OnDisable()
        {
            All.Remove(this);
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
            GameEvents.Noise -= OnNoise;
            GameEvents.PlayerRespawned -= OnPlayerRespawned;
        }

        protected virtual void Start()
        {
            started = true;
            spawnPosition = transform.position;
            if (definition != null)
            {
                health.SetMaxHealth(definition.maxHealth, true);
                if (patrolRadius < 0f) patrolRadius = definition.patrolRadius;
            }
            if (patrolRadius < 0f) patrolRadius = 0f;

            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            if (stationary)
            {
                rb.isKinematic = true;
            }
            else if (flying)
            {
                rb.useGravity = false;
                rb.drag = 0.5f;
            }
            nextSightCheck = Time.time + Random.Range(0f, 0.2f);

            if (!string.IsNullOrEmpty(arenaId))
            {
                ArenaRegistry.Register(arenaId, this);
                registeredWithArena = true;
            }
            SetState(patrolRadius > 0.5f ? EnemyState.Patrol : EnemyState.Idle);
        }

        // ------------------------------------------------------------------ Update

        protected virtual void Update()
        {
            if (!started || State == EnemyState.Dead) return;
            UpdatePerception();

            hasLookTarget = false;
            switch (State)
            {
                case EnemyState.Idle: UpdateIdle(); break;
                case EnemyState.Patrol: UpdatePatrol(); break;
                case EnemyState.Alert: UpdateAlert(); break;
                case EnemyState.Chase: UpdateChase(); break;
                case EnemyState.Attack: UpdateAttack(); break;
                case EnemyState.Search: UpdateSearch(); break;
                case EnemyState.Stunned: UpdateStunned(); break;
            }
            UpdateAimPivot();
        }

        protected virtual void FixedUpdate()
        {
            if (!started || State == EnemyState.Dead) return;
            float dt = Time.fixedDeltaTime;
            if (stationary)
            {
                return;
            }
            if (flying) FlyingMove(dt);
            else GroundMove(dt);
        }

        void UpdatePerception()
        {
            if (Time.time < nextSightCheck) return;
            nextSightCheck = Time.time + 0.15f;
            var target = Target;
            canSeeTarget = target != null && CanSee(target);
            if (canSeeTarget)
            {
                lastKnownTargetPos = target.AimPoint;
                lastSeenTime = Time.time;
            }
        }

        protected bool CanSee(PlayerController target)
        {
            if (definition == null) return false;
            Vector3 eyePos = EyePosition;
            Vector3 aim = target.AimPoint;
            Vector3 to = aim - eyePos;
            float dist = to.magnitude;
            if (dist > definition.sightRange) return false;
            bool unaware = State == EnemyState.Idle || State == EnemyState.Patrol;
            if (unaware && dist > definition.hearingRange)
            {
                Vector3 forward = aimPivot != null ? aimPivot.forward : transform.forward;
                if (Vector3.Angle(forward, to) > definition.fieldOfView * 0.5f) return false;
            }
            if (Physics.Linecast(eyePos, aim, out RaycastHit hit, Layers.SightBlockMask, QueryTriggerInteraction.Ignore))
            {
                if (!hit.collider.transform.IsChildOf(target.transform) && !hit.collider.transform.IsChildOf(transform)) return false;
            }
            return true;
        }

        // ------------------------------------------------------------------ States

        protected void SetState(EnemyState state)
        {
            if (State == EnemyState.Dead) return;
            if (State == state) return;
            var previous = State;
            State = state;
            stateEnterTime = Time.time;
            if (alertIndicator != null) alertIndicator.SetActive(state == EnemyState.Alert);
            if (state == EnemyState.Alert && previous != EnemyState.Stunned) AudioManager.Play(SoundId.EnemyAlert, EyePosition, 0.6f);
            OnStateChanged(previous, state);
        }

        protected virtual void OnStateChanged(EnemyState previous, EnemyState current) { }

        protected virtual void UpdateIdle()
        {
            StopMoving();
            if (canSeeTarget)
            {
                SetState(EnemyState.Alert);
                return;
            }
            if (patrolRadius > 0.5f && TimeInState > 2f) SetState(EnemyState.Patrol);
        }

        protected virtual void UpdatePatrol()
        {
            if (canSeeTarget)
            {
                SetState(EnemyState.Alert);
                return;
            }
            if (stationary)
            {
                StopMoving();
                return;
            }
            if (Time.time >= nextPatrolTime || HorizontalDistance(transform.position, patrolTarget) < 1f)
            {
                if (Time.time >= nextPatrolTime)
                {
                    Vector2 r = Random.insideUnitCircle * patrolRadius;
                    patrolTarget = spawnPosition + new Vector3(r.x, 0f, r.y);
                    if (flying) patrolTarget.y = spawnPosition.y;
                    nextPatrolTime = Time.time + Random.Range(3f, 6f);
                }
                else
                {
                    StopMoving();
                    return;
                }
            }
            MoveTo(patrolTarget, definition != null ? definition.moveSpeed : 3f);
        }

        protected virtual void UpdateAlert()
        {
            StopMoving();
            FacePoint(lastKnownTargetPos);
            float reaction = definition != null ? definition.reactionTime : 0.4f;
            if (TimeInState >= reaction)
            {
                nextAttackTime = Mathf.Max(nextAttackTime, Time.time + Random.Range(0.1f, 0.4f));
                SetState(EnemyState.Chase);
            }
        }

        protected virtual void UpdateChase()
        {
            var target = Target;
            if (target == null)
            {
                SetState(EnemyState.Search);
                return;
            }
            float dist = DistanceToTarget;
            if (canSeeTarget && dist <= definition.attackRange)
            {
                SetState(EnemyState.Attack);
                return;
            }
            if (!canSeeTarget && Time.time - lastSeenTime > 3f)
            {
                SetState(EnemyState.Search);
                return;
            }
            Vector3 destination = canSeeTarget ? target.transform.position : lastKnownTargetPos;
            MoveTo(destination, definition.chaseSpeed);
            if (canSeeTarget) FacePoint(target.AimPoint);
        }

        protected abstract void UpdateAttack();

        protected virtual void UpdateSearch()
        {
            if (canSeeTarget)
            {
                SetState(EnemyState.Chase);
                return;
            }
            if (HorizontalDistance(transform.position, lastKnownTargetPos) > 1.5f && !stationary)
            {
                MoveTo(lastKnownTargetPos, definition.moveSpeed);
            }
            else
            {
                StopMoving();
                float yaw = Mathf.Sin(TimeInState * 1.3f) * 90f;
                FacePoint(transform.position + Quaternion.Euler(0f, yaw, 0f) * Vector3.forward * 5f);
            }
            if (TimeInState > definition.searchDuration) SetState(patrolRadius > 0.5f ? EnemyState.Patrol : EnemyState.Idle);
        }

        protected virtual void UpdateStunned()
        {
            StopMoving();
            if (!IsStunned) SetState(Target != null ? EnemyState.Chase : EnemyState.Search);
        }

        // ------------------------------------------------------------------ Movement helpers

        protected void MoveTo(Vector3 destination, float speed)
        {
            if (stationary)
            {
                desiredVelocity = Vector3.zero;
                return;
            }
            if (flying)
            {
                Vector3 to = destination - transform.position;
                desiredVelocity = to.sqrMagnitude > 0.25f ? to.normalized * speed : Vector3.zero;
                return;
            }
            Vector3 steer = navigator.GetSteerTarget(transform.position, destination);
            Vector3 dir = steer - transform.position;
            dir.y = 0f;
            desiredVelocity = dir.sqrMagnitude > 0.04f ? dir.normalized * speed : Vector3.zero;
        }

        protected void StopMoving()
        {
            desiredVelocity = Vector3.zero;
        }

        protected void FacePoint(Vector3 point)
        {
            lookTarget = point;
            hasLookTarget = true;
        }

        void GroundMove(float dt)
        {
            Vector3 vel = rb.velocity;
            Vector3 horizontal = new Vector3(vel.x, 0f, vel.z);
            if (IsStunned)
            {
                if (IsGrounded()) horizontal = Vector3.MoveTowards(horizontal, Vector3.zero, 8f * dt);
            }
            else
            {
                float accel = accelerationOverride > 0f ? accelerationOverride : (definition != null ? definition.acceleration : 25f);
                horizontal = Vector3.MoveTowards(horizontal, desiredVelocity, accel * dt);
            }
            rb.velocity = new Vector3(horizontal.x, vel.y, horizontal.z);
            RotateBody(dt);
        }

        void FlyingMove(float dt)
        {
            Vector3 vel = rb.velocity;
            if (IsStunned)
            {
                vel = Vector3.MoveTowards(vel, Vector3.zero, 6f * dt);
            }
            else
            {
                Vector3 desired = desiredVelocity;
                // Simple obstacle avoidance.
                if (desired.sqrMagnitude > 0.01f &&
                    Physics.SphereCast(transform.position, 0.6f, desired.normalized, out RaycastHit hit, 3f, Layers.SolidMask, QueryTriggerInteraction.Ignore))
                {
                    desired += (hit.normal + Vector3.up) * desired.magnitude;
                }
                desired.y += Mathf.Sin(Time.time * 2f + spawnPosition.x) * 0.6f;
                float accel = definition != null ? definition.acceleration : 15f;
                vel = Vector3.MoveTowards(vel, desired, accel * dt);
            }
            rb.velocity = vel;
            RotateBody(dt);
        }

        void RotateBody(float dt)
        {
            Vector3 facing;
            if (hasLookTarget) facing = lookTarget - transform.position;
            else facing = desiredVelocity;
            facing.y = 0f;
            if (facing.sqrMagnitude < 0.01f) return;
            float turn = definition != null ? definition.turnSpeed : 360f;
            var targetRot = Quaternion.LookRotation(facing.normalized, Vector3.up);
            rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, targetRot, turn * dt));
        }

        protected virtual void UpdateAimPivot()
        {
            if (aimPivot == null) return;
            Vector3 target = hasLookTarget ? lookTarget : aimPivot.position + transform.forward;
            Vector3 dir = target - aimPivot.position;
            if (dir.sqrMagnitude < 0.001f) return;
            var desired = Quaternion.LookRotation(dir.normalized, Vector3.up);
            float turn = definition != null ? definition.turnSpeed : 360f;
            aimPivot.rotation = Quaternion.RotateTowards(aimPivot.rotation, desired, turn * 1.5f * Time.deltaTime);
        }

        protected bool IsGrounded()
        {
            return Physics.Raycast(transform.position + Vector3.up * 0.2f, Vector3.down, 0.4f, Layers.SolidMask, QueryTriggerInteraction.Ignore);
        }

        protected static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        // ------------------------------------------------------------------ Combat helpers

        /// <summary>Aim direction from the muzzle toward the target with lead and inaccuracy.</summary>
        protected Vector3 AimDirection(float projectileSpeed, float extraInaccuracy = 0f)
        {
            var target = Target;
            Vector3 from = MuzzlePosition;
            if (target == null) return muzzle != null ? muzzle.forward : transform.forward;
            Vector3 predicted = MathUtil.PredictPosition(from, target.AimPoint, target.Velocity, projectileSpeed, definition.leadFactor);
            Vector3 dir = (predicted - from).normalized;
            return MathUtil.RandomInCone(dir, definition.inaccuracy + extraInaccuracy);
        }

        protected Projectile FireProjectile(Vector3 direction, float speed, float damage, float knockback)
        {
            var prefab = projectilePrefab;
            if (prefab == null && PrefabRegistry.Instance != null) prefab = PrefabRegistry.Instance.enemyBullet;
            if (prefab == null) return null;
            var go = PoolManager.Spawn(prefab, MuzzlePosition, Quaternion.LookRotation(direction));
            var projectile = go != null ? go.GetComponent<Projectile>() : null;
            if (projectile == null) return null;
            var data = ProjectileData.Bullet(direction, speed, damage, gameObject, false);
            data.knockback = knockback;
            projectile.Launch(data);
            SpawnMuzzleFlash();
            AudioManager.Play(SoundId.EnemyShot, MuzzlePosition, 0.5f);
            return projectile;
        }

        protected void SpawnMuzzleFlash()
        {
            var registry = PrefabRegistry.Instance;
            if (registry != null && registry.muzzleFlash != null && muzzle != null)
            {
                PoolManager.Spawn(registry.muzzleFlash, muzzle.position, muzzle.rotation);
            }
        }

        // ------------------------------------------------------------------ Damage / knockback

        public virtual void ApplyKnockback(Vector3 velocityChange, float stunDuration)
        {
            if (stationary || rb == null || rb.isKinematic) return;
            float multiplier = definition != null ? definition.knockbackMultiplier : 1f;
            if (multiplier <= 0f) return;
            rb.AddForce(velocityChange * multiplier, ForceMode.VelocityChange);
            if (stunDuration > 0f && State != EnemyState.Dead) Stun(stunDuration);
        }

        public void Stun(float duration)
        {
            if (State == EnemyState.Dead) return;
            stunnedUntil = Mathf.Max(stunnedUntil, Time.time + duration);
            SetState(EnemyState.Stunned);
        }

        protected virtual void OnDamaged(DamageInfo info)
        {
            if (hitFlash != null) hitFlash.Flash();
            if (State == EnemyState.Idle || State == EnemyState.Patrol || State == EnemyState.Search)
            {
                var target = Target;
                lastKnownTargetPos = info.fromPlayer && target != null ? target.AimPoint : info.point;
                lastSeenTime = Time.time;
                SetState(EnemyState.Alert);
            }
        }

        void OnNoise(Vector3 position, float radius)
        {
            if (State != EnemyState.Idle && State != EnemyState.Patrol && State != EnemyState.Search) return;
            if ((position - transform.position).sqrMagnitude > radius * radius) return;
            lastKnownTargetPos = position;
            SetState(State == EnemyState.Search ? EnemyState.Chase : EnemyState.Alert);
        }

        void OnPlayerRespawned(PlayerController player)
        {
            if (State == EnemyState.Dead) return;
            canSeeTarget = false;
            lastSeenTime = -999f;
            navigator.Clear();
            SetState(patrolRadius > 0.5f ? EnemyState.Patrol : EnemyState.Idle);
        }

        void OnDied(DamageInfo info)
        {
            if (State == EnemyState.Dead) return;
            SetState(EnemyState.Dead);
            All.Remove(this);
            if (registeredWithArena) ArenaRegistry.NotifyDeath(arenaId, this);
            GameEvents.RaiseEnemyKilled(this);
            OnDeath(info);
        }

        /// <summary>Death presentation: effect, sound, drops and breaking the visual model into physics debris.</summary>
        protected virtual void OnDeath(DamageInfo info)
        {
            var registry = PrefabRegistry.Instance;
            Vector3 center = transform.position + Vector3.up * 1f;
            if (registry != null && registry.enemyDeath != null) PoolManager.Spawn(registry.enemyDeath, center, Quaternion.identity);
            AudioManager.Play(SoundId.EnemyDeath, center, 0.8f);
            AudioManager.Play2D(SoundId.KillConfirm, 0.6f);
            DropLoot(registry);
            BreakApart(info);
            Destroy(gameObject);
        }

        void DropLoot(PrefabRegistry registry)
        {
            if (registry == null || definition == null) return;
            GameObject drop = null;
            float roll = Random.value;
            if (roll < definition.healthDropChance) drop = registry.GetLevelObjectPrefab("HealthPickup");
            else if (roll < definition.healthDropChance + definition.ammoDropChance) drop = registry.GetLevelObjectPrefab("AmmoPickup");
            if (drop == null) return;
            Vector3 pos = transform.position;
            if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out RaycastHit hit, 30f, Layers.SolidMask, QueryTriggerInteraction.Ignore)) pos = hit.point;
            var go = Instantiate(drop, pos, Quaternion.identity, transform.parent);
            var pickup = go.GetComponent<Weapons.PickupBase>();
            if (pickup != null)
            {
                var data = new LevelObjectData { objectType = "Drop" };
                data.Set("respawn", false);
                pickup.ApplyLevelProperties(data);
            }
        }

        protected void BreakApart(DamageInfo info)
        {
            var root = visualRoot != null ? visualRoot : transform;
            var renderers = root.GetComponentsInChildren<MeshRenderer>();
            Vector3 force = info.direction * Mathf.Clamp(info.knockback + 4f, 4f, 18f);
            foreach (var r in renderers)
            {
                var piece = r.gameObject;
                if (piece == gameObject) continue;
                piece.transform.SetParent(null, true);
                foreach (var c in piece.GetComponents<Collider>()) Destroy(c);
                foreach (var mb in piece.GetComponents<MonoBehaviour>()) Destroy(mb);
                piece.layer = Layers.Debris;
                var box = piece.AddComponent<BoxCollider>();
                box.size = Vector3.Max(box.size, Vector3.one * 0.05f);
                var body = piece.AddComponent<Rigidbody>();
                body.mass = 0.4f;
                body.velocity = rb != null ? rb.velocity : Vector3.zero;
                body.AddForce(force + Random.insideUnitSphere * 3f + Vector3.up * 3f, ForceMode.VelocityChange);
                body.AddTorque(Random.insideUnitSphere * 10f, ForceMode.VelocityChange);
                Destroy(piece, Random.Range(3f, 5f));
            }
        }

        // ------------------------------------------------------------------ Level data

        public void ApplyLevelProperties(LevelObjectData data)
        {
            arenaId = data.GetString("arena", arenaId);
            patrolRadius = data.GetFloat("patrol", patrolRadius);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            All.Clear();
        }
    }
}

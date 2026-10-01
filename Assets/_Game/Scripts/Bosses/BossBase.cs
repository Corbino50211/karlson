using System;
using System.Collections;
using System.Collections.Generic;
using Momentum.Audio;
using Momentum.Levels;
using Momentum.PlayerSystems;
using UnityEngine;

namespace Momentum.Bosses
{
    public enum BossType
    {
        Warden = 0,
        Sentinel = 1,
        Juggernaut = 2,
        Core = 3
    }

    /// <summary>One entry in a boss's attack table.</summary>
    [Serializable]
    public class BossAttack
    {
        public string id = "attack";
        [Tooltip("Seconds before this attack can be used again.")]
        public float cooldown = 5f;
        [Tooltip("First phase (0-based) in which this attack is available.")]
        public int minPhase;
        public int maxPhase = 99;
        public float weight = 1f;
        public float minRange;
        public float maxRange = 999f;
        [NonSerialized] public float readyTime;

        public BossAttack() { }

        public BossAttack(string id, float cooldown, int minPhase, float weight, float minRange = 0f, float maxRange = 999f)
        {
            this.id = id;
            this.cooldown = cooldown;
            this.minPhase = minPhase;
            this.weight = weight;
            this.minRange = minRange;
            this.maxRange = maxRange;
        }
    }

    /// <summary>
    /// Shared boss framework: activation by arena trigger, health bar events, phase thresholds,
    /// cooldown-driven weighted attack selection, telegraph/projectile/shockwave helpers, reset when the
    /// player dies, and a multi-explosion death sequence. Subclasses implement PerformAttack.
    /// </summary>
    [RequireComponent(typeof(BossHealth))]
    public abstract class BossBase : MonoBehaviour, ILevelObjectConfigurable
    {
        public static readonly List<BossBase> All = new List<BossBase>();

        [Header("Boss")]
        [SerializeField] protected string bossName = "BOSS";
        [SerializeField] protected BossType bossType;
        [SerializeField] protected float maxHealth = 2000f;
        [Tooltip("Health fractions at which the next phase begins (descending).")]
        [SerializeField] protected float[] phaseThresholds = { 0.66f, 0.33f };
        [SerializeField] protected float globalCooldown = 1.2f;
        [SerializeField] protected List<BossAttack> attacks = new List<BossAttack>();
        [SerializeField] protected float arenaRadius = 30f;

        [Header("Presentation")]
        [SerializeField] protected Transform visualRoot;
        [SerializeField] protected float introDuration = 1.8f;
        [SerializeField] protected float deathDuration = 3f;

        protected BossHealth health;
        protected HitFlash hitFlash;
        protected Rigidbody body;
        protected Vector3 arenaCenter;
        protected Vector3 homePosition;
        protected Quaternion homeRotation;
        protected bool busy;
        protected float nextAttackTime;
        readonly List<GameObject> spawned = new List<GameObject>();
        Coroutine mainRoutine;
        bool arenaAssigned;
        bool started;

        public string BossName => bossName;
        public BossType Type => bossType;
        public Health Health => health;
        public int Phase { get; protected set; }
        public bool IsActive { get; private set; }
        public bool IsDefeated { get; private set; }
        public float HealthNormalized => health != null ? health.Normalized : 0f;
        public float ArenaRadius => arenaRadius;
        public Vector3 ArenaCenter => arenaCenter;

        /// <summary>Last commanded body position (use instead of transform.position, which is interpolated).</summary>
        protected Vector3 BodyPosition { get; private set; }

        /// <summary>Damage taken multiplier (e.g. 2x while stunned after crashing into a wall).</summary>
        public float DamageTakenMultiplier { get; protected set; } = 1f;

        /// <summary>Label shown under the boss health bar.</summary>
        public virtual string PhaseLabel => "PHASE " + (Phase + 1);

        protected virtual float CooldownMultiplier => 1f;

        protected PlayerController Target
        {
            get
            {
                var p = PlayerController.Current;
                return p != null && !p.IsDead ? p : null;
            }
        }

        protected float DistanceToTarget
        {
            get
            {
                var t = Target;
                if (t == null) return float.PositiveInfinity;
                Vector3 a = transform.position;
                Vector3 b = t.transform.position;
                a.y = b.y = 0f;
                return Vector3.Distance(a, b);
            }
        }

        // ------------------------------------------------------------------ Lifecycle

        protected virtual void Awake()
        {
            health = GetComponent<BossHealth>();
            hitFlash = GetComponent<HitFlash>();
            body = GetComponent<Rigidbody>();
            if (attacks == null || attacks.Count == 0) PopulateDefaultAttacks();
        }

        protected virtual void OnEnable()
        {
            if (!All.Contains(this)) All.Add(this);
            health.Damaged += OnDamaged;
            health.Died += OnDied;
            GameEvents.PlayerRespawned += OnPlayerRespawned;
        }

        protected virtual void OnDisable()
        {
            All.Remove(this);
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
            GameEvents.PlayerRespawned -= OnPlayerRespawned;
        }

        protected virtual void Start()
        {
            started = true;
            homePosition = transform.position;
            homeRotation = transform.rotation;
            BodyPosition = homePosition;
            if (!arenaAssigned) arenaCenter = homePosition;
            health.Owner = this;
            health.SetMaxHealth(maxHealth, true);
            health.Invulnerable = true;
            if (body != null)
            {
                body.isKinematic = true;
                body.interpolation = RigidbodyInterpolation.Interpolate;
            }
        }

        /// <summary>Fills the attack table with this boss's defaults (also used by the prefab generator).</summary>
        public abstract void PopulateDefaultAttacks();

        public void SetArena(Vector3 center, float radius)
        {
            arenaCenter = center;
            arenaRadius = radius;
            arenaAssigned = true;
        }

        // ------------------------------------------------------------------ Activation & loop

        public void Activate()
        {
            if (IsActive || IsDefeated || !started) return;
            IsActive = true;
            Phase = 0;
            busy = false;
            DamageTakenMultiplier = 1f;
            nextAttackTime = Time.time + 0.5f;
            foreach (var a in attacks) a.readyTime = 0f;
            GameEvents.RaiseBossStarted(this);
            GameEvents.RaiseBossHealthChanged(this);
            AudioManager.PlayMusic(MusicTrack.Boss);
            GameEvents.RaiseNotification(bossName, GameConfig.Instance.bossColor);
            mainRoutine = StartCoroutine(MainLoop());
        }

        IEnumerator MainLoop()
        {
            yield return StartCoroutine(Intro());
            health.Invulnerable = false;
            OnFightStarted();
            yield return StartCoroutine(FightRoutine());
        }

        protected virtual IEnumerator Intro()
        {
            AudioManager.Play(SoundId.BossRoar, transform.position, 1f);
            CameraShaker.Shake(0.5f);
            yield return new WaitForSeconds(introDuration);
        }

        protected virtual void OnFightStarted() { }

        /// <summary>Default fight: pick weighted attacks whose cooldown, phase and range allow it.</summary>
        protected virtual IEnumerator FightRoutine()
        {
            while (!IsDefeated)
            {
                if (!busy && Time.time >= nextAttackTime && Target != null)
                {
                    var attack = ChooseAttack();
                    if (attack != null)
                    {
                        busy = true;
                        attack.readyTime = Time.time + attack.cooldown * CooldownMultiplier;
                        yield return StartCoroutine(PerformAttack(attack.id));
                        busy = false;
                        nextAttackTime = Time.time + globalCooldown * CooldownMultiplier;
                    }
                }
                yield return null;
            }
        }

        protected virtual BossAttack ChooseAttack()
        {
            float dist = DistanceToTarget;
            float total = 0f;
            foreach (var a in attacks)
            {
                if (IsAvailable(a, dist)) total += a.weight;
            }
            if (total <= 0f) return null;
            float roll = UnityEngine.Random.value * total;
            foreach (var a in attacks)
            {
                if (!IsAvailable(a, dist)) continue;
                roll -= a.weight;
                if (roll <= 0f) return a;
            }
            return null;
        }

        protected BossAttack FindAttack(string id)
        {
            foreach (var a in attacks)
            {
                if (a.id == id) return a;
            }
            return null;
        }

        bool IsAvailable(BossAttack a, float dist)
        {
            return a != null && a.weight > 0f && Phase >= a.minPhase && Phase <= a.maxPhase && Time.time >= a.readyTime &&
                   dist >= a.minRange && dist <= a.maxRange;
        }

        protected abstract IEnumerator PerformAttack(string attackId);

        protected virtual void Update()
        {
            if (IsActive && !IsDefeated) UpdateBoss(Time.deltaTime);
        }

        /// <summary>Per-frame behaviour while active (movement between attacks, facing).</summary>
        protected virtual void UpdateBoss(float dt) { }

        // ------------------------------------------------------------------ Damage & phases

        void OnDamaged(DamageInfo info)
        {
            if (hitFlash != null) hitFlash.Flash();
            GameEvents.RaiseBossHealthChanged(this);
            CheckPhase();
        }

        protected void CheckPhase()
        {
            if (phaseThresholds == null) return;
            int newPhase = 0;
            foreach (float t in phaseThresholds)
            {
                if (health.Normalized <= t) newPhase++;
            }
            if (newPhase > Phase)
            {
                Phase = newPhase;
                OnPhaseChanged(Phase);
                GameEvents.RaiseBossPhaseChanged(this, Phase);
                GameEvents.RaiseNotification(PhaseLabel, GameConfig.Instance.bossColor);
                AudioManager.Play(SoundId.BossRoar, transform.position, 0.8f, 1.15f);
            }
        }

        protected virtual void OnPhaseChanged(int phase) { }

        void OnDied(DamageInfo info)
        {
            if (IsDefeated) return;
            IsDefeated = true;
            busy = true;
            StopAllCoroutines();
            OnDefeatedCleanup();
            CleanupSpawned();
            StartCoroutine(DeathSequence());
        }

        protected virtual void OnDefeatedCleanup() { }

        IEnumerator DeathSequence()
        {
            GameEvents.RaiseBossHealthChanged(this);
            AudioManager.Play(SoundId.BossDeath, transform.position, 1f);
            var registry = PrefabRegistry.Instance;
            Bounds bounds = GetVisualBounds();
            float t = 0f;
            while (t < deathDuration)
            {
                if (registry != null && registry.smallExplosion != null)
                {
                    Vector3 p = new Vector3(
                        UnityEngine.Random.Range(bounds.min.x, bounds.max.x),
                        UnityEngine.Random.Range(bounds.min.y, bounds.max.y),
                        UnityEngine.Random.Range(bounds.min.z, bounds.max.z));
                    PoolManager.Spawn(registry.smallExplosion, p, Quaternion.identity);
                    AudioManager.Play(SoundId.Explosion, p, 0.4f, UnityEngine.Random.Range(0.8f, 1.2f));
                }
                CameraShaker.Shake(0.15f);
                float wait = 0.18f;
                t += wait;
                yield return new WaitForSeconds(wait);
            }

            var final = ExplosionParams.Create(bounds.center, 8f, 0f, 14f, gameObject, false);
            final.effectScale = 2.5f;
            final.cameraShake = 1f;
            final.sound = SoundId.BossDeath;
            final.ignoreRoot = transform;
            Explosions.Explode(final);
            BreakVisuals();

            IsActive = false;
            GameEvents.RaiseBossDefeated(this);
            GameEvents.RaiseNotification(bossName + " DEFEATED", GameConfig.Instance.successColor);
            AudioManager.PlayMusic(MusicTrack.Level);
            foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
            gameObject.SetActive(false);
        }

        Bounds GetVisualBounds()
        {
            var renderers = (visualRoot != null ? visualRoot : transform).GetComponentsInChildren<MeshRenderer>();
            if (renderers.Length == 0) return new Bounds(transform.position + Vector3.up * 2f, Vector3.one * 3f);
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            return b;
        }

        void BreakVisuals()
        {
            var root = visualRoot != null ? visualRoot : transform;
            foreach (var r in root.GetComponentsInChildren<MeshRenderer>())
            {
                var piece = r.gameObject;
                if (piece == gameObject) continue;
                piece.transform.SetParent(null, true);
                foreach (var c in piece.GetComponents<Collider>()) Destroy(c);
                foreach (var mb in piece.GetComponents<MonoBehaviour>()) Destroy(mb);
                piece.layer = Layers.Debris;
                piece.AddComponent<BoxCollider>();
                var rb = piece.AddComponent<Rigidbody>();
                rb.mass = 2f;
                rb.AddForce(UnityEngine.Random.insideUnitSphere * 10f + Vector3.up * 8f, ForceMode.VelocityChange);
                rb.AddTorque(UnityEngine.Random.insideUnitSphere * 6f, ForceMode.VelocityChange);
                Destroy(piece, UnityEngine.Random.Range(4f, 7f));
            }
        }

        // ------------------------------------------------------------------ Reset

        void OnPlayerRespawned(PlayerController player)
        {
            if (IsActive && !IsDefeated) ResetBoss();
        }

        /// <summary>Restores the boss to its pre-fight state (player died during the fight).</summary>
        public virtual void ResetBoss()
        {
            StopAllCoroutines();
            busy = false;
            IsActive = false;
            Phase = 0;
            DamageTakenMultiplier = 1f;
            CleanupSpawned();
            OnReset();
            if (body != null)
            {
                body.position = homePosition;
                body.rotation = homeRotation;
            }
            transform.SetPositionAndRotation(homePosition, homeRotation);
            BodyPosition = homePosition;
            health.ResetHealth();
            health.Invulnerable = true;
            foreach (var a in attacks) a.readyTime = 0f;
            GameEvents.RaiseBossReset(this);
            AudioManager.PlayMusic(MusicTrack.Level);
        }

        protected virtual void OnReset() { }

        // ------------------------------------------------------------------ Helpers

        protected T Track<T>(T obj) where T : Component
        {
            if (obj != null) spawned.Add(obj.gameObject);
            return obj;
        }

        protected GameObject Track(GameObject obj)
        {
            if (obj != null) spawned.Add(obj);
            return obj;
        }

        protected void Untrack(GameObject obj)
        {
            spawned.Remove(obj);
        }

        protected void CleanupSpawned()
        {
            foreach (var go in spawned)
            {
                if (go == null) continue;
                if (go.GetComponent<PooledObject>() != null) PoolManager.Despawn(go);
                else Destroy(go);
            }
            spawned.Clear();
        }

        protected Vector3 ClampToArena(Vector3 position, float margin = 2f)
        {
            Vector3 offset = position - arenaCenter;
            offset.y = 0f;
            float max = Mathf.Max(1f, arenaRadius - margin);
            if (offset.magnitude > max) offset = offset.normalized * max;
            return new Vector3(arenaCenter.x + offset.x, position.y, arenaCenter.z + offset.z);
        }

        protected bool IsOutsideArena(Vector3 position, float margin = 1f)
        {
            Vector3 offset = position - arenaCenter;
            offset.y = 0f;
            return offset.magnitude > arenaRadius - margin;
        }

        protected Vector3 TargetFeet => Target != null ? Target.FeetPosition : arenaCenter;

        protected TelegraphIndicator TelegraphCircle(Vector3 center, float radius, float duration, Color color)
        {
            var registry = PrefabRegistry.Instance;
            if (registry == null || registry.telegraphDisc == null) return null;
            var go = PoolManager.Spawn(registry.telegraphDisc, center, Quaternion.identity);
            var t = go != null ? go.GetComponent<TelegraphIndicator>() : null;
            if (t != null) t.Show(center + Vector3.up * 0.05f, Quaternion.identity, new Vector3(radius * 2f, 0.02f, radius * 2f), duration, color);
            return Track(t);
        }

        protected TelegraphIndicator TelegraphLane(Vector3 start, Vector3 direction, float length, float width, float duration, Color color)
        {
            var registry = PrefabRegistry.Instance;
            if (registry == null || registry.telegraphLine == null) return null;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f) direction = transform.forward;
            direction.Normalize();
            var go = PoolManager.Spawn(registry.telegraphLine, start, Quaternion.identity);
            var t = go != null ? go.GetComponent<TelegraphIndicator>() : null;
            if (t != null) t.Show(start + direction * length * 0.5f + Vector3.up * 0.05f, Quaternion.LookRotation(direction), new Vector3(width, 0.02f, length), duration, color);
            return Track(t);
        }

        protected Projectile FireProjectile(GameObject prefab, Vector3 from, Vector3 direction, float speed, float damage, float knockback)
        {
            if (prefab == null) return null;
            var go = PoolManager.Spawn(prefab, from, Quaternion.LookRotation(direction));
            var p = go != null ? go.GetComponent<Projectile>() : null;
            if (p == null) return null;
            var data = ProjectileData.Bullet(direction, speed, damage, gameObject, false);
            data.knockback = knockback;
            p.Launch(data);
            return p;
        }

        protected Projectile FireProjectile(GameObject prefab, ProjectileData data, Vector3 from)
        {
            if (prefab == null) return null;
            var go = PoolManager.Spawn(prefab, from, Quaternion.LookRotation(data.direction));
            var p = go != null ? go.GetComponent<Projectile>() : null;
            if (p == null) return null;
            data.owner = gameObject;
            p.Launch(data);
            return Track(p);
        }

        protected Shockwave SpawnShockwave(Vector3 center, float maxRadius, float speed, float height, float baseOffset, float damage, float knockback, Color color)
        {
            var registry = PrefabRegistry.Instance;
            if (registry == null || registry.shockwave == null) return null;
            var go = PoolManager.Spawn(registry.shockwave, center, Quaternion.identity);
            var s = go != null ? go.GetComponent<Shockwave>() : null;
            if (s != null) s.Launch(center, maxRadius, speed, height, baseOffset, damage, knockback, color, gameObject);
            AudioManager.Play(SoundId.Shockwave, center, 0.9f);
            return Track(s);
        }

        protected LaserBeam SpawnLaser(Vector3 origin, Quaternion rotation, float length, float damage)
        {
            var registry = PrefabRegistry.Instance;
            if (registry == null || registry.laserBeam == null) return null;
            var go = PoolManager.Spawn(registry.laserBeam, origin, rotation);
            var beam = go != null ? go.GetComponent<LaserBeam>() : null;
            if (beam != null) beam.Configure(length, damage, gameObject);
            return Track(beam);
        }

        protected void DespawnTracked(Component c)
        {
            if (c == null) return;
            spawned.Remove(c.gameObject);
            PoolManager.Despawn(c.gameObject);
        }

        /// <summary>Area damage around a point that does not hurt the boss itself.</summary>
        protected void AreaBlast(Vector3 center, float radius, float damage, float force)
        {
            var p = ExplosionParams.Create(center, radius, damage, force, gameObject, false);
            p.ignoreRoot = transform;
            p.selfDamageMultiplier = 1f;
            p.effectScale = radius / 5f;
            Explosions.Explode(p);
        }

        protected void MoveBody(Vector3 position)
        {
            BodyPosition = position;
            if (body != null) body.MovePosition(position);
            else transform.position = position;
        }

        protected void RotateBodyTowards(Vector3 point, float degreesPerSecond, float dt)
        {
            Vector3 dir = point - BodyPosition;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) return;
            var target = Quaternion.LookRotation(dir.normalized);
            var rot = Quaternion.RotateTowards(transform.rotation, target, degreesPerSecond * dt);
            if (body != null) body.MoveRotation(rot);
            else transform.rotation = rot;
        }

        public virtual void ApplyLevelProperties(LevelObjectData data)
        {
            float healthOverride = data.GetFloat("health", 0f);
            if (healthOverride > 0f) maxHealth = healthOverride;
            arenaRadius = data.GetFloat("radius", arenaRadius);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            All.Clear();
        }
    }
}

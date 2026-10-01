using System.Collections;
using System.Collections.Generic;
using Momentum.Audio;
using Momentum.Enemies;
using UnityEngine;

namespace Momentum.Bosses
{
    /// <summary>
    /// THE CORE — final arena boss with four scripted stages.
    /// 1. Defense Grid: shielded core, destroy the turrets.
    /// 2. Laser Grid: core vulnerable, rotating/sweeping lasers (jump the low beam, slide under the high one).
    /// 3. Swarm: shield returns while waves of enemies are summoned.
    /// 4. Overload: core rises out of reach; parkour around the arena to destroy exposed weak points.
    /// Uses BossSocket markers in the level when present, otherwise generates positions around the arena.
    /// </summary>
    public class CoreBoss : BossBase
    {
        enum Stage { Dormant, Turrets, Lasers, Waves, WeakPoints }

        [Header("Core Parts")]
        [SerializeField] GameObject shieldVisual;
        [SerializeField] Transform coreVisual;

        [Header("Stage 1 - Turrets")]
        [SerializeField] int fallbackTurretCount = 4;

        [Header("Stage 2 - Lasers")]
        [Tooltip("Stage 2 ends when core health drops below this fraction.")]
        [SerializeField] float laserStageEndHealth = 0.6f;
        [SerializeField] float lowBeamHeight = 0.8f;
        [SerializeField] float highBeamHeight = 1.6f;
        [SerializeField] float beamDamage = 15f;
        [SerializeField] float lowBeamSpeed = 42f;
        [SerializeField] float highBeamSpeed = -30f;
        [SerializeField] float emitterSweepAngle = 35f;

        [Header("Stage 3 - Waves")]
        [SerializeField] float timeBetweenWaves = 2f;

        [Header("Stage 4 - Weak Points")]
        [SerializeField] float riseHeight = 11f;
        [SerializeField] float weakPointHealth = 160f;
        [SerializeField] int fallbackWeakPoints = 4;
        [SerializeField] float fallbackWeakPointHeight = 9f;

        [Header("Pulse")]
        [SerializeField] float orbSpeed = 18f;
        [SerializeField] float orbDamage = 8f;

        static readonly EnemyType[][] Waves =
        {
            new[] { EnemyType.Gunner, EnemyType.Gunner, EnemyType.Gunner, EnemyType.Drone },
            new[] { EnemyType.Charger, EnemyType.Charger, EnemyType.Shotgunner, EnemyType.Drone },
            new[] { EnemyType.Sniper, EnemyType.Gunner, EnemyType.Shotgunner, EnemyType.Drone, EnemyType.Drone }
        };

        Stage stage = Stage.Dormant;
        float floorY;
        readonly List<EnemyBase> minions = new List<EnemyBase>();
        readonly List<BossWeakPoint> weakPoints = new List<BossWeakPoint>();
        float weakPointDamageShare;
        Coroutine pulseRoutine;

        public override string PhaseLabel
        {
            get
            {
                switch (stage)
                {
                    case Stage.Turrets: return "STAGE 1 - DEFENSE GRID";
                    case Stage.Lasers: return "STAGE 2 - LASER GRID";
                    case Stage.Waves: return "STAGE 3 - SWARM";
                    case Stage.WeakPoints: return "STAGE 4 - OVERLOAD (" + weakPoints.Count + " WEAK POINTS)";
                    default: return "DORMANT";
                }
            }
        }

        protected override void Start()
        {
            base.Start();
            floorY = transform.position.y;
            SetShield(true);
        }

        public override void PopulateDefaultAttacks()
        {
            bossName = "THE CORE";
            bossType = BossType.Core;
            phaseThresholds = new float[0];
            attacks = new List<BossAttack> { new BossAttack("pulse", 3.5f, 0, 1f) };
        }

        protected override void Update()
        {
            base.Update();
            if (coreVisual != null) coreVisual.Rotate(0f, (stage == Stage.Lasers ? 90f : 30f) * Time.deltaTime, 0f, Space.World);
        }

        protected override IEnumerator FightRoutine()
        {
            yield return StartCoroutine(TurretStage());
            yield return StartCoroutine(LaserStage());
            yield return StartCoroutine(WaveStage());
            yield return StartCoroutine(WeakPointStage());
        }

        protected override IEnumerator PerformAttack(string attackId)
        {
            if (attackId == "pulse") yield return StartCoroutine(Pulse());
        }

        void SetStage(Stage s)
        {
            stage = s;
            Phase = Mathf.Max(0, (int)s - 1);
            GameEvents.RaiseBossPhaseChanged(this, Phase);
            GameEvents.RaiseBossHealthChanged(this);
            GameEvents.RaiseNotification(PhaseLabel, GameConfig.Instance.bossColor);
            AudioManager.Play(SoundId.BossRoar, transform.position, 0.8f, 0.8f);
        }

        void SetShield(bool on)
        {
            if (shieldVisual != null) shieldVisual.SetActive(on);
            health.Invulnerable = on;
            if (!on) AudioManager.Play(SoundId.ShieldDown, transform.position, 1f);
        }

        // ------------------------------------------------------------------ Stage 1

        IEnumerator TurretStage()
        {
            SetStage(Stage.Turrets);
            SetShield(true);
            var positions = SocketPositions(BossSocketType.TurretMount, fallbackTurretCount, arenaRadius * 0.55f, 0f);
            foreach (var pos in positions)
            {
                Vector3 face = arenaCenter - pos;
                face.y = 0f;
                var turret = EnemySpawner.Spawn(EnemyType.Turret, pos, Quaternion.LookRotation(face.sqrMagnitude > 0.01f ? -face.normalized : Vector3.forward), transform.parent);
                if (turret != null)
                {
                    Track(turret);
                    minions.Add(turret);
                }
                yield return new WaitForSeconds(0.25f);
            }
            StartPulses(5f);
            while (AnyMinionAlive()) yield return null;
            StopPulses();
            minions.Clear();
            yield return new WaitForSeconds(1f);
        }

        // ------------------------------------------------------------------ Stage 2

        IEnumerator LaserStage()
        {
            SetStage(Stage.Lasers);
            SetShield(false);
            var beams = new List<LaserBeam>();
            var beamSpeeds = new List<float>();
            var beamBaseYaw = new List<float>();
            var beamOrigins = new List<Vector3>();
            var sweeping = new List<bool>();

            Vector3 lowOrigin = new Vector3(arenaCenter.x, floorY + lowBeamHeight, arenaCenter.z);
            Vector3 highOrigin = new Vector3(arenaCenter.x, floorY + highBeamHeight, arenaCenter.z);
            AddBeam(beams, beamSpeeds, beamBaseYaw, beamOrigins, sweeping, lowOrigin, 0f, lowBeamSpeed, false);
            AddBeam(beams, beamSpeeds, beamBaseYaw, beamOrigins, sweeping, highOrigin, 90f, highBeamSpeed, false);

            foreach (var socket in BossSocket.Find(BossSocketType.LaserEmitter, arenaCenter, arenaRadius * 1.5f))
            {
                Vector3 toCenter = arenaCenter - socket.transform.position;
                toCenter.y = 0f;
                float yaw = Mathf.Atan2(toCenter.x, toCenter.z) * Mathf.Rad2Deg;
                AddBeam(beams, beamSpeeds, beamBaseYaw, beamOrigins, sweeping, socket.transform.position, yaw, 0.8f, true);
            }

            AudioManager.Play(SoundId.LaserCharge, transform.position, 1f, 0.7f);
            yield return new WaitForSeconds(1.2f);
            foreach (var b in beams)
            {
                if (b != null) b.SetArmed(true);
            }
            AudioManager.Play(SoundId.LaserFire, transform.position, 1f, 0.6f);
            StartPulses(5f);

            float t = 0f;
            while (health.Normalized > laserStageEndHealth && !IsDefeated)
            {
                t += Time.deltaTime;
                for (int i = 0; i < beams.Count; i++)
                {
                    if (beams[i] == null) continue;
                    float yaw = sweeping[i]
                        ? beamBaseYaw[i] + Mathf.Sin(t * beamSpeeds[i]) * emitterSweepAngle
                        : beamBaseYaw[i] + t * beamSpeeds[i];
                    beams[i].transform.SetPositionAndRotation(beamOrigins[i], Quaternion.Euler(0f, yaw, 0f));
                }
                yield return null;
            }
            StopPulses();
            foreach (var b in beams) DespawnTracked(b);
            yield return new WaitForSeconds(0.8f);
        }

        void AddBeam(List<LaserBeam> beams, List<float> speeds, List<float> baseYaw, List<Vector3> origins, List<bool> sweeping,
            Vector3 origin, float yaw, float speed, bool sweep)
        {
            var beam = SpawnLaser(origin, Quaternion.Euler(0f, yaw, 0f), arenaRadius * 2.2f, beamDamage);
            if (beam == null) return;
            beam.SetArmed(false);
            beams.Add(beam);
            speeds.Add(speed);
            baseYaw.Add(yaw);
            origins.Add(origin);
            sweeping.Add(sweep);
        }

        // ------------------------------------------------------------------ Stage 3

        IEnumerator WaveStage()
        {
            SetStage(Stage.Waves);
            SetShield(true);
            var spawnPoints = SocketPositions(BossSocketType.EnemySpawn, 6, arenaRadius * 0.7f, 0f);
            for (int w = 0; w < Waves.Length; w++)
            {
                GameEvents.RaiseNotification("WAVE " + (w + 1) + " / " + Waves.Length, GameConfig.Instance.bossColor);
                minions.Clear();
                var wave = Waves[w];
                for (int i = 0; i < wave.Length; i++)
                {
                    Vector3 pos = spawnPoints[(i + w) % spawnPoints.Count];
                    if (wave[i] == EnemyType.Drone) pos += Vector3.up * 5f;
                    var enemy = EnemySpawner.Spawn(wave[i], pos, Quaternion.LookRotation(arenaCenter - pos == Vector3.zero ? Vector3.forward : FlatDirection(arenaCenter - pos)), transform.parent);
                    if (enemy != null)
                    {
                        Track(enemy);
                        minions.Add(enemy);
                    }
                    yield return new WaitForSeconds(0.2f);
                }
                while (AnyMinionAlive()) yield return null;
                yield return new WaitForSeconds(timeBetweenWaves);
            }
            minions.Clear();
        }

        // ------------------------------------------------------------------ Stage 4

        IEnumerator WeakPointStage()
        {
            SetStage(Stage.WeakPoints);
            SetShield(true);
            Vector3 start = BodyPosition;
            Vector3 top = start + Vector3.up * riseHeight;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / 2f;
                MoveBody(Vector3.Lerp(start, top, Mathf.SmoothStep(0f, 1f, t)));
                yield return null;
            }

            var registry = PrefabRegistry.Instance;
            var positions = SocketPositions(BossSocketType.WeakPoint, fallbackWeakPoints, arenaRadius * 0.85f, fallbackWeakPointHeight);
            weakPoints.Clear();
            foreach (var pos in positions)
            {
                if (registry == null || registry.weakPoint == null) break;
                var go = Instantiate(registry.weakPoint, pos, Quaternion.identity, transform.parent);
                var wp = go.GetComponent<BossWeakPoint>();
                if (wp == null)
                {
                    Destroy(go);
                    continue;
                }
                wp.SetHealth(weakPointHealth);
                wp.Destroyed += OnWeakPointDestroyed;
                weakPoints.Add(wp);
                Track(go);
            }
            if (weakPoints.Count == 0)
            {
                // No weak point prefab: make the core vulnerable as a fallback so the fight can end.
                SetShield(false);
            }
            weakPointDamageShare = health.Current / Mathf.Max(1, weakPoints.Count);
            GameEvents.RaiseBossHealthChanged(this);

            Vector3 beamOrigin = new Vector3(arenaCenter.x, floorY + lowBeamHeight, arenaCenter.z);
            var beam = SpawnLaser(beamOrigin, Quaternion.identity, arenaRadius * 1.2f, beamDamage);
            if (beam != null) beam.SetArmed(true);
            StartPulses(3.2f);

            float yaw = 0f;
            while (!IsDefeated)
            {
                yaw += lowBeamSpeed * 0.8f * Time.deltaTime;
                if (beam != null) beam.transform.SetPositionAndRotation(beamOrigin, Quaternion.Euler(0f, yaw, 0f));
                yield return null;
            }
        }

        void OnWeakPointDestroyed(BossWeakPoint wp)
        {
            wp.Destroyed -= OnWeakPointDestroyed;
            weakPoints.Remove(wp);
            float damage = weakPoints.Count == 0 ? health.Current + 1f : weakPointDamageShare;
            AudioManager.Play(SoundId.ShieldDown, transform.position, 1f, 1.2f);
            health.ForceDamage(new DamageInfo(damage, DamageType.Explosion, wp.gameObject, wp.transform.position, Vector3.down, true));
            if (!IsDefeated) GameEvents.RaiseNotification(PhaseLabel, GameConfig.Instance.bossColor);
        }

        // ------------------------------------------------------------------ Pulse attack

        void StartPulses(float interval)
        {
            StopPulses();
            pulseRoutine = StartCoroutine(PulseLoop(interval));
        }

        void StopPulses()
        {
            if (pulseRoutine != null) StopCoroutine(pulseRoutine);
            pulseRoutine = null;
        }

        IEnumerator PulseLoop(float interval)
        {
            while (!IsDefeated)
            {
                yield return new WaitForSeconds(interval);
                yield return StartCoroutine(Pulse());
            }
        }

        IEnumerator Pulse()
        {
            var registry = PrefabRegistry.Instance;
            var target = Target;
            if (registry == null || target == null) yield break;
            Vector3 from = BodyPosition + Vector3.up * 1.5f;
            Vector3 dir = (target.AimPoint - from).normalized;
            for (int i = 0; i < 6; i++)
            {
                float offset = Mathf.Lerp(-15f, 15f, i / 5f);
                FireProjectile(registry.energyOrb, from, Quaternion.AngleAxis(offset, Vector3.up) * dir, orbSpeed, orbDamage, 4f);
            }
            AudioManager.Play(SoundId.EnemyShot, from, 1f, 0.5f);
        }

        // ------------------------------------------------------------------ Helpers

        bool AnyMinionAlive()
        {
            foreach (var m in minions)
            {
                if (m != null && !m.IsDead) return true;
            }
            return false;
        }

        static Vector3 FlatDirection(Vector3 v)
        {
            v.y = 0f;
            return v.sqrMagnitude > 0.001f ? v.normalized : Vector3.forward;
        }

        List<Vector3> SocketPositions(BossSocketType type, int fallbackCount, float fallbackRadius, float fallbackHeight)
        {
            var result = new List<Vector3>();
            foreach (var s in BossSocket.Find(type, arenaCenter, arenaRadius * 1.5f)) result.Add(s.transform.position);
            if (result.Count > 0) return result;
            for (int i = 0; i < fallbackCount; i++)
            {
                float a = (i + 0.5f) / fallbackCount * Mathf.PI * 2f;
                result.Add(new Vector3(arenaCenter.x + Mathf.Cos(a) * fallbackRadius, floorY + fallbackHeight, arenaCenter.z + Mathf.Sin(a) * fallbackRadius));
            }
            return result;
        }

        protected override void OnReset()
        {
            StopPulses();
            foreach (var wp in weakPoints)
            {
                if (wp != null) wp.Destroyed -= OnWeakPointDestroyed;
            }
            weakPoints.Clear();
            minions.Clear();
            stage = Stage.Dormant;
            SetShield(true);
        }

        protected override void OnDefeatedCleanup()
        {
            StopPulses();
            weakPoints.Clear();
            if (shieldVisual != null) shieldVisual.SetActive(false);
        }
    }
}

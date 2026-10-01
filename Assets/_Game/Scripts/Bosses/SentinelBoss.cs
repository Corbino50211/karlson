using System.Collections;
using System.Collections.Generic;
using Momentum.Audio;
using Momentum.Enemies;
using Momentum.Levels;
using UnityEngine;

namespace Momentum.Bosses
{
    /// <summary>
    /// THE SENTINEL — floating mechanical eye.
    /// Attacks: sweeping floor laser (jump it), orb volleys, drone summons, rotating ground beams
    /// (keep jumping / get on high ground) and electrified arena floor panels.
    /// </summary>
    public class SentinelBoss : BossBase
    {
        [Header("Sentinel Parts")]
        [SerializeField] Transform eye;
        [SerializeField] Transform ringA;
        [SerializeField] Transform ringB;

        [Header("Hover")]
        [SerializeField] float hoverHeight = 9f;
        [SerializeField] float moveSpeed = 7f;

        [Header("Laser Sweep")]
        [SerializeField] float sweepDuration = 2.4f;
        [SerializeField] float sweepArc = 120f;
        [SerializeField] float sweepDamage = 12f;

        [Header("Orbs")]
        [SerializeField] int orbsPerVolley = 5;
        [SerializeField] float orbSpeed = 20f;
        [SerializeField] float orbDamage = 9f;
        [SerializeField] float orbSpread = 14f;

        [Header("Drones")]
        [SerializeField] int maxDrones = 4;

        [Header("Rotating Beams")]
        [SerializeField] float beamHeight = 1f;
        [SerializeField] float beamDuration = 6.5f;
        [SerializeField] float beamDamage = 14f;

        [Header("Arena Hazards")]
        [SerializeField] float hazardWarn = 1.2f;
        [SerializeField] float hazardActive = 2.6f;

        static readonly Color StrikeColor = new Color(1f, 0.3f, 0.1f);

        readonly List<EnemyBase> drones = new List<EnemyBase>();
        float floorY;
        Vector3 waypoint;
        float nextWaypointTime;

        protected override float CooldownMultiplier => Phase >= 2 ? 0.7f : 1f;

        protected override void Start()
        {
            base.Start();
            floorY = transform.position.y;
            waypoint = HoverPoint(arenaCenter);
        }

        public override void PopulateDefaultAttacks()
        {
            bossName = "THE SENTINEL";
            bossType = BossType.Sentinel;
            phaseThresholds = new[] { 0.66f, 0.33f };
            attacks = new List<BossAttack>
            {
                new BossAttack("sweep", 7f, 0, 1.3f),
                new BossAttack("volley", 4f, 0, 1.5f),
                new BossAttack("drones", 14f, 0, 0.8f),
                new BossAttack("beams", 12f, 1, 1.4f),
                new BossAttack("hazards", 10f, 1, 1.2f)
            };
        }

        Vector3 HoverPoint(Vector3 horizontal)
        {
            return new Vector3(horizontal.x, floorY + hoverHeight, horizontal.z);
        }

        protected override IEnumerator Intro()
        {
            AudioManager.Play(SoundId.BossRoar, transform.position, 1f, 1.3f);
            Vector3 start = BodyPosition;
            Vector3 end = HoverPoint(start);
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / Mathf.Max(0.1f, introDuration);
                MoveBody(Vector3.Lerp(start, end, Mathf.SmoothStep(0f, 1f, t)));
                yield return null;
            }
        }

        protected override void Update()
        {
            base.Update();
            float spin = IsActive ? (Phase >= 2 ? 160f : 90f) : 25f;
            if (ringA != null) ringA.Rotate(spin * Time.deltaTime, 0f, 0f, Space.Self);
            if (ringB != null) ringB.Rotate(0f, 0f, -spin * 0.7f * Time.deltaTime, Space.Self);
        }

        protected override void UpdateBoss(float dt)
        {
            var target = Target;
            if (eye != null && target != null)
            {
                Vector3 look = target.AimPoint - eye.position;
                if (look.sqrMagnitude > 0.01f) eye.rotation = Quaternion.Slerp(eye.rotation, Quaternion.LookRotation(look), MathUtil.Damp(6f, dt));
            }
            if (busy) return;
            if (Time.time >= nextWaypointTime)
            {
                Vector2 r = Random.insideUnitCircle * arenaRadius * 0.45f;
                waypoint = HoverPoint(arenaCenter + new Vector3(r.x, 0f, r.y));
                nextWaypointTime = Time.time + Random.Range(4f, 6f);
            }
            Vector3 bob = Vector3.up * Mathf.Sin(Time.time * 1.5f) * 0.5f;
            MoveBody(Vector3.MoveTowards(BodyPosition, waypoint + bob, moveSpeed * dt));
            if (target != null) RotateBodyTowards(target.transform.position, 90f, dt);
        }

        protected override IEnumerator PerformAttack(string attackId)
        {
            switch (attackId)
            {
                case "sweep": yield return StartCoroutine(Sweep()); break;
                case "volley": yield return StartCoroutine(Volley()); break;
                case "drones": yield return StartCoroutine(SummonDrones()); break;
                case "beams": yield return StartCoroutine(RotatingBeams()); break;
                case "hazards": yield return StartCoroutine(ArenaHazards()); break;
            }
        }

        Vector3 EyePosition => eye != null ? eye.position : BodyPosition;

        IEnumerator Sweep()
        {
            var target = Target;
            if (target == null) yield break;
            Vector3 flatCenter = new Vector3(BodyPosition.x, floorY, BodyPosition.z);
            Vector3 toTarget = target.transform.position - flatCenter;
            toTarget.y = 0f;
            float distance = Mathf.Clamp(toTarget.magnitude, 6f, arenaRadius);
            float baseAngle = Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg;
            float side = Random.value > 0.5f ? 1f : -1f;
            float startAngle = baseAngle - sweepArc * 0.5f * side;
            float endAngle = baseAngle + sweepArc * 0.5f * side;

            var beam = SpawnLaser(EyePosition, Quaternion.identity, 90f, sweepDamage);
            if (beam == null) yield break;
            beam.SetArmed(false);
            AudioManager.Play(SoundId.LaserCharge, EyePosition, 0.9f);

            float t = 0f;
            while (t < 0.8f)
            {
                t += Time.deltaTime;
                AimBeamAtFloor(beam, flatCenter, startAngle, distance);
                yield return null;
            }

            beam.SetArmed(true);
            AudioManager.Play(SoundId.LaserFire, EyePosition, 1f);
            t = 0f;
            float duration = sweepDuration * (Phase >= 2 ? 0.8f : 1f);
            while (t < duration)
            {
                t += Time.deltaTime;
                float angle = Mathf.Lerp(startAngle, endAngle, Mathf.SmoothStep(0f, 1f, t / duration));
                AimBeamAtFloor(beam, flatCenter, angle, distance);
                yield return null;
            }
            DespawnTracked(beam);
            yield return new WaitForSeconds(0.3f);
        }

        void AimBeamAtFloor(LaserBeam beam, Vector3 flatCenter, float angle, float distance)
        {
            Vector3 point = flatCenter + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * distance;
            Vector3 origin = EyePosition;
            Vector3 dir = point - origin;
            beam.transform.SetPositionAndRotation(origin, Quaternion.LookRotation(dir.normalized));
        }

        IEnumerator Volley()
        {
            var registry = PrefabRegistry.Instance;
            int volleys = Phase >= 2 ? 4 : 3;
            for (int v = 0; v < volleys; v++)
            {
                var target = Target;
                if (target == null || registry == null) yield break;
                Vector3 from = EyePosition;
                Vector3 aim = MathUtil.PredictPosition(from, target.AimPoint, target.Velocity, orbSpeed, 0.4f);
                Vector3 dir = (aim - from).normalized;
                int count = Mathf.Max(1, orbsPerVolley + (Phase >= 1 ? 2 : 0));
                for (int i = 0; i < count; i++)
                {
                    float offset = count == 1 ? 0f : Mathf.Lerp(-orbSpread, orbSpread, (float)i / (count - 1));
                    Vector3 d = Quaternion.AngleAxis(offset, Vector3.up) * dir;
                    FireProjectile(registry.energyOrb, from, d, orbSpeed, orbDamage, 4f);
                }
                AudioManager.Play(SoundId.EnemyShot, from, 0.9f, 0.6f);
                yield return new WaitForSeconds(0.4f);
            }
        }

        IEnumerator SummonDrones()
        {
            drones.RemoveAll(d => d == null || d.IsDead);
            int count = Mathf.Min(Phase >= 1 ? 3 : 2, maxDrones - drones.Count);
            if (count <= 0)
            {
                yield return StartCoroutine(Volley());
                yield break;
            }
            AudioManager.Play(SoundId.EnemyAlert, BodyPosition, 1f, 0.5f);
            for (int i = 0; i < count; i++)
            {
                Vector3 pos = BodyPosition + Random.insideUnitSphere * 4f;
                pos.y = Mathf.Max(pos.y, floorY + 4f);
                var drone = EnemySpawner.Spawn(EnemyType.Drone, pos, Quaternion.identity, transform.parent);
                if (drone != null)
                {
                    Track(drone);
                    drones.Add(drone);
                }
                yield return new WaitForSeconds(0.3f);
            }
            yield return new WaitForSeconds(0.6f);
        }

        IEnumerator RotatingBeams()
        {
            // Descend to the arena center.
            Vector3 low = new Vector3(arenaCenter.x, floorY + 3.4f, arenaCenter.z);
            Vector3 start = BodyPosition;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / 1.2f;
                MoveBody(Vector3.Lerp(start, low, Mathf.SmoothStep(0f, 1f, t)));
                yield return null;
            }

            int count = Phase >= 2 ? 4 : (Phase == 1 ? 3 : 2);
            float speed = Phase >= 2 ? 70f : 50f;
            var beams = new List<LaserBeam>();
            Vector3 origin = new Vector3(arenaCenter.x, floorY + beamHeight, arenaCenter.z);
            for (int i = 0; i < count; i++)
            {
                var b = SpawnLaser(origin, Quaternion.Euler(0f, i * 360f / count, 0f), arenaRadius + 5f, beamDamage);
                if (b != null)
                {
                    b.SetArmed(false);
                    beams.Add(b);
                }
            }
            AudioManager.Play(SoundId.LaserCharge, origin, 1f, 0.8f);
            yield return new WaitForSeconds(1.1f);
            foreach (var b in beams) b.SetArmed(true);
            AudioManager.Play(SoundId.LaserFire, origin, 1f, 0.7f);

            float yaw = 0f;
            float direction = Random.value > 0.5f ? 1f : -1f;
            t = 0f;
            while (t < beamDuration)
            {
                float dt = Time.deltaTime;
                t += dt;
                if (Phase >= 2 && t > beamDuration * 0.5f && t - dt <= beamDuration * 0.5f) direction = -direction;
                yaw += speed * direction * dt;
                for (int i = 0; i < beams.Count; i++)
                {
                    if (beams[i] == null) continue;
                    beams[i].transform.SetPositionAndRotation(origin, Quaternion.Euler(0f, yaw + i * 360f / beams.Count, 0f));
                }
                yield return null;
            }
            foreach (var b in beams) DespawnTracked(b);

            start = BodyPosition;
            Vector3 high = HoverPoint(start);
            t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / 1f;
                MoveBody(Vector3.Lerp(start, high, Mathf.SmoothStep(0f, 1f, t)));
                yield return null;
            }
        }

        IEnumerator ArenaHazards()
        {
            var panels = new List<HazardPanel>();
            foreach (var p in HazardPanel.All)
            {
                if (p == null) continue;
                Vector3 d = p.transform.position - arenaCenter;
                d.y = 0f;
                if (d.magnitude <= arenaRadius) panels.Add(p);
            }

            int waves = Phase >= 2 ? 2 : 1;
            for (int w = 0; w < waves; w++)
            {
                if (panels.Count > 0)
                {
                    foreach (var p in panels)
                    {
                        if (Random.value < 0.45f) p.Trigger(hazardWarn, hazardActive);
                    }
                    AudioManager.Play(SoundId.LaserCharge, BodyPosition, 0.8f, 0.5f);
                    yield return new WaitForSeconds(hazardWarn + hazardActive * 0.6f);
                }
                else
                {
                    yield return StartCoroutine(OrbitalStrikes());
                }
            }
        }

        /// <summary>Fallback when the arena has no hazard panels: telegraphed strikes around the player.</summary>
        IEnumerator OrbitalStrikes()
        {
            var target = Target;
            if (target == null) yield break;
            var points = new List<Vector3>();
            for (int i = 0; i < 6; i++)
            {
                Vector2 r = i == 0 ? Vector2.zero : Random.insideUnitCircle * 9f;
                Vector3 p = target.FeetPosition + new Vector3(r.x, 0f, r.y);
                p.y = floorY;
                points.Add(p);
                TelegraphCircle(p, 3.5f, 1.2f, StrikeColor);
            }
            yield return new WaitForSeconds(1.2f);
            foreach (var p in points) AreaBlast(p + Vector3.up * 0.5f, 3.5f, 22f, 10f);
            yield return new WaitForSeconds(0.4f);
        }

        protected override void OnReset()
        {
            drones.Clear();
            foreach (var p in HazardPanel.All)
            {
                if (p != null) p.ForceIdle();
            }
        }

        protected override void OnDefeatedCleanup()
        {
            foreach (var p in HazardPanel.All)
            {
                if (p != null) p.ForceIdle();
            }
        }
    }
}

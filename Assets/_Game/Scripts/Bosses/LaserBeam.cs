using Momentum.PlayerSystems;
using UnityEngine;

namespace Momentum.Bosses
{
    /// <summary>
    /// Damaging beam along this transform's forward axis. Owners move/rotate the transform; the beam
    /// raycasts each frame, stops at walls and damages the player on contact while armed.
    /// Unarmed beams are thin telegraph lines that deal no damage.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class LaserBeam : MonoBehaviour, IPoolable
    {
        [SerializeField] float length = 40f;
        [SerializeField] float radius = 0.3f;
        [SerializeField] float damagePerTick = 10f;
        [SerializeField] float tickInterval = 0.2f;
        [SerializeField] float knockback = 7f;
        [SerializeField] Color warnColor = new Color(1f, 0.75f, 0.1f, 0.5f);
        [SerializeField] Color armedColor = new Color(1f, 0.1f, 0.15f, 1f);
        [SerializeField] float warnWidth = 0.06f;
        [SerializeField] float armedWidth = 0.4f;

        LineRenderer line;
        Transform ignoreRoot;
        GameObject source;
        float nextTick;

        public bool Armed { get; private set; }
        public float Length
        {
            get => length;
            set => length = Mathf.Max(0.5f, value);
        }

        void Awake()
        {
            line = GetComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
        }

        public void Configure(float beamLength, float tickDamage, GameObject owner)
        {
            Length = beamLength;
            damagePerTick = tickDamage;
            source = owner;
            ignoreRoot = owner != null ? owner.transform : null;
        }

        public void SetArmed(bool armed)
        {
            Armed = armed;
        }

        public void OnSpawned()
        {
            Armed = false;
            line.enabled = true;
        }

        public void OnDespawned()
        {
            Armed = false;
            source = null;
            ignoreRoot = null;
        }

        void LateUpdate()
        {
            Vector3 origin = transform.position;
            Vector3 dir = transform.forward;
            Vector3 end = origin + dir * length;
            int mask = Layers.SolidMask | (1 << Layers.Player);
            if (PhysicsUtil.SweepClosest(origin, dir, length, Armed ? radius : 0.05f, mask, ignoreRoot, out RaycastHit hit))
            {
                end = origin + dir * hit.distance;
                if (Armed && hit.collider.gameObject.layer == Layers.Player && Time.time >= nextTick)
                {
                    var player = hit.collider.GetComponentInParent<PlayerController>();
                    if (player != null && !player.IsDead)
                    {
                        nextTick = Time.time + tickInterval;
                        player.Health.TakeDamage(new DamageInfo(damagePerTick, DamageType.Laser, source, hit.point, dir));
                        Vector3 push = Vector3.ProjectOnPlane(player.AimPoint - origin, dir).normalized;
                        player.ApplyKnockback(push * knockback + Vector3.up * 2f, 0f);
                    }
                }
            }

            line.SetPosition(0, origin);
            line.SetPosition(1, end);
            Color c = Armed ? armedColor : warnColor;
            if (Armed) c *= 0.85f + 0.15f * Mathf.Sin(Time.time * 50f);
            c.a = Armed ? 1f : warnColor.a;
            line.startColor = c;
            line.endColor = c;
            line.widthMultiplier = Armed ? armedWidth * (0.9f + 0.1f * Mathf.Sin(Time.time * 70f)) : warnWidth;
        }
    }
}

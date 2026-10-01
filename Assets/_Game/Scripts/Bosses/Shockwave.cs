using Momentum.PlayerSystems;
using UnityEngine;

namespace Momentum.Bosses
{
    /// <summary>
    /// Expanding ring hazard. Low rings must be jumped over; high rings (raised base) must be slid under.
    /// Hits the player at most once.
    /// </summary>
    public class Shockwave : MonoBehaviour, IPoolable
    {
        [SerializeField] Transform ringVisual;
        [SerializeField] Renderer ringRenderer;
        [SerializeField] float thickness = 1.2f;

        Vector3 center;
        float radius;
        float maxRadius;
        float speed;
        float height;
        float baseOffset;
        float damage;
        float knockback;
        bool active;
        bool hitPlayer;
        Color color;
        GameObject source;
        MaterialPropertyBlock block;

        void Awake()
        {
            block = new MaterialPropertyBlock();
            if (ringRenderer == null && ringVisual != null) ringRenderer = ringVisual.GetComponent<Renderer>();
        }

        public void Launch(Vector3 origin, float maxRadius, float speed, float height, float baseOffset, float damage, float knockback, Color tint, GameObject owner)
        {
            center = origin;
            this.maxRadius = maxRadius;
            this.speed = speed;
            this.height = height;
            this.baseOffset = baseOffset;
            this.damage = damage;
            this.knockback = knockback;
            color = tint;
            source = owner;
            radius = 0.5f;
            hitPlayer = false;
            active = true;
            transform.position = center;
            transform.rotation = Quaternion.identity;
            UpdateVisual();
        }

        public void OnSpawned()
        {
            active = false;
        }

        public void OnDespawned()
        {
            active = false;
        }

        void Update()
        {
            if (!active) return;
            radius += speed * Time.deltaTime;
            UpdateVisual();
            CheckPlayer();
            if (radius >= maxRadius)
            {
                active = false;
                PoolManager.Despawn(gameObject);
            }
        }

        void UpdateVisual()
        {
            if (ringVisual != null)
            {
                ringVisual.localPosition = Vector3.up * baseOffset;
                ringVisual.localScale = new Vector3(radius, height, radius);
            }
            if (ringRenderer != null)
            {
                var c = color;
                c.a = Mathf.Lerp(0.9f, 0.2f, radius / Mathf.Max(1f, maxRadius));
                ringRenderer.GetPropertyBlock(block);
                block.SetColor("_Color", c);
                block.SetColor("_EmissionColor", new Color(color.r, color.g, color.b) * 2f);
                ringRenderer.SetPropertyBlock(block);
            }
        }

        void CheckPlayer()
        {
            if (hitPlayer) return;
            var player = PlayerController.Current;
            if (player == null || player.IsDead || player.Movement == null) return;
            Vector3 feet = player.FeetPosition;
            Vector3 flat = feet - center;
            flat.y = 0f;
            float dist = flat.magnitude;
            float playerRadius = player.Movement.Capsule.radius;
            if (Mathf.Abs(dist - radius) > thickness * 0.5f + playerRadius) return;

            float ringBottom = center.y + baseOffset;
            float ringTop = ringBottom + height;
            float playerBottom = feet.y;
            float playerTop = feet.y + player.Movement.Capsule.height;
            if (playerTop < ringBottom || playerBottom > ringTop) return;

            hitPlayer = true;
            Vector3 outward = dist > 0.01f ? flat / dist : Vector3.forward;
            player.Health.TakeDamage(new DamageInfo(damage, DamageType.Hazard, source, feet, outward));
            player.ApplyKnockback(outward * knockback + Vector3.up * knockback * 0.5f, 0f);
            CameraShaker.Shake(0.35f);
        }
    }
}

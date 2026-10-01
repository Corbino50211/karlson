using System.Collections;
using Momentum.Audio;
using Momentum.PlayerSystems;
using UnityEngine;

namespace Momentum.Levels
{
    /// <summary>
    /// Reusable destructible: glass, crates, explosive barrels, breakable walls.
    /// Breaks from damage (bullets, explosions), optionally from a fast player impact (smash through glass
    /// without losing speed) and optionally from boss charges. Spawns physics fragments and can explode.
    /// </summary>
    public class BreakableObject : MonoBehaviour, IDamageable, ILevelObjectConfigurable
    {
        [Header("Health")]
        [SerializeField] float maxHealth = 30f;
        [SerializeField] float explosionDamageMultiplier = 2f;

        [Header("Impact")]
        [Tooltip("Break when the player runs into it faster than the threshold (glass).")]
        [SerializeField] bool breakOnPlayerImpact;
        [SerializeField] float impactSpeedThreshold = 9f;
        [Tooltip("Boss charges smash straight through this object.")]
        [SerializeField] bool bossBreakable = true;

        [Header("Fragments")]
        [SerializeField] GameObject fragmentPrefab;
        [SerializeField] int fragmentCount = 8;
        [SerializeField] Vector2 fragmentScale = new Vector2(0.15f, 0.45f);
        [SerializeField] bool fragmentsUseOwnMaterial = true;
        [SerializeField] SoundId breakSound = SoundId.CrateBreak;

        [Header("Explosive")]
        [SerializeField] bool explosive;
        [SerializeField] float explosionRadius = 6f;
        [SerializeField] float explosionDamage = 70f;
        [SerializeField] float explosionForce = 20f;
        [SerializeField] float explosionDelay = 0.12f;

        float health;
        bool broken;
        Renderer mainRenderer;

        public bool IsAlive => !broken;
        public bool BossBreakable => bossBreakable;
        public bool Explosive => explosive;

        void Awake()
        {
            health = maxHealth;
            mainRenderer = GetComponentInChildren<MeshRenderer>();
        }

        public void TakeDamage(DamageInfo info)
        {
            if (broken) return;
            float amount = info.amount * (info.type == DamageType.Explosion ? explosionDamageMultiplier : 1f);
            health -= amount;
            if (health <= 0f) Break(info.direction * Mathf.Max(3f, info.knockback));
        }

        void OnCollisionEnter(Collision collision)
        {
            if (broken || !breakOnPlayerImpact) return;
            var movement = collision.collider.GetComponentInParent<PlayerMovement>();
            if (movement == null) return;
            // Only the speed *into* the surface counts (running along glass does not break it).
            Vector3 normal = collision.contactCount > 0 ? collision.GetContact(0).normal : -movement.PreviousVelocity.normalized;
            float speed = Mathf.Abs(Vector3.Dot(movement.PreviousVelocity, normal));
            if (speed < impactSpeedThreshold) return;
            Break(movement.PreviousVelocity * 0.4f);
            movement.RestorePreviousVelocity(0.92f);
        }

        /// <summary>Destroys the object, spawning fragments and (if explosive) an explosion.</summary>
        public void Break(Vector3 impulse)
        {
            if (broken) return;
            broken = true;
            Bounds bounds = mainRenderer != null ? mainRenderer.bounds : new Bounds(transform.position, Vector3.one);
            SpawnFragments(bounds, impulse);
            AudioManager.Play(breakSound, bounds.center, 0.9f);

            if (explosive)
            {
                var runner = PoolManager.Instance;
                if (runner != null && explosionDelay > 0f) runner.StartCoroutine(DelayedExplosion(bounds.center, explosionDelay, explosionRadius, explosionDamage, explosionForce));
                else Explode(bounds.center, explosionRadius, explosionDamage, explosionForce);
            }

            foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
            gameObject.SetActive(false);
            Destroy(gameObject, 0.5f);
        }

        static IEnumerator DelayedExplosion(Vector3 position, float delay, float radius, float damage, float force)
        {
            yield return new WaitForSeconds(delay);
            Explode(position, radius, damage, force);
        }

        static void Explode(Vector3 position, float radius, float damage, float force)
        {
            var p = ExplosionParams.Create(position, radius, damage, force, null, false);
            p.selfDamageMultiplier = 1f;
            p.cameraShake = 0.7f;
            Explosions.Explode(p);
        }

        void SpawnFragments(Bounds bounds, Vector3 impulse)
        {
            var prefab = fragmentPrefab;
            if (prefab == null && PrefabRegistry.Instance != null) prefab = PrefabRegistry.Instance.debrisChunk;
            if (prefab == null) return;
            Material mat = fragmentsUseOwnMaterial && mainRenderer != null ? mainRenderer.sharedMaterial : null;
            float sizeScale = Mathf.Clamp(Mathf.Min(bounds.size.x, Mathf.Min(bounds.size.y, bounds.size.z)) * 2f, 0.3f, 2f);
            int count = Mathf.Clamp(Mathf.RoundToInt(fragmentCount * Mathf.Clamp(bounds.size.magnitude / 2f, 0.5f, 2.5f)), 3, 24);
            for (int i = 0; i < count; i++)
            {
                Vector3 pos = new Vector3(
                    Random.Range(bounds.min.x, bounds.max.x),
                    Random.Range(bounds.min.y, bounds.max.y),
                    Random.Range(bounds.min.z, bounds.max.z));
                var go = PoolManager.Spawn(prefab, pos, Random.rotation);
                if (go == null) continue;
                go.transform.localScale = Vector3.one * Random.Range(fragmentScale.x, fragmentScale.y) * sizeScale;
                if (mat != null)
                {
                    var r = go.GetComponent<MeshRenderer>();
                    if (r != null) r.sharedMaterial = mat;
                }
                var body = go.GetComponent<Rigidbody>();
                if (body != null)
                {
                    body.velocity = impulse + Random.insideUnitSphere * 4f + Vector3.up * 2f;
                    body.angularVelocity = Random.insideUnitSphere * 8f;
                }
            }
        }

        public void ApplyLevelProperties(LevelObjectData data)
        {
            maxHealth = data.GetFloat("health", maxHealth);
            bossBreakable = data.GetBool("bossBreakable", bossBreakable);
            health = maxHealth;
        }
    }
}

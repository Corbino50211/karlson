using UnityEngine;

namespace Momentum.Bosses
{
    /// <summary>Pulsing translucent warning shape on the ground (slam zones, charge lanes, strikes).</summary>
    public class TelegraphIndicator : MonoBehaviour, IPoolable
    {
        [SerializeField] Renderer shapeRenderer;
        [SerializeField] float pulseSpeed = 18f;

        MaterialPropertyBlock block;
        Color color = new Color(1f, 0.2f, 0.1f, 0.5f);
        float duration = 1f;
        float age;
        bool showing;

        void Awake()
        {
            if (shapeRenderer == null) shapeRenderer = GetComponentInChildren<Renderer>();
            block = new MaterialPropertyBlock();
        }

        public void Show(Vector3 position, Quaternion rotation, Vector3 scale, float time, Color tint)
        {
            transform.SetPositionAndRotation(position, rotation);
            transform.localScale = scale;
            duration = Mathf.Max(0.05f, time);
            color = tint;
            age = 0f;
            showing = true;
            Apply(0f);
        }

        public void OnSpawned()
        {
            age = 0f;
            showing = false;
        }

        public void OnDespawned()
        {
            showing = false;
        }

        void Update()
        {
            if (!showing) return;
            age += Time.deltaTime;
            float k = Mathf.Clamp01(age / duration);
            Apply(k);
            if (age >= duration)
            {
                showing = false;
                PoolManager.Despawn(gameObject);
            }
        }

        void Apply(float k)
        {
            if (shapeRenderer == null) return;
            float pulse = (Mathf.Sin(age * pulseSpeed * Mathf.Lerp(0.6f, 1.6f, k)) + 1f) * 0.5f;
            var c = color;
            c.a = Mathf.Lerp(0.15f, 0.65f, pulse) * Mathf.Lerp(0.6f, 1f, k);
            shapeRenderer.GetPropertyBlock(block);
            block.SetColor("_Color", c);
            block.SetColor("_EmissionColor", new Color(c.r, c.g, c.b) * 1.5f);
            shapeRenderer.SetPropertyBlock(block);
        }
    }
}

using UnityEngine;

namespace Momentum
{
    /// <summary>
    /// Returns the object to its pool after a lifetime. Restarts particle systems and clears trails
    /// each time the object is spawned, so pooled effects replay correctly.
    /// </summary>
    public class AutoDespawn : MonoBehaviour, IPoolable
    {
        [SerializeField] float lifetime = 2f;
        [SerializeField] bool useUnscaledTime;

        float timer;
        ParticleSystem[] particleSystems;
        TrailRenderer[] trails;

        public float Lifetime
        {
            get => lifetime;
            set => lifetime = value;
        }

        void Awake()
        {
            particleSystems = GetComponentsInChildren<ParticleSystem>(true);
            trails = GetComponentsInChildren<TrailRenderer>(true);
        }

        void OnEnable()
        {
            timer = 0f;
        }

        public void OnSpawned()
        {
            timer = 0f;
            if (particleSystems != null)
            {
                foreach (var ps in particleSystems)
                {
                    if (ps == null) continue;
                    ps.Clear(true);
                    ps.Play(true);
                }
            }
            if (trails != null)
            {
                foreach (var t in trails)
                {
                    if (t != null) t.Clear();
                }
            }
        }

        public void OnDespawned()
        {
            if (particleSystems == null) return;
            foreach (var ps in particleSystems)
            {
                if (ps != null) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        void Update()
        {
            timer += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            if (timer >= lifetime)
            {
                timer = 0f;
                PoolManager.Despawn(gameObject);
            }
        }
    }
}

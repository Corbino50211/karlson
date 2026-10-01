using UnityEngine;

namespace Momentum
{
    /// <summary>Briefly flashes a light and fades it out. Used by muzzle flashes and explosions.</summary>
    [RequireComponent(typeof(Light))]
    public class LightFlash : MonoBehaviour, IPoolable
    {
        [SerializeField] float peakIntensity = 4f;
        [SerializeField] float duration = 0.08f;

        Light flashLight;
        float timer;

        void Awake()
        {
            flashLight = GetComponent<Light>();
        }

        void OnEnable()
        {
            Restart();
        }

        public void OnSpawned()
        {
            Restart();
        }

        public void OnDespawned()
        {
            if (flashLight != null) flashLight.intensity = 0f;
        }

        void Restart()
        {
            timer = 0f;
            if (flashLight != null)
            {
                flashLight.intensity = peakIntensity;
                flashLight.enabled = true;
            }
        }

        void Update()
        {
            if (flashLight == null || !flashLight.enabled) return;
            timer += Time.deltaTime;
            float t = duration > 0f ? timer / duration : 1f;
            flashLight.intensity = Mathf.Lerp(peakIntensity, 0f, t);
            if (t >= 1f) flashLight.enabled = false;
        }
    }
}

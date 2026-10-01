using Momentum.SaveSystem;
using UnityEngine;

namespace Momentum.PlayerSystems
{
    /// <summary>
    /// Trauma-based camera shake (perlin noise). Explosions, landings, weapons and bosses add trauma;
    /// PlayerCamera reads the resulting offsets. Respects the "Camera Shake" setting.
    /// </summary>
    public class CameraShaker : MonoBehaviour
    {
        public static CameraShaker Instance { get; private set; }

        [SerializeField] float maxAngle = 4f;
        [SerializeField] float maxOffset = 0.12f;
        [SerializeField] float frequency = 22f;
        [SerializeField] float traumaDecay = 1.5f;

        float trauma;
        float seed;

        public Vector3 PositionOffset { get; private set; }
        public Vector3 RotationOffset { get; private set; }
        public float Trauma => trauma;

        void Awake()
        {
            seed = Random.value * 100f;
        }

        void OnEnable()
        {
            Instance = this;
        }

        void OnDisable()
        {
            if (Instance == this) Instance = null;
            trauma = 0f;
            PositionOffset = Vector3.zero;
            RotationOffset = Vector3.zero;
        }

        public void AddTrauma(float amount)
        {
            if (!SettingsManager.Settings.cameraShake) return;
            trauma = Mathf.Clamp01(trauma + amount);
        }

        public void Clear()
        {
            trauma = 0f;
        }

        public static void Shake(float amount)
        {
            if (Instance != null) Instance.AddTrauma(amount);
        }

        /// <summary>Adds trauma scaled by the distance between the camera and a world position.</summary>
        public static void ShakeAt(Vector3 position, float amount, float radius)
        {
            if (Instance == null || radius <= 0f) return;
            float dist = Vector3.Distance(Instance.transform.position, position);
            float falloff = 1f - Mathf.Clamp01(dist / radius);
            if (falloff > 0f) Instance.AddTrauma(amount * falloff);
        }

        void Update()
        {
            if (trauma <= 0f)
            {
                PositionOffset = Vector3.zero;
                RotationOffset = Vector3.zero;
                return;
            }
            float shake = trauma * trauma;
            float t = Time.time * frequency;
            RotationOffset = new Vector3(
                (Mathf.PerlinNoise(seed, t) * 2f - 1f) * maxAngle * shake,
                (Mathf.PerlinNoise(seed + 1f, t) * 2f - 1f) * maxAngle * shake,
                (Mathf.PerlinNoise(seed + 2f, t) * 2f - 1f) * maxAngle * shake);
            PositionOffset = new Vector3(
                (Mathf.PerlinNoise(seed + 3f, t) * 2f - 1f) * maxOffset * shake,
                (Mathf.PerlinNoise(seed + 4f, t) * 2f - 1f) * maxOffset * shake,
                0f);
            trauma = Mathf.Max(0f, trauma - traumaDecay * Time.deltaTime);
        }
    }
}

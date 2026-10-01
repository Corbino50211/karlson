using UnityEngine;

namespace Momentum.Levels
{
    /// <summary>Decorative point light with an emissive fixture. Optional slow pulse.</summary>
    public class DecorLight : MonoBehaviour, ILevelObjectConfigurable
    {
        [SerializeField] Light pointLight;
        [SerializeField] Renderer fixture;
        [SerializeField] Color color = new Color(1f, 0.85f, 0.6f);
        [SerializeField] float range = 12f;
        [SerializeField] float intensity = 2f;
        [SerializeField] bool pulse;
        [SerializeField] float pulseSpeed = 2f;

        MaterialPropertyBlock block;

        void Start()
        {
            Apply();
        }

        void Update()
        {
            if (!pulse || pointLight == null) return;
            pointLight.intensity = intensity * (0.65f + 0.35f * Mathf.Sin(Time.time * pulseSpeed));
        }

        void Apply()
        {
            if (pointLight != null)
            {
                pointLight.color = color;
                pointLight.range = range;
                pointLight.intensity = intensity;
            }
            if (fixture != null)
            {
                if (block == null) block = new MaterialPropertyBlock();
                fixture.GetPropertyBlock(block);
                block.SetColor("_Color", color);
                block.SetColor("_EmissionColor", color * 2f);
                fixture.SetPropertyBlock(block);
            }
        }

        public void ApplyLevelProperties(LevelObjectData data)
        {
            color = data.GetColor("color", color);
            range = data.GetFloat("range", range);
            intensity = data.GetFloat("intensity", intensity);
            pulse = data.GetBool("pulse", pulse);
            Apply();
        }
    }
}

using UnityEngine;

namespace Momentum
{
    /// <summary>Flashes renderers white for a moment when damaged (enemy and boss hit feedback).</summary>
    public class HitFlash : MonoBehaviour
    {
        [SerializeField] Renderer[] renderers = new Renderer[0];
        [SerializeField] Color flashColor = new Color(1f, 1f, 1f);
        [SerializeField] float duration = 0.07f;

        MaterialPropertyBlock flashBlock;
        MaterialPropertyBlock emptyBlock;
        float until;
        bool active;

        void Awake()
        {
            if (renderers == null || renderers.Length == 0) renderers = GetComponentsInChildren<MeshRenderer>(true);
            flashBlock = new MaterialPropertyBlock();
            emptyBlock = new MaterialPropertyBlock();
            flashBlock.SetColor("_Color", flashColor);
            flashBlock.SetColor("_EmissionColor", flashColor * 1.2f);
        }

        public void Flash()
        {
            until = Time.time + duration;
            if (!active) Apply(true);
        }

        void Update()
        {
            if (active && Time.time >= until) Apply(false);
        }

        void OnDisable()
        {
            if (active) Apply(false);
        }

        void Apply(bool on)
        {
            active = on;
            foreach (var r in renderers)
            {
                if (r == null) continue;
                r.SetPropertyBlock(on ? flashBlock : emptyBlock);
            }
        }
    }
}

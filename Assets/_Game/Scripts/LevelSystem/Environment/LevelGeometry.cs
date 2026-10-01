using UnityEngine;

namespace Momentum.Levels
{
    /// <summary>
    /// Static level geometry. Applies the material chosen by key (MaterialLibrary) and the optional
    /// "no wallrun" flag from level data.
    /// </summary>
    public class LevelGeometry : MonoBehaviour, ILevelObjectConfigurable
    {
        [SerializeField] Renderer[] targetRenderers = new Renderer[0];
        [SerializeField] string materialKey = "White";

        public string MaterialKey => materialKey;

        public void ApplyLevelProperties(LevelObjectData data)
        {
            materialKey = data.GetString("material", materialKey);
            ApplyMaterial();
            if (data.GetBool("noWallRun", false))
            {
                foreach (var c in GetComponentsInChildren<Collider>(true)) c.gameObject.tag = Tags.NoWallRun;
            }
        }

        public void ApplyMaterial()
        {
            var mat = MaterialLibrary.Resolve(materialKey);
            if (mat == null) return;
            var renderers = targetRenderers != null && targetRenderers.Length > 0 ? targetRenderers : GetComponentsInChildren<MeshRenderer>(true);
            foreach (var r in renderers)
            {
                if (r != null) r.sharedMaterial = mat;
            }
        }
    }
}

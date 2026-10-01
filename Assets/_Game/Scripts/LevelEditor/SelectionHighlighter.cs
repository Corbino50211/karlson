using System.Collections.Generic;
using UnityEngine;

namespace Momentum.LevelEditor
{
    /// <summary>Draws wireframe bounding boxes around selected editor objects using pooled LineRenderers.</summary>
    public class SelectionHighlighter : MonoBehaviour
    {
        [SerializeField] Color primaryColor = new Color(1f, 0.6f, 0.1f);
        [SerializeField] Color secondaryColor = new Color(0.3f, 0.8f, 1f);
        [SerializeField] float width = 0.04f;

        readonly List<LineRenderer> lines = new List<LineRenderer>();
        readonly Vector3[] corners = new Vector3[16];
        Material material;

        void Awake()
        {
            var lib = MaterialLibrary.Instance;
            material = lib != null && lib.gizmoHighlight != null ? lib.gizmoHighlight : (lib != null ? lib.line : null);
        }

        public void Draw(IReadOnlyList<EditorObject> selected)
        {
            int count = selected != null ? selected.Count : 0;
            while (lines.Count < count) lines.Add(CreateLine());
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                if (i >= count || selected[i] == null)
                {
                    if (line.enabled) line.enabled = false;
                    continue;
                }
                line.enabled = true;
                var b = selected[i].GetBounds();
                b.Expand(0.08f);
                FillCorners(b);
                line.positionCount = corners.Length;
                line.SetPositions(corners);
                var c = i == 0 ? primaryColor : secondaryColor;
                line.startColor = c;
                line.endColor = c;
                float camDist = Camera.main != null ? Vector3.Distance(Camera.main.transform.position, b.center) : 10f;
                line.widthMultiplier = width * Mathf.Clamp(camDist / 15f, 0.5f, 4f);
            }
        }

        LineRenderer CreateLine()
        {
            var go = new GameObject("SelectionBox");
            go.layer = Layers.EditorGizmo;
            go.transform.SetParent(transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.loop = false;
            line.numCornerVertices = 0;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            if (material != null) line.sharedMaterial = material;
            line.enabled = false;
            return line;
        }

        void FillCorners(Bounds b)
        {
            Vector3 min = b.min;
            Vector3 max = b.max;
            Vector3 b0 = new Vector3(min.x, min.y, min.z);
            Vector3 b1 = new Vector3(max.x, min.y, min.z);
            Vector3 b2 = new Vector3(max.x, min.y, max.z);
            Vector3 b3 = new Vector3(min.x, min.y, max.z);
            Vector3 t0 = new Vector3(min.x, max.y, min.z);
            Vector3 t1 = new Vector3(max.x, max.y, min.z);
            Vector3 t2 = new Vector3(max.x, max.y, max.z);
            Vector3 t3 = new Vector3(min.x, max.y, max.z);
            // Continuous path covering all 12 edges.
            corners[0] = b0; corners[1] = b1; corners[2] = b2; corners[3] = b3; corners[4] = b0;
            corners[5] = t0; corners[6] = t1; corners[7] = b1; corners[8] = t1; corners[9] = t2;
            corners[10] = b2; corners[11] = t2; corners[12] = t3; corners[13] = b3; corners[14] = t3; corners[15] = t0;
        }
    }
}
